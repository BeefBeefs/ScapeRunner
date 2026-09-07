namespace OSRSIdle;

public partial class ActivityBar : ContentView, IDisposable
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

    private readonly EventHandler _progressTimerTick;
    private int _combatUpdatePending;
    private bool _disposed;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public ActivityBar(
        ActivityManager activityManager,
        CombatManager combatManager)
    {
        InitializeComponent();

        _progressTimerTick = OnProgressTimerTick;

        _activityManager =
            activityManager;

        _combatManager =
            combatManager;


        _activityManager.ActivityStateChanged +=
            OnActivityChanged;

        _combatManager.CombatStarted +=
            OnCombatChanged;

        _combatManager.CombatStopped +=
            OnCombatChanged;

        _combatManager.CombatUpdated +=
            OnCombatUpdated;

        ActivityProgressBar.SizeChanged += OnActivityProgressBarSizeChanged;
        CombatXPBar.SizeChanged += OnCombatXPBarSizeChanged;
        PlayerMiniHPBar.SizeChanged += OnMiniHPBarSizeChanged;
        EnemyMiniHPBar.SizeChanged += OnMiniHPBarSizeChanged;


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
            if (_disposed)
                return;

            UpdateDisplay();
        });
    }

    private void OnCombatUpdated()
    {
        if (_disposed ||
            Interlocked.Exchange(ref _combatUpdatePending, 1) != 0)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Interlocked.Exchange(ref _combatUpdatePending, 0);

            if (_disposed)
                return;

            if (_combatManager.IsAutoFightRespawning)
                UpdateDisplay();
            else if (_combatManager.IsInCombat)
            {
                UpdateCombatMiniHPBars();
                UpdateCombatXP();
            }
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
            TimeSpan.FromMilliseconds(200);

        _progressTimer.Tick += _progressTimerTick;

        _progressTimer.Start();
    }

    private void OnProgressTimerTick(object? sender, EventArgs e)
    {
        if (_combatManager.IsAutoFightRespawning ||
            _combatManager.IsInCombat)
        {
            return;
        }

        UpdateProgress();
    }

    private void OnActivityProgressBarSizeChanged(object? sender, EventArgs e) =>
        SetActivityProgress(_activityProgress);

    private void OnCombatXPBarSizeChanged(object? sender, EventArgs e) =>
        SetCombatXPProgress(_combatXPProgress);

    private void OnMiniHPBarSizeChanged(object? sender, EventArgs e) =>
        UpdateCombatMiniHPBars();

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _progressTimer?.Stop();
        if (_progressTimer != null)
            _progressTimer.Tick -= _progressTimerTick;

        _activityManager.ActivityStateChanged -= OnActivityChanged;
        _combatManager.CombatStarted -= OnCombatChanged;
        _combatManager.CombatStopped -= OnCombatChanged;
        _combatManager.CombatUpdated -= OnCombatUpdated;
        ActivityProgressBar.SizeChanged -= OnActivityProgressBarSizeChanged;
        CombatXPBar.SizeChanged -= OnCombatXPBarSizeChanged;
        PlayerMiniHPBar.SizeChanged -= OnMiniHPBarSizeChanged;
        EnemyMiniHPBar.SizeChanged -= OnMiniHPBarSizeChanged;
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
        Color fillColor =
            alwaysGreen
                ? Colors.Green
                : progress <= 0.30
                    ? Colors.Red
                    : progress <= 0.70
                        ? Colors.Yellow
                        : Colors.Green;

        if (!Equals(progressFill.BackgroundColor, fillColor))
            progressFill.BackgroundColor = fillColor;

        double fillWidth = Math.Max(0, progressBar.Width * progress);
        if (Math.Abs(progressFill.WidthRequest - fillWidth) > 0.5)
            progressFill.WidthRequest = fillWidth;
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

        if (PlayerMiniHPLabel.Text != "Your HP")
            PlayerMiniHPLabel.Text = "Your HP";

        string enemyName = $"{enemy.Name} lvl {enemy.CombatLevel}";
        if (EnemyMiniNameLabel.Text != enemyName)
            EnemyMiniNameLabel.Text = enemyName;

        string playerHP = $"{_combatManager.Player.CurrentHP}/{playerMaxHP}";
        if (PlayerMiniHPValueLabel.Text != playerHP)
            PlayerMiniHPValueLabel.Text = playerHP;

        if (EnemyMiniHPLabel.Text != "Enemy HP")
            EnemyMiniHPLabel.Text = "Enemy HP";

        string enemyHP = $"{enemy.CurrentHP}/{enemy.HP}";
        if (EnemyMiniHPValueLabel.Text != enemyHP)
            EnemyMiniHPValueLabel.Text = enemyHP;

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
            (int)Math.Ceiling(
                (end - now).TotalMilliseconds *
                GameClock.SpeedMultiplier /
                GameClock.StandardTickMilliseconds));

        ActivityTime.Text =
            $"{remainingTicks} tick{(remainingTicks == 1 ? "" : "s")}";
    }


    // ============================================================
    // ATTACK STYLE
    // ============================================================

    private void OnAttackStyleClicked(
    object? sender,
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
    object? sender,
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
    object? sender,
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
        object? sender,
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
