namespace OSRSIdle;

public class Game
{
    public Player Player { get; private set; }

    public long OfflineTicks { get; private set; }

    public OfflineActivitySaveData? OfflineActivity { get; private set; }

    public OfflineSkillingSummary? OfflineSkillingSummary { get; private set; }

    public CombatStyle LastCombatStyle { get; private set; } = CombatStyle.Attack;

    public bool IsOfflineCombatSimulationActive { get; private set; }

    private ActivityManager? _activityManager;

    private CombatManager? _combatManager;

    private readonly Random _offlineProgressRandom = new();

    private readonly object _saveLock = new();

    private bool _savePending;

    public bool HasStarted { get; private set; }

    public Game()
    {
        Player = new Player();
        Player.Inventory.InventoryChanged += ScheduleSave;
        Player.EquipmentChanged += ScheduleSave;
        Player.AutoEatSettingsChanged += ScheduleSave;
        Player.CollectionLog.CollectionChanged += ScheduleSave;
        Player.LuckiestDropChanged += ScheduleSave;
    }

    public void Load()
    {
        OfflineProgressLoadResult loadResult = SaveManager.Load(Player);
        OfflineTicks = loadResult.ElapsedTicks;
        OfflineActivity = loadResult.ActiveActivity;

        if (Enum.TryParse(loadResult.LastCombatStyle, out CombatStyle style))
        {
            LastCombatStyle = style;
        }

        HasStarted = true;

        ApplyOfflineSkillingProgress();

        // Persist the awarded rewards and a fresh timestamp before the
        // activity managers are created for the active game session.
        Save();
    }

    public void SetActivityManagers(
        ActivityManager activityManager,
        CombatManager combatManager)
    {
        if (_activityManager != null)
            _activityManager.ActivityStateChanged -= OnActivityChangedForSave;

        if (_combatManager != null)
        {
            _combatManager.CombatStarted -= ScheduleSave;
            _combatManager.CombatStopped -= ScheduleSave;
        }

        _activityManager = activityManager;
        _combatManager = combatManager;

        _combatManager.SetCombatStyle(LastCombatStyle);

        _activityManager.ActivityStateChanged += OnActivityChangedForSave;
        _combatManager.CombatStarted += ScheduleSave;
        _combatManager.CombatStopped += ScheduleSave;
    }

    private void OnActivityChangedForSave(object? sender, EventArgs e)
    {
        ScheduleSave();
    }

    public void ClearActivityManagers(
        ActivityManager activityManager,
        CombatManager combatManager)
    {
        if (!ReferenceEquals(_activityManager, activityManager) ||
            !ReferenceEquals(_combatManager, combatManager))
        {
            return;
        }

        _activityManager.ActivityStateChanged -= OnActivityChangedForSave;
        _combatManager.CombatStarted -= ScheduleSave;
        _combatManager.CombatStopped -= ScheduleSave;
        _activityManager = null;
        _combatManager = null;
    }

    public void ResumeSavedSkillingActivity(
        ActivityManager activityManager)
    {
        if (OfflineActivity?.Kind != "Skilling" ||
            string.IsNullOrWhiteSpace(OfflineActivity.SkillName) ||
            string.IsNullOrWhiteSpace(OfflineActivity.ActivityName))
        {
            return;
        }

        Skill? skill = Player.GetAllSkills().FirstOrDefault(candidate =>
            candidate.Name == OfflineActivity.SkillName);

        SkillActivity? activity = skill?.Activities.FirstOrDefault(candidate =>
            candidate.Name == OfflineActivity.ActivityName);

        if (skill != null && activity != null)
        {
            activityManager.StartActivity(skill, activity);
        }
    }

    public OfflineCombatLoad? BeginOfflineCombatSimulation()
    {
        if (OfflineTicks <= 0 ||
            OfflineActivity?.Kind != "Combat" ||
            string.IsNullOrWhiteSpace(OfflineActivity.ActivityName))
        {
            return null;
        }

        IsOfflineCombatSimulationActive = true;

        return new OfflineCombatLoad(
            OfflineActivity,
            OfflineTicks);
    }

    public void CompleteOfflineCombatSimulation()
    {
        IsOfflineCombatSimulationActive = false;
        OfflineTicks = 0;
        OfflineActivity = null;
        Save();
    }

    public void Reset()
    {
        SaveManager.DeleteSave();

        Player = new Player();
        Player.Inventory.Clear();
        Player.Inventory.InventoryChanged += ScheduleSave;
        Player.EquipmentChanged += ScheduleSave;
        Player.AutoEatSettingsChanged += ScheduleSave;
        Player.CollectionLog.CollectionChanged += ScheduleSave;
        Player.LuckiestDropChanged += ScheduleSave;

        HasStarted = false;
        OfflineTicks = 0;
        OfflineActivity = null;
        OfflineSkillingSummary = null;
        IsOfflineCombatSimulationActive = false;
        LastCombatStyle = CombatStyle.Attack;
    }

    public void Save()
    {
        if (!HasStarted || IsOfflineCombatSimulationActive)
            return;

        lock (_saveLock)
        {
            if (_combatManager != null)
            {
                LastCombatStyle = _combatManager.CurrentCombatStyle;
            }

            SaveManager.Save(
                Player,
                CaptureActiveActivity(),
                LastCombatStyle.ToString());
        }
    }

    private OfflineActivitySaveData? CaptureActiveActivity()
    {
        if (_combatManager?.IsInCombat == true)
        {
            return new OfflineActivitySaveData
            {
                Kind = "Combat",
                ActivityName = _combatManager.CurrentEnemy?.Name ?? "",
                IsAutoFight = _combatManager.IsAutoFightEnabled,
                EnemyCurrentHP = _combatManager.CurrentEnemy?.CurrentHP ?? 0,
                PlayerAttackProgress = _combatManager.PlayerAttackProgress,
                EnemyAttackProgress = _combatManager.EnemyAttackProgress,
                AutoEatCooldownTicks = _combatManager.AutoEatCooldownTicks,
                CombatStyle = _combatManager.CurrentCombatStyle.ToString()
            };
        }

        if (_combatManager?.IsAutoFightRespawning == true)
        {
            return new OfflineActivitySaveData
            {
                Kind = "Combat",
                ActivityName = _combatManager.AutoFightEnemy?.Name ?? "",
                IsAutoFight = _combatManager.IsAutoFightEnabled,
                IsRespawning = true,
                RespawnTicksRemaining = _combatManager.AutoFightTicksRemaining,
                AutoEatCooldownTicks = _combatManager.AutoEatCooldownTicks,
                CombatStyle = _combatManager.CurrentCombatStyle.ToString()
            };
        }

        if (_activityManager?.IsActive == true &&
            _activityManager.CurrentSkill != null &&
            _activityManager.CurrentActivity != null)
        {
            return new OfflineActivitySaveData
            {
                Kind = "Skilling",
                SkillName = _activityManager.CurrentSkill.Name,
                ActivityName = _activityManager.CurrentActivity.Name
            };
        }

        // Loading applies rewards before the live managers exist. Keep the
        // saved activity through that short handoff so it can be resumed.
        return IsOfflineCombatSimulationActive ||
               (_activityManager == null && _combatManager == null)
            ? OfflineActivity
            : null;
    }

    private void ApplyOfflineSkillingProgress()
    {
        if (OfflineTicks <= 0 ||
            OfflineActivity?.Kind != "Skilling" ||
            string.IsNullOrWhiteSpace(OfflineActivity.SkillName) ||
            string.IsNullOrWhiteSpace(OfflineActivity.ActivityName))
        {
            return;
        }

        Skill? skill = Player.GetAllSkills().FirstOrDefault(candidate =>
            candidate.Name == OfflineActivity.SkillName);

        SkillActivity? activity = skill?.Activities.FirstOrDefault(candidate =>
            candidate.Name == OfflineActivity.ActivityName);

        if (skill == null || activity == null || activity.ActionTicks <= 0)
            return;

        long completedActions = (long)Math.Floor(
            OfflineTicks /
            (double)ActivityMetrics.EffectiveActionTicks(activity));

        if (completedActions <= 0)
            return;

        int startingLevel = skill.Level;
        double totalXP = CalculateOfflineSkillXP(
            skill,
            activity.XP,
            completedActions);
        skill.AddXP(totalXP);
        skill.RecordAction(completedActions);

        int rewardQuantity = 0;

        if (activity.ItemReward != null)
        {
            int potentialRewardQuantity = (int)Math.Min(
                completedActions,
                int.MaxValue);

            if (Player.Inventory.AddItem(
                    activity.ItemReward,
                    potentialRewardQuantity))
            {
                rewardQuantity = potentialRewardQuantity;
            }
        }

        Item? pet = TryAwardOfflineSkillingPet(
            skill,
            activity,
            completedActions);

        OfflineSkillingSummary = new OfflineSkillingSummary(
            skill.Name,
            activity.Name,
            completedActions,
            totalXP,
            activity.ItemReward?.Name,
            rewardQuantity,
            pet?.Name,
            startingLevel,
            skill.Level);
    }

    private static double CalculateOfflineSkillXP(
        Skill skill,
        double baseXPPerAction,
        long actionCount)
    {
        if (baseXPPerAction <= 0 || actionCount <= 0)
            return 0;

        double totalXP = 0;
        double currentXP = skill.XP;
        long remainingActions = actionCount;

        // The bonus is level-dependent. Process only the at-most 99 level
        // boundaries rather than simulating every idle action individually.
        while (remainingActions > 0)
        {
            int level = ExperienceTable.GetLevel(currentXP);
            double xpPerAction = baseXPPerAction *
                CombatRules.GetSkillXpMultiplier(level);

            if (level >= 99)
            {
                totalXP += remainingActions * xpPerAction;
                break;
            }

            double xpToNextLevel =
                ExperienceTable.GetXPForLevel(level + 1) - currentXP;
            long actionsToNextLevel = Math.Max(
                1,
                (long)Math.Ceiling(xpToNextLevel / xpPerAction));
            long actionsAtThisLevel = Math.Min(
                remainingActions,
                actionsToNextLevel);

            totalXP += actionsAtThisLevel * xpPerAction;
            currentXP += actionsAtThisLevel * xpPerAction;
            remainingActions -= actionsAtThisLevel;
        }

        return totalXP;
    }

    public OfflineSkillingSummary? TakeOfflineSkillingSummary()
    {
        OfflineSkillingSummary? summary = OfflineSkillingSummary;
        OfflineSkillingSummary = null;
        return summary;
    }

    private Item? TryAwardOfflineSkillingPet(
        Skill skill,
        SkillActivity activity,
        long completedActions)
    {
        SkillingPet? pet = SkillingPetData.GetForSkill(skill);

        if (pet == null ||
            Player.CollectionLog.HasReceivedSkillingPet(pet.Item))
        {
            return null;
        }

        double denominator = SkillingPetData.GetDropDenominator(pet, activity);
        double chanceToReceivePet = 1d - Math.Pow(
            1d - 1d / denominator,
            completedActions);

        if (_offlineProgressRandom.NextDouble() >= chanceToReceivePet)
            return null;

        if (!Player.Inventory.AddItem(pet.Item))
            return null;

        Player.RecordLuckiestDrop(
            pet.Item,
            Math.Max(1, skill.ActionsCompleted),
            1d / denominator,
            $"{skill.Name} actions");
        Player.CollectionLog.RecordSkillingPet(pet.Item);
        return pet.Item;
    }

    public void ScheduleSave()
    {
        // Offline catch-up mutates a large amount of player state in one
        // operation. Completion writes one coherent save; intermediate save
        // snapshots would add work and could observe a background batch.
        if (IsOfflineCombatSimulationActive)
            return;

        lock (_saveLock)
        {
            if (_savePending)
                return;

            _savePending = true;
        }

        _ = SaveAfterDelayAsync();
    }

    private async Task SaveAfterDelayAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));

        lock (_saveLock)
        {
            _savePending = false;
        }

        if (!HasStarted || IsOfflineCombatSimulationActive)
            return;

        long requestId = SaveManager.CreateSaveRequest();

        PlayerSaveData saveData = await MainThread.InvokeOnMainThreadAsync(() =>
        {
            lock (_saveLock)
            {
                if (_combatManager != null)
                    LastCombatStyle = _combatManager.CurrentCombatStyle;

                return SaveManager.CreateSaveData(
                    Player,
                    CaptureActiveActivity(),
                    LastCombatStyle.ToString());
            }
        });

        await SaveManager.SaveAsync(saveData, requestId);
    }
}

public readonly record struct OfflineSkillingSummary(
    string SkillName,
    string ActivityName,
    long CompletedActions,
    double XPGranted,
    string? ItemName,
    int ItemQuantity,
    string? PetName,
    int StartingLevel,
    int EndingLevel);

public readonly record struct OfflineLevelUp(
    string SkillName,
    int StartingLevel,
    int EndingLevel);

public readonly record struct OfflineCombatLoad(
    OfflineActivitySaveData Activity,
    long Ticks);
