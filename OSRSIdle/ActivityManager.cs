namespace OSRSIdle;

public class ActivityManager
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

    // Fired whenever the current activity changes.
    public event EventHandler? ActivityChanged;

    // Fired whenever XP is awarded.
    public event EventHandler? XPChanged;

    // Fired whenever a skill levels up.
    public event EventHandler<LevelUpEventArgs>? LevelUp;


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
    }


    // ============================================================
    // START ACTIVITY
    // ============================================================

    public void StartActivity(
        Skill skill,
        SkillActivity activity)
    {
        // Stop whatever we were doing before.
        StopActivity();

        CurrentSkill = skill;
        CurrentActivity = activity;

        // Set up the first action.
        SetNextAction();

        ActivityChanged?.Invoke(
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
            CurrentActivity.ActionTicks * 600);
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
                        remaining,
                        cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return;


            // ====================================================
            // ACTION COMPLETE
            // ====================================================

            // Remember the level before XP is awarded.
            int oldLevel =
                CurrentSkill.Level;


            // Award XP.
            CurrentSkill.AddXP(
                CurrentActivity.XP);

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

            XPChanged?.Invoke(
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

            ActivityChanged?.Invoke(
                this,
                EventArgs.Empty);
        }
    }


    // ============================================================
    // STOP ACTIVITY
    // ============================================================

    public void StopActivity()
    {
        _cancellationTokenSource?.Cancel();

        _cancellationTokenSource?.Dispose();

        _cancellationTokenSource = null;

        CurrentSkill = null;

        CurrentActivity = null;

        ActionStarted = default;

        ActionEnds = default;

        ActivityChanged?.Invoke(
            this,
            EventArgs.Empty);
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
