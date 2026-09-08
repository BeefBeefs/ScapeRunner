namespace OSRSIdle;

public class ActivityManager : IDisposable
{
    private readonly Player _player;

    private readonly Random _random = new();

    // ============================================================
    // CURRENT ACTIVITY
    // ============================================================

    public Skill? CurrentSkill { get; private set; }

    public SkillActivity? CurrentActivity { get; private set; }

    public bool IsActive =>
        CurrentSkill != null &&
        CurrentActivity != null;


    // ============================================================
    // ACTION TIMING
    // ============================================================

    public DateTime ActionStarted { get; private set; }

    public DateTime ActionEnds { get; private set; }


    // ============================================================
    // EVENTS
    // ============================================================

    // Fired when an activity starts, stops, or its timing is recalculated.
    public event EventHandler? ActivityStateChanged;

    // Fired after an action rewards XP/items and records the action.
    public event EventHandler? ActionCompleted;

    // Fired whenever a skill levels up.
    public event EventHandler<LevelUpEventArgs>? LevelUp;

    // Fired when a requested activity cannot be started or its reward cannot
    // be stored. The UI can explain the block instead of failing silently.
    public event EventHandler<ActivityBlockedEventArgs>? ActivityBlocked;


    // ============================================================
    // CANCELLATION
    // ============================================================

    private CancellationTokenSource? _cancellationTokenSource;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public ActivityManager(
        Player player)
    {
        _player = player;
        GameClock.SpeedChanged += OnGameSpeedChanged;
    }


    // ============================================================
    // START ACTIVITY
    // ============================================================

    public void StartActivity(
        Skill skill,
        SkillActivity activity)
    {
        if (!skill.Activities.Contains(activity) ||
            skill.Level < activity.RequiredLevel)
        {
            ActivityBlocked?.Invoke(
                this,
                new ActivityBlockedEventArgs(
                    skill,
                    activity,
                    "Reach the required level first."));
            return;
        }

        // Stop whatever we were doing before.
        StopActivity(raiseStateChanged: false);

        CurrentSkill = skill;
        CurrentActivity = activity;

        // Set up the first action.
        SetNextAction();

        ActivityStateChanged?.Invoke(
            this,
            EventArgs.Empty);

        // Start ONE activity loop.
        _cancellationTokenSource =
            new CancellationTokenSource();

        _ = RunActivityAsync(
            _cancellationTokenSource.Token);
    }


    // ============================================================
    // SET NEXT ACTION
    // ============================================================

    private void SetNextAction()
    {
        if (CurrentActivity == null)
            return;

        ActionStarted =
            DateTime.UtcNow;

        ActionEnds = ActionStarted.AddMilliseconds(
            ActivityMetrics.EffectiveActionTicks(CurrentActivity) *
            GameClock.StandardTickMilliseconds /
            (double)GameClock.SpeedMultiplier);
    }

    private void OnGameSpeedChanged(object? sender, EventArgs e)
    {
        if (!IsActive || ActionStarted == default || ActionEnds == default)
            return;

        DateTime now = DateTime.UtcNow;
        double oldDurationMilliseconds =
            (ActionEnds - ActionStarted).TotalMilliseconds;

        if (oldDurationMilliseconds <= 0)
            return;

        double progress = Math.Clamp(
            (now - ActionStarted).TotalMilliseconds /
            oldDurationMilliseconds,
            0,
            1);

        double newDurationMilliseconds =
            ActivityMetrics.EffectiveActionTicks(CurrentActivity!) *
            GameClock.StandardTickMilliseconds /
            (double)GameClock.SpeedMultiplier;

        ActionStarted = now.AddMilliseconds(
            -(newDurationMilliseconds * progress));
        ActionEnds = ActionStarted.AddMilliseconds(
            newDurationMilliseconds);

        ActivityStateChanged?.Invoke(this, EventArgs.Empty);
    }


    // ============================================================
    // ACTIVITY LOOP
    // ============================================================

    private async Task RunActivityAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (CurrentSkill == null ||
                CurrentActivity == null)
            {
                return;
            }

            TimeSpan remaining =
                ActionEnds - DateTime.UtcNow;

            if (remaining > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(
                            Math.Min(remaining.TotalMilliseconds, 100)),
                        cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                // Re-evaluate the deadline after each short wait so a
                // mid-action debug-speed change takes effect immediately.
                continue;
            }

            if (cancellationToken.IsCancellationRequested)
                return;


            // ====================================================
            // ACTION COMPLETE
            // ====================================================

            // Remember the level before XP is awarded.
            int oldLevel =
                CurrentSkill.Level;


            if (CurrentActivity.ItemReward != null &&
                !_player.Inventory.CanAddItem(CurrentActivity.ItemReward))
            {
                Skill blockedSkill = CurrentSkill;
                SkillActivity blockedActivity = CurrentActivity;
                StopActivity();
                ActivityBlocked?.Invoke(
                    this,
                    new ActivityBlockedEventArgs(
                        blockedSkill,
                        blockedActivity,
                        $"Inventory full for {blockedActivity.ItemReward.Name}."));
                return;
            }

            // Award XP.
            CurrentSkill.AddXP(
                CurrentActivity.XP *
                CombatRules.GetSkillXpMultiplier(CurrentSkill));

            CurrentSkill.RecordAction();

            AwardActivityItem(
                CurrentActivity);

            TryAwardSkillingPet(
                CurrentSkill,
                CurrentActivity);


            // Determine the new level.
            int newLevel =
                CurrentSkill.Level;


            // ====================================================
            // XP EVENT
            // ====================================================

            ActionCompleted?.Invoke(
                this,
                EventArgs.Empty);


            // ====================================================
            // LEVEL UP EVENT
            // ====================================================

            if (newLevel > oldLevel)
            {
                LevelUp?.Invoke(
                    this,
                    new LevelUpEventArgs(
                        CurrentSkill,
                        newLevel));
            }


            // ====================================================
            // START NEXT ACTION
            // ====================================================

            // We do NOT start another async loop here.
            // We simply reset the timestamps.
            SetNextAction();

        }
    }


    // ============================================================
    // STOP ACTIVITY
    // ============================================================

    public void StopActivity(bool raiseStateChanged = true)
    {
        _cancellationTokenSource?.Cancel();

        _cancellationTokenSource?.Dispose();

        _cancellationTokenSource = null;

        CurrentSkill = null;

        CurrentActivity = null;

        ActionStarted = default;

        ActionEnds = default;

        if (raiseStateChanged)
        {
            ActivityStateChanged?.Invoke(
                this,
                EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        StopActivity();
        GameClock.SpeedChanged -= OnGameSpeedChanged;
    }


    // ============================================================
    // ACTION REWARDS
    // ============================================================

    private void AwardActivityItem(
        SkillActivity activity)
    {
        if (activity.ItemReward == null)
            return;

        _player.Inventory.AddItem(
            activity.ItemReward);
    }

    private void TryAwardSkillingPet(
        Skill skill,
        SkillActivity activity)
    {
        SkillingPet? pet =
            SkillingPetData.GetForSkill(skill);

        if (pet == null ||
            _player.CollectionLog.HasReceivedSkillingPet(pet.Item))
        {
            return;
        }

        double denominator =
            SkillingPetData.GetDropDenominator(
                pet,
                activity);

        if (_random.NextDouble() >= 1d / denominator)
            return;

        if (!_player.Inventory.AddItem(pet.Item))
            return;

        _player.RecordLuckiestDrop(
            pet.Item,
            Math.Max(1, skill.ActionsCompleted),
            1d / denominator,
            $"{skill.Name} actions");

        _player.CollectionLog.RecordSkillingPet(
            pet.Item);
    }
}

public sealed class ActivityBlockedEventArgs : EventArgs
{
    public Skill Skill { get; }
    public SkillActivity Activity { get; }
    public string Reason { get; }

    public ActivityBlockedEventArgs(
        Skill skill,
        SkillActivity activity,
        string reason)
    {
        Skill = skill;
        Activity = activity;
        Reason = reason;
    }
}


// ================================================================
// LEVEL UP EVENT DATA
// ================================================================

public class LevelUpEventArgs : EventArgs
{
    public Skill Skill { get; }

    public int Level { get; }


    public LevelUpEventArgs(
        Skill skill,
        int level)
    {
        Skill = skill;
        Level = level;
    }
}
