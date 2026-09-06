namespace OSRSIdle;

public partial class ActivityBar : ContentView
{
    // ============================================================
    // GAME SYSTEMS
    // ============================================================

    private readonly ActivityManager _activityManager;

    private readonly CombatManager _combatManager;


    // ============================================================
    // TIMER
    // ============================================================

    private IDispatcherTimer? _progressTimer;

    private double _activityProgress;

    private double _combatXPProgress;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public ActivityBar(
        ActivityManager activityManager,
        CombatManager combatManager)
    {
        InitializeComponent();

        _activityManager =
            activityManager;

        _combatManager =
            combatManager;


        _activityManager.ActivityChanged +=
            OnActivityChanged;

        _combatManager.CombatStarted +=
            OnCombatChanged;

        _combatManager.CombatStopped +=
            OnCombatChanged;

        ActivityProgressBar.SizeChanged +=
            (sender, e) => SetActivityProgress(_activityProgress);

        CombatXPBar.SizeChanged +=
            (sender, e) => SetCombatXPProgress(_combatXPProgress);

        PlayerMiniHPBar.SizeChanged +=
            (sender, e) => UpdateCombatMiniHPBars();

        EnemyMiniHPBar.SizeChanged +=
            (sender, e) => UpdateCombatMiniHPBars();


        UpdateDisplay();

        StartProgressTimer();
    }


    // ============================================================
    // ACTIVITY CHANGED
    // ============================================================

    private void OnActivityChanged(
        object? sender,
        EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateDisplay();
        });
    }


    // ============================================================
    // COMBAT CHANGED
    // ============================================================

    private void OnCombatChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateDisplay();
        });
    }


    // ============================================================
    // PROGRESS TIMER
    // ============================================================

    private void StartProgressTimer()
    {
        _progressTimer =
            Dispatcher.CreateTimer();

        _progressTimer.Interval =
            TimeSpan.FromMilliseconds(100);

        _progressTimer.Tick +=
            (sender, e) =>
            {
                if (_combatManager.IsAutoFightRespawning)
                {
                    UpdateDisplay();
                }
                else if (_combatManager.IsInCombat)
                {
                    UpdateCombatMiniHPBars();
                    UpdateCombatXP();
                }
                else
                {
                    UpdateProgress();
                }
            };

        _progressTimer.Start();
    }


    // ============================================================
    // UPDATE DISPLAY
    // ============================================================

    private void UpdateDisplay()
    {
        // --------------------------------------------------------
        // AUTO-FIGHT RESPAWN
        // --------------------------------------------------------

        if (_combatManager.IsAutoFightRespawning)
        {
            Enemy? enemy =
                _combatManager.AutoFightEnemy;

            int ticksRemaining =
                _combatManager.AutoFightTicksRemaining;

            ActivityTitle.Text =
                "COMBAT";

            ActivityTitle.IsVisible =
                false;

            ActivityNameRow.IsVisible =
                true;

            CombatStatusRows.IsVisible =
                false;

            ActivityName.Text =
                enemy == null
                    ? $"⚔️ Respawning in {ticksRemaining} tick{(ticksRemaining == 1 ? "" : "s")}..."
                    : $"⚔️ {enemy.Name} respawning in {ticksRemaining} tick{(ticksRemaining == 1 ? "" : "s")}...";

            UpdateAutoFightLootDisplay();

            ActivityProgressBar.IsVisible =
                false;

            ActivityTime.Text =
                "";

            CombatStylePanel.IsVisible =
                true;

            StopButton.IsVisible =
                false;

            SkillStopButton.IsVisible =
                false;

            UpdateCombatStyleButtons();
            UpdateCombatXP();

            return;
        }


        // --------------------------------------------------------
        // COMBAT MODE
        // --------------------------------------------------------

        if (_combatManager.IsInCombat)
        {
            AutoFightLootLabel.IsVisible =
                false;

            ActivityTitle.Text =
                "COMBAT";

            ActivityTitle.IsVisible =
                false;

            Enemy? enemy =
                _combatManager.CurrentEnemy;

            ActivityNameRow.IsVisible =
                false;

            CombatStatusRows.IsVisible =
                enemy != null;

            UpdateCombatMiniHPBars();

            // Combat no longer uses the activity progress bar as an
            // attack countdown.
            ActivityProgressBar.IsVisible =
                false;


            CombatStylePanel.IsVisible =
                true;

            StopButton.IsVisible =
                true;

            SkillStopButton.IsVisible =
                false;


            UpdateCombatStyleButtons();

            UpdateCombatXP();

            return;
        }


        // --------------------------------------------------------
        // NORMAL ACTIVITY MODE
        // --------------------------------------------------------

        CombatStylePanel.IsVisible =
            false;

        AutoFightLootLabel.IsVisible =
            false;

        ActivityProgressBar.IsVisible =
            true;

        CombatStatusRows.IsVisible =
            false;

        ActivityNameRow.IsVisible =
            true;

        ActivityTitle.Text =
            "CURRENT ACTIVITY";

        ActivityTitle.IsVisible =
            true;


        if (!_activityManager.IsActive ||
            _activityManager.CurrentSkill == null ||
            _activityManager.CurrentActivity == null)
        {
            ActivityName.Text =
                "No activity";

            SetActivityProgress(0);

            ActivityTime.Text =
                "";

            StopButton.IsVisible =
                false;

            SkillStopButton.IsVisible =
                false;

            return;
        }


        Skill skill =
            _activityManager.CurrentSkill;

        SkillActivity activity =
            _activityManager.CurrentActivity;


        ActivityName.Text =
            $"{skill.Icon} {skill.Name} " +
            $"(Lvl. {skill.Level}) — " +
            $"{activity.Name}";


        StopButton.IsVisible =
            false;

        SkillStopButton.IsVisible =
            true;


        UpdateProgress();
    }


    // ============================================================
    // AUTO-FIGHT LOOT
    // ============================================================

    private void UpdateAutoFightLootDisplay()
    {
        if (_combatManager.LastLoot.Count == 0)
        {
            AutoFightLootLabel.FormattedText = null;
            AutoFightLootLabel.Text = "";
            AutoFightLootLabel.IsVisible = false;
            return;
        }

        FormattedString lootText = new();
        lootText.Spans.Add(
            new Span
            {
                Text = "Loot: ",
                TextColor = Color.FromArgb("#D99032")
            });

        for (int index = 0; index < _combatManager.LastLoot.Count; index++)
        {
            LootResult loot = _combatManager.LastLoot[index];
            if (index > 0)
            {
                lootText.Spans.Add(new Span { Text = ", " });
            }

            lootText.Spans.Add(
                new Span
                {
                    Text = $"{loot.Item.Name} x{loot.Quantity}",
                    TextColor = GameThemeCache.GetRarityColor(loot.Rarity)
                });
        }

        AutoFightLootLabel.Text = "";
        AutoFightLootLabel.FormattedText = lootText;
        AutoFightLootLabel.IsVisible = true;
    }


    // ============================================================
    // UPDATE COMBAT STYLE BUTTONS
    // ============================================================

    private void UpdateCombatStyleButtons()
    {
        AttackStyleButton.Text =
            $"Attack {_combatManager.Player.Attack.Level}";

        StrengthStyleButton.Text =
            $"Strength {_combatManager.Player.Strength.Level}";

        DefenseStyleButton.Text =
            $"Defense {_combatManager.Player.Defense.Level}";

        SetCombatStyleButtonAppearance(
            AttackStyleButton,
            _combatManager.CurrentCombatStyle == CombatStyle.Attack);

        SetCombatStyleButtonAppearance(
            StrengthStyleButton,
            _combatManager.CurrentCombatStyle == CombatStyle.Strength);

        SetCombatStyleButtonAppearance(
            DefenseStyleButton,
            _combatManager.CurrentCombatStyle == CombatStyle.Defense);
    }


    private static void SetCombatStyleButtonAppearance(
        GoldSliceButton button,
        bool isSelected)
    {
        button.FontAttributes =
            isSelected
                ? FontAttributes.Bold
                : FontAttributes.None;

        button.Variant = isSelected
            ? GoldSliceButtonVariant.Green
            : GoldSliceButtonVariant.Neutral;

        button.TextColor =
            Colors.White;
    }


    // ============================================================
    // CUSTOM PROGRESS BARS
    // ============================================================

    private void SetActivityProgress(
        double progress)
    {
        _activityProgress =
            Math.Clamp(progress, 0, 1);

        UpdateCustomProgressBar(
            ActivityProgressBar,
            ActivityProgressFill,
            _activityProgress,
            alwaysGreen: true);
    }


    private void SetCombatXPProgress(
        double progress)
    {
        _combatXPProgress =
            Math.Clamp(progress, 0, 1);

        UpdateCustomProgressBar(
            CombatXPBar,
            CombatXPFill,
            _combatXPProgress);
    }


    private static void UpdateCustomProgressBar(
        Grid progressBar,
        BoxView progressFill,
        double progress,
        bool alwaysGreen = false)
    {
        progressFill.BackgroundColor =
            alwaysGreen
                ? Colors.Green
                : progress <= 0.30
                    ? Colors.Red
                    : progress <= 0.70
                        ? Colors.Yellow
                        : Colors.Green;

        progressFill.WidthRequest =
            progressBar.Width * progress;
    }

    private void UpdateCombatMiniHPBars()
    {
        Enemy? enemy =
            _combatManager.CurrentEnemy;

        if (enemy == null)
            return;

        int playerMaxHP =
            Math.Max(
                1,
                _combatManager.Player.GetMaxHP());

        double playerProgress =
            (double)_combatManager.Player.CurrentHP /
            playerMaxHP;

        double enemyProgress =
            (double)enemy.CurrentHP /
            Math.Max(1, enemy.HP);

        PlayerMiniHPLabel.Text = "Your HP";

        EnemyMiniNameLabel.Text =
            $"{enemy.Name} lvl {enemy.CombatLevel}";

        PlayerMiniHPValueLabel.Text =
            $"{_combatManager.Player.CurrentHP}/{playerMaxHP}";

        EnemyMiniHPLabel.Text = "Enemy HP";

        EnemyMiniHPValueLabel.Text =
            $"{enemy.CurrentHP}/{enemy.HP}";

        UpdateCustomProgressBar(
            PlayerMiniHPBar,
            PlayerMiniHPFill,
            playerProgress);

        UpdateCustomProgressBar(
            EnemyMiniHPBar,
            EnemyMiniHPFill,
            enemyProgress);
    }


    // ============================================================
    // UPDATE PROGRESS
    // ============================================================

    private void UpdateProgress()
    {
        // --------------------------------------------------------
        // NORMAL ACTIVITY
        // --------------------------------------------------------

        if (!_activityManager.IsActive ||
            _activityManager.CurrentActivity == null)
        {
            SetActivityProgress(0);

            ActivityTime.Text =
                "";

            return;
        }


        DateTime now =
            DateTime.UtcNow;

        DateTime start =
            _activityManager.ActionStarted;

        DateTime end =
            _activityManager.ActionEnds;


        double totalSeconds =
            (end - start).TotalSeconds;

        double elapsedSeconds =
            (now - start).TotalSeconds;


        if (totalSeconds <= 0)
        {
            SetActivityProgress(1);

            return;
        }


        double progress =
            elapsedSeconds /
            totalSeconds;


        progress =
            Math.Clamp(
                progress,
                0,
                1);


        SetActivityProgress(progress);


        int remainingTicks = Math.Max(
            0,
            (int)Math.Ceiling((end - now).TotalMilliseconds / 600d));

        ActivityTime.Text =
            $"{remainingTicks} tick{(remainingTicks == 1 ? "" : "s")}";
    }


    // ============================================================
    // ATTACK STYLE
    // ============================================================

    private void OnAttackStyleClicked(
    object sender,
    EventArgs e)
    {
        _combatManager.SetCombatStyle(
            CombatStyle.Attack);

        UpdateCombatStyleButtons();

        UpdateCombatXP();
    }


    // ============================================================
    // STRENGTH STYLE
    // ============================================================

    private void OnStrengthStyleClicked(
    object sender,
    EventArgs e)
    {
        _combatManager.SetCombatStyle(
            CombatStyle.Strength);

        UpdateCombatStyleButtons();

        UpdateCombatXP();
    }


    // ============================================================
    // DEFENSE STYLE
    // ============================================================

    private void OnDefenseStyleClicked(
    object sender,
    EventArgs e)
    {
        _combatManager.SetCombatStyle(
            CombatStyle.Defense);

        UpdateCombatStyleButtons();

        UpdateCombatXP();
    }


    // ============================================================
    // STOP
    // ============================================================

    private void OnStopClicked(
        object sender,
        EventArgs e)
    {
        if (_combatManager.IsInCombat)
        {
            _combatManager.AbortCombatEncounter();

            UpdateDisplay();

            return;
        }

        // "Run" is also the escape hatch from an auto-fight respawn. Clear
        // every auto-fight flag so the combat button returns to its neutral
        // state instead of remaining green for the next encounter.
        _combatManager.SetAutoFightEnabled(false);
        _combatManager.ClearAutoFightRespawn();
        _activityManager.StopActivity();
        UpdateDisplay();
    }
// ============================================================
// UPDATE COMBAT XP
// ============================================================

private void UpdateCombatXP()
    {
        Skill skill;

        switch (_combatManager.CurrentCombatStyle)
        {
            case CombatStyle.Strength:

                skill =
                    _combatManager.Player.Strength;

                break;


            case CombatStyle.Defense:

                skill =
                    _combatManager.Player.Defense;

                break;


            default:

                skill =
                    _combatManager.Player.Attack;

                break;
        }


        // --------------------------------------------------------
        // Current level XP.
        // --------------------------------------------------------

        int currentLevelXP =
            ExperienceTable.GetXPForLevel(
                skill.Level);


        // --------------------------------------------------------
        // Next level XP.
        // --------------------------------------------------------

        int nextLevelXP =
            skill.Level >= 99
                ? currentLevelXP
                : ExperienceTable.GetXPForLevel(
                    skill.Level + 1);


        // --------------------------------------------------------
        // XP earned within the current level.
        // --------------------------------------------------------

        double levelProgressXP =
            skill.XP -
            currentLevelXP;


        double levelRequiredXP =
            nextLevelXP -
            currentLevelXP;


        double progress =
            levelRequiredXP > 0
                ? levelProgressXP /
                  levelRequiredXP
                : 1;


        progress =
            Math.Clamp(
                progress,
                0,
                1);


        // --------------------------------------------------------
        // Display.
        // --------------------------------------------------------

        CombatXPLabel.Text = $"{skill.Name} XP";


        SetCombatXPProgress(progress);


        if (skill.Level >= 99)
        {
            CombatXPAmountLabel.Text =
                $"{skill.XP:0} XP — MAX LEVEL";
        }
        else
        {
            CombatXPAmountLabel.Text =
                $"{skill.XP:0} / {nextLevelXP} XP";
        }
    }
}
