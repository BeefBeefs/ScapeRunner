namespace OSRSIdle;

public partial class GamePage : ContentPage
{
    // ============================================================
    // GAME SYSTEMS
    // ============================================================

    public Player Player { get; }

    public ActivityManager ActivityManager { get; }

    public CombatManager CombatManager { get; }

    private readonly Game _game;

    // Kept alive while switching tabs so auto-fight state and its UI
    // remain in sync when the player returns to combat.
    private CombatView? _combatView;

    private CollectionLogView? _collectionLogView;

    private HomeView? _homeView;

    private InventoryView? _inventoryView;

    private SkillsView? _skillsView;

    private readonly Dictionary<Skill, SkillPage> _skillPages = new();

    private SettingsView? _settingsView;

    private ActivityBar? _activityBar;

    private readonly NotificationQueue _notificationQueue = new();

    private bool _disposed;


    // ============================================================
    // PLAYER DEATH
    // ============================================================

    private CancellationTokenSource? _deathCancellation;

    private const int DeathRespawnTicks = 60;

    private const int HealthRegenerationTicks = 100;

    private IDispatcherTimer? _healthRegenerationTimer;

    private int _healthRegenerationTickCount;

    private IDispatcherTimer? _fpsTimer;

    private DateTime _fpsWindowStartedUtc = DateTime.UtcNow;

    private int _fpsFrameCount;

    private bool _offlineSummaryShown;

    private bool _offlineCombatPlayerDied;

    private Enemy? _offlineAutoFightEnemy;

    private long _offlineRespawnAccelerationTicks;

    private TaskCompletionSource<string?>? _customDialogCompletion;

    private bool _customDialogDismissOnBackground;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public GamePage()
    {
        InitializeComponent();

        ApplyCachedLayoutMetrics();
        SizeChanged += OnGamePageSizeChanged;


        // --------------------------------------------------------
        // Create player.
        // --------------------------------------------------------

        App app = (App)Application.Current!;
        _game = app.Game;

        CustomDialogService.SetHost(this);

        Player = _game.Player;


        // --------------------------------------------------------
        // Create game systems.
        // --------------------------------------------------------

        ActivityManager =
            new ActivityManager(
                Player);

        (CombatManager? preloadedCombatManager,
            CombatView? preloadedCombatView) =
            app.TakePreloadedCombatSession();

        CombatManager = preloadedCombatManager ??
            new CombatManager(Player);
        _combatView = preloadedCombatView;

        _game.SetActivityManagers(
            ActivityManager,
            CombatManager);

        _game.ResumeSavedSkillingActivity(ActivityManager);

        StartHealthRegenerationTimer();
        StartFpsCounterTimer();


        // --------------------------------------------------------
        // Create persistent Activity Bar.
        // --------------------------------------------------------

        _activityBar = new ActivityBar(
            ActivityManager,
            CombatManager);
        ActivityBarContainer.Content = _activityBar;


        // --------------------------------------------------------
        // Listen for game events.
        // --------------------------------------------------------

        ActivityManager.ActionCompleted += OnActivityXPChanged;

        ActivityManager.LevelUp +=
            OnLevelUp;

        ActivityManager.ActivityStateChanged +=
            OnActivityChanged;

        CombatManager.PlayerDefeated +=
            OnPlayerDefeated;

        CombatManager.XPChanged +=
            _game.ScheduleSave;

        CombatManager.EnemyDefeated +=
            OnEnemyDefeated;

        Player.CollectionLog.CollectionCompleted +=
            OnCollectionCompleted;

        CombatManager.LevelUp +=
            OnLevelUp;

        CombatManager.CombatStarted +=
            OnCombatStarted;


        // --------------------------------------------------------
        // Start on home page.
        // --------------------------------------------------------

        ShowHomePage();

        Loaded += OnGamePageLoaded;
    }

    private void OnGamePageSizeChanged(object? sender, EventArgs e)
    {
        if (UiLayoutMetrics.Update(Width, Height))
            ApplyCachedLayoutMetrics();
    }

    private void ApplyCachedLayoutMetrics()
    {
        NavigationBar.HeightRequest = UiLayoutMetrics.NavigationBarHeight;
        NavigationButtonsGrid.Padding = new Thickness(
            UiLayoutMetrics.CommonSpacing / 2);

        foreach (Border button in NavigationButtonsGrid.Children.OfType<Border>())
        {
            if (button.Content is not Image icon)
                continue;

            icon.WidthRequest = UiLayoutMetrics.NavigationIconSize;
            icon.HeightRequest = UiLayoutMetrics.NavigationIconSize;
        }
    }

    private void StartHealthRegenerationTimer()
    {
        _healthRegenerationTimer = Dispatcher.CreateTimer();
        _healthRegenerationTimer.Interval =
            GameClock.TickInterval;

        GameClock.SpeedChanged += OnGameSpeedChanged;

        _healthRegenerationTimer.Tick += OnHealthRegenerationTick;

        _healthRegenerationTimer.Start();
    }

    private void OnGameSpeedChanged(object? sender, EventArgs e)
    {
        if (_healthRegenerationTimer != null)
            _healthRegenerationTimer.Interval = GameClock.TickInterval;
    }

    private void OnActivityXPChanged(object? sender, EventArgs e)
    {
        _game.ScheduleSave();
    }

    private void OnHealthRegenerationTick(object? sender, EventArgs e)
    {
        _healthRegenerationTickCount++;

        if (_healthRegenerationTickCount < HealthRegenerationTicks)
            return;

        _healthRegenerationTickCount = 0;

        if (Player.CurrentHP >= Player.GetMaxHP())
            return;

        Player.CurrentHP = Math.Min(
            Player.GetMaxHP(),
            Player.CurrentHP + 1);

        _game.ScheduleSave();
    }

    private void StartFpsCounterTimer()
    {
        _fpsTimer = Dispatcher.CreateTimer();
        _fpsTimer.Interval = TimeSpan.FromMilliseconds(16);
        _fpsTimer.Tick += OnFpsTimerTick;
        _fpsTimer.Start();
    }

    private void OnFpsTimerTick(object? sender, EventArgs e)
    {
        _fpsFrameCount++;

        DateTime now = DateTime.UtcNow;
        double elapsedSeconds =
            (now - _fpsWindowStartedUtc).TotalSeconds;

        if (elapsedSeconds < 1d)
            return;

        double framesPerSecond = _fpsFrameCount / elapsedSeconds;
        FpsDebugLabel.Text = $"FPS: {framesPerSecond:0}";
        _fpsFrameCount = 0;
        _fpsWindowStartedUtc = now;
    }

    private async void OnGamePageLoaded(
        object? sender,
        EventArgs e)
    {
        if (_offlineSummaryShown)
            return;

        _offlineSummaryShown = true;

        // Offline catch-up always presents over the Home page. This keeps the
        // character portrait and navigation context visible instead of
        // showing the page's plain background while the simulation runs.
        ShowHomePage();

        // The Home view is attached during construction, but Android has not
        // necessarily committed its first layout when Loaded fires. Give it a
        // layout and render turn before presenting a catch-up overlay so the
        // page remains visible behind the dialog.
        await WaitForInitialGameContentRenderAsync();

        OfflineCombatLoad? combatLoad =
            _game.BeginOfflineCombatSimulation();

        if (combatLoad != null)
        {
            await RunOfflineCombatSimulationAsync(combatLoad.Value);
            return;
        }

        OfflineSkillingSummary? summary =
            _game.TakeOfflineSkillingSummary();

        if (summary == null)
            return;

        ShowOfflineSkillingSummary(summary.Value);
    }

    private async Task WaitForInitialGameContentRenderAsync()
    {
        if (GameContent.Width <= 0 || GameContent.Height <= 0)
        {
            TaskCompletionSource layoutReady = new();

            void OnContentSizeChanged(object? sender, EventArgs e)
            {
                if (GameContent.Width <= 0 || GameContent.Height <= 0)
                    return;

                GameContent.SizeChanged -= OnContentSizeChanged;
                layoutReady.TrySetResult();
            }

            GameContent.SizeChanged += OnContentSizeChanged;

            // Cover the small interval between the first size check and
            // subscribing to SizeChanged.
            OnContentSizeChanged(this, EventArgs.Empty);
            await layoutReady.Task;
        }

        await Task.Yield();
        await Task.Delay(16);
    }

    private async Task RunOfflineCombatSimulationAsync(
        OfflineCombatLoad combatLoad)
    {
        OfflineCombatSimulation simulation;

        try
        {
            simulation = new OfflineCombatSimulation(
                Player,
                combatLoad.Activity);
        }
        catch
        {
            _game.CompleteOfflineCombatSimulation();
            return;
        }

        OfflineCombatOverlay.IsVisible = true;
        OfflineCombatTitleLabel.Text =
            $"SIMULATING {simulation.Enemy.Name.ToUpperInvariant()}";
        OfflineCombatResultLabel.Text = "";
        OfflineCombatLootLabel.Text = "No loot yet.";
        OfflineCombatContinueButton.IsEnabled = false;
        OfflineCombatContinueButton.Text = "Simulating...";

        await Task.Yield();

        const long ticksPerBatch = 2_000;

        while (simulation.TicksProcessed < combatLoad.Ticks &&
               !simulation.IsComplete)
        {
            long ticksThisBatch = Math.Min(
                ticksPerBatch,
                combatLoad.Ticks - simulation.TicksProcessed);

            simulation.Advance(ticksThisBatch);

            UpdateOfflineCombatSimulationDisplay(
                simulation,
                combatLoad.Ticks);

            // Yield between large batches so the progress window remains
            // responsive while still replaying far faster than real time.
            await Task.Delay(16);
        }

        UpdateOfflineCombatSimulationDisplay(
            simulation,
            combatLoad.Ticks);

        string simulationResult = simulation.PlayerDied
            ? BuildOfflineCombatDeathMessage(simulation)
            : simulation.IsComplete
                ? "Combat ended before all saved ticks were used."
                : BuildOfflineCombatXPMessage(simulation);

        OfflineCombatResultLabel.Text = simulationResult +
            BuildLevelUpSummary(simulation.GetLevelUps());

        _offlineCombatPlayerDied = simulation.PlayerDied;
        _offlineAutoFightEnemy = !simulation.PlayerDied && simulation.AutoFight
            ? simulation.Enemy
            : null;
        _offlineRespawnAccelerationTicks = simulation.PlayerDied
            ? Math.Max(0, combatLoad.Ticks - simulation.TicksProcessed)
            : 0;

        OfflineCombatContinueButton.Text = simulation.PlayerDied
            ? "Continue to respawn"
            : "Continue";
        OfflineCombatContinueButton.Variant = GoldSliceButtonVariant.Green;
        OfflineCombatContinueButton.IsEnabled = true;

        _game.CompleteOfflineCombatSimulation();
    }

    private void UpdateOfflineCombatSimulationDisplay(
        OfflineCombatSimulation simulation,
        long totalTicks)
    {
        OfflineCombatProgressLabel.Text =
            $"Kills: {simulation.Kills:N0}  •  " +
            $"Ticks: {simulation.TicksProcessed:N0} / {totalTicks:N0}";

        OfflineCombatProgressBar.Progress = totalTicks <= 0
            ? 1
            : Math.Clamp(
                (double)simulation.TicksProcessed / totalTicks,
                0,
                1);

        if (simulation.Loot.Count == 0)
        {
            OfflineCombatLootLabel.FormattedText = null;
            OfflineCombatLootLabel.Text = "No loot yet.";
            OfflineCombatLootLabel.TextColor = Color.FromArgb("#FFE26A");
            return;
        }

        FormattedString lootText = new();

        foreach (KeyValuePair<Item, long> entry in simulation.Loot
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key.Name))
        {
            lootText.Spans.Add(new Span
            {
                Text =
                    $"{(simulation.NewCollectionItems.Contains(entry.Key) ? "*NEW* " : "")}" +
                    $"{entry.Key.Icon} {entry.Key.Name} × {entry.Value:N0}\n",
                TextColor = GetRarityColor(simulation.LootRarities[entry.Key])
            });
        }

        OfflineCombatLootLabel.Text = "";
        OfflineCombatLootLabel.FormattedText = lootText;
    }

    private async void OnOfflineCombatContinueClicked(
        object? sender,
        EventArgs e)
    {
        // Prevent double taps and let the overlay repaint before constructing
        // or switching to the next page. This keeps the Continue tap smooth
        // after a long offline simulation.
        OfflineCombatContinueButton.IsEnabled = false;
        OfflineCombatOverlay.IsVisible = false;
        await Task.Yield();

        if (_offlineCombatPlayerDied)
        {
            ShowHomePage();
            _offlineCombatPlayerDied = false;
            long accelerationTicks = _offlineRespawnAccelerationTicks;
            _offlineRespawnAccelerationTicks = 0;

            MainThread.BeginInvokeOnMainThread(
                async () => await ShowDeathScreenAsync(accelerationTicks));

            return;
        }

        Enemy? autoFightEnemy = _offlineAutoFightEnemy;
        _offlineAutoFightEnemy = null;

        if (autoFightEnemy != null)
        {
            ShowCombatPage();
            _combatView?.ResumeAutoFight(autoFightEnemy);
            return;
        }

        ShowHomePage();
    }

    private static string BuildOfflineCombatXPMessage(
        OfflineCombatSimulation simulation)
    {
        double hours = simulation.TicksProcessed * ActivityMetrics.SecondsPerTick / 3600d;
        string rate = hours > 0
            ? $"\nAverage: {simulation.Kills / hours:0.0} kills/hr • " +
              $"{(simulation.HPXPGranted + simulation.StyleXPGranted) / hours:0} XP/hr"
            : string.Empty;
        return "Offline combat simulation complete!\n" +
               $"Combat XP: {simulation.HPXPGranted:N0} HP, " +
               $"{simulation.StyleXPGranted:N0} " +
               $"{simulation.CombatStyle} XP" + rate;
    }

    private static string BuildOfflineCombatDeathMessage(
        OfflineCombatSimulation simulation)
    {
        string enemyName = simulation.Enemy.Name;
        string enemyLabel = simulation.Kills == 1
            ? enemyName
            : $"{enemyName}s";

        double hours = simulation.TicksProcessed * ActivityMetrics.SecondsPerTick / 3600d;
        string rate = hours > 0 ? $"\nAverage: {simulation.Kills / hours:0.0} kills/hr" : string.Empty;
        return $"You died after killing {simulation.Kills:N0} {enemyLabel}.\n" +
               "Your offline combat run has ended." + rate;
    }

    private void ShowOfflineSkillingSummary(
        OfflineSkillingSummary summary)
    {
        OfflineSkillingSummaryLabel.Text =
            BuildOfflineSummaryMessage(summary);

        FormattedString rewards = new();

        rewards.Spans.Add(new Span
        {
            Text = $"{summary.XPGranted:N0} {summary.SkillName} XP\n",
            TextColor = Color.FromArgb("#FFE26A")
        });

        if (summary.EndingLevel > summary.StartingLevel)
        {
            rewards.Spans.Add(new Span
            {
                Text = $"Level up! {summary.SkillName} " +
                       $"{summary.StartingLevel}->{summary.EndingLevel}\n",
                TextColor = Color.FromArgb("#62C7FF")
            });
        }

        if (!string.IsNullOrWhiteSpace(summary.ItemName) &&
            summary.ItemQuantity > 0)
        {
            rewards.Spans.Add(new Span
            {
                Text = $"{summary.ItemQuantity:N0} {summary.ItemName}\n",
                TextColor = Color.FromArgb("#FFE26A")
            });
        }

        Skill? skill = Player.Skills.FirstOrDefault(item => item.Name == summary.SkillName);
        SkillActivity? activity = skill?.Activities.FirstOrDefault(item => item.Name == summary.ActivityName);
        if (activity != null)
        {
            rewards.Spans.Add(new Span
            {
                Text = $"Rate: {ActivityMetrics.FormatRate(ActivityMetrics.XpPerHour(activity))} XP/hr" +
                       (activity.ItemReward == null
                           ? "\n"
                           : $" • {ActivityMetrics.FormatRate(ActivityMetrics.ItemsPerHour(activity))}/hr {activity.ItemReward.Name}\n"),
                TextColor = Color.FromArgb("#62C7FF")
            });
        }

        if (!string.IsNullOrWhiteSpace(summary.PetName))
        {
            rewards.Spans.Add(new Span { Text = "*NEW* Skilling pet: " });
            FormattedString petText = RarityVisuals.RainbowText($"{summary.PetName}!");
            foreach (Span span in petText.Spans)
                rewards.Spans.Add(span);
        }

        OfflineSkillingRewardsLabel.Text = "";
        OfflineSkillingRewardsLabel.FormattedText = rewards;
        OfflineSkillingOverlay.IsVisible = true;
    }

    private static Color GetRarityColor(DropRarity rarity)
    {
        return GameThemeCache.GetRarityColor(rarity);
    }

    private static string BuildLevelUpSummary(
        IReadOnlyList<OfflineLevelUp> levelUps)
    {
        if (levelUps.Count == 0)
            return "";

        return "\n" + string.Join(
            "\n",
            levelUps.Select(levelUp =>
                $"Level up! {levelUp.SkillName} " +
                $"{levelUp.StartingLevel}->{levelUp.EndingLevel}"));
    }

    private async void OnOfflineSkillingContinueClicked(
        object? sender,
        EventArgs e)
    {
        OfflineSkillingOverlay.IsVisible = false;
        await Task.Yield();
    }

    public Task<string?> ShowCustomDialogAsync(
        string title,
        string message,
        IReadOnlyList<string> buttons,
        string? itemImageSource = null,
        Color? titleColor = null)
    {
        if (_customDialogCompletion != null)
            return Task.FromResult<string?>(null);

        CustomDialogTitleLabel.Text = title;
        CustomDialogTitleLabel.TextColor = titleColor ??
            Color.FromArgb("#2D7D46");
        int upgradeMarker = title.LastIndexOf(" +", StringComparison.Ordinal);
        int upgradeLevel = 0;
        bool hasUpgrade = upgradeMarker >= 0 &&
                          int.TryParse(title[(upgradeMarker + 2)..], out upgradeLevel);

        CustomDialogMessageLabel.Text = itemImageSource == null
            ? message
            : null;
        CustomDialogMessageLabel.FormattedText = hasUpgrade
            ? BuildDialogFormattedText(
                message.Replace($"\nUpgrade level: +{upgradeLevel}", string.Empty, StringComparison.Ordinal),
                itemImageSource != null)
            : itemImageSource != null
                ? BuildDialogFormattedText(message, true)
                : null;
        CustomDialogUpgradeLabel.Text = hasUpgrade
            ? $"Upgrade level: +{upgradeLevel}"
            : string.Empty;
        CustomDialogUpgradeLabel.IsVisible = hasUpgrade;
        CustomDialogItemImage.Source = itemImageSource;
        CustomDialogItemImageFrame.IsVisible =
            !string.IsNullOrWhiteSpace(itemImageSource);
        _customDialogDismissOnBackground =
            !string.IsNullOrWhiteSpace(itemImageSource);
        CustomDialogButtons.Children.Clear();

        foreach (string buttonText in buttons)
        {
            GoldSliceButton button = new()
            {
                Text = buttonText,
                Variant = buttonText switch
                {
                    "Cancel" or "Close" => GoldSliceButtonVariant.Neutral,
                    "Drop" => GoldSliceButtonVariant.Red,
                    _ => GoldSliceButtonVariant.Green
                },
                TextColor = Colors.White,
                MinimumWidthRequest = 90,
                Margin = new Thickness(3)
            };

            button.Clicked += (sender, e) => DismissCustomDialog(buttonText);
            CustomDialogButtons.Children.Add(button);
        }

        CustomDialogOverlay.IsVisible = true;
        _customDialogCompletion = new TaskCompletionSource<string?>();

        return _customDialogCompletion.Task;
    }

    private void OnCustomDialogBackgroundTapped(
        object? sender,
        TappedEventArgs e)
    {
        if (_customDialogDismissOnBackground)
            DismissCustomDialog("Close");
    }

    private static FormattedString BuildDialogFormattedText(
        string message,
        bool colorEquipmentLines)
    {
        FormattedString formatted = new();
        string[] lines = message.Split('\n');
        // Equipment dialogs contain one or more bonus/speed lines.  Keep
        // value text in the normal item dialogs unchanged while applying the
        // equipment palette only where it belongs.
        bool isEquipmentMessage = lines.Any(line =>
            line.EndsWith(" equipment", StringComparison.Ordinal) ||
            line.StartsWith("Attack +", StringComparison.Ordinal) ||
            line.StartsWith("Strength +", StringComparison.Ordinal) ||
            line.StartsWith("Defense +", StringComparison.Ordinal) ||
            line.EndsWith(" attack ticks", StringComparison.Ordinal));

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];
            Color color = Colors.White;

            if (colorEquipmentLines)
            {
                if (line.StartsWith("Attack +", StringComparison.Ordinal))
                    color = Color.FromArgb("#E64A4A");
                else if (line.StartsWith("Strength +", StringComparison.Ordinal))
                    color = Color.FromArgb("#FFE26A");
                else if (line.StartsWith("Defense +", StringComparison.Ordinal))
                    color = Color.FromArgb("#4D9FFF");
                else if (line.EndsWith(" attack ticks", StringComparison.Ordinal) &&
                         int.TryParse(line[..^13], out int ticks))
                {
                    color = ticks <= 3
                        ? Color.FromArgb("#42A85A")
                        : ticks <= 5
                            ? Color.FromArgb("#FFE26A")
                            : Color.FromArgb("#E64A4A");
                }
                else if (isEquipmentMessage &&
                         line.StartsWith("Value:", StringComparison.Ordinal))
                    color = Color.FromArgb("#FFE26A");
            }

            formatted.Spans.Add(new Span
            {
                Text = line + (index < lines.Length - 1 ? "\n" : string.Empty),
                TextColor = color
            });
        }

        return formatted;
    }

    private void DismissCustomDialog(string result)
    {
        TaskCompletionSource<string?>? completion = _customDialogCompletion;
        _customDialogCompletion = null;
        _customDialogDismissOnBackground = false;
        CustomDialogOverlay.IsVisible = false;
        completion?.SetResult(result);
    }

    private static string BuildOfflineSummaryMessage(
        OfflineSkillingSummary summary)
    {
        string actionVerb = summary.SkillName switch
        {
            "Fishing" => "fished",
            "Mining" => "mined",
            "Woodcutting" => "chopped wood",
            "Agility" => "trained agility",
            "Thieving" => "thieved",
            "Crafting" => "crafted",
            "Fletching" => "fletched",
            "Farming" => "farmed",
            _ => "trained"
        };

        string message =
            $"While you were away, you {actionVerb} " +
            $"{summary.CompletedActions:N0} times " +
            $"({summary.ActivityName}).";

        return message;
    }


    // ============================================================
    // PAGE NAVIGATION
    // ============================================================

    private void OnCombatStarted()
    {
        if (ActivityManager.IsActive)
        {
            ActivityManager.StopActivity();
        }
    }

    private void OnActivityChanged(
        object? sender,
        EventArgs e)
    {
        if (!ActivityManager.IsActive ||
            (!CombatManager.IsInCombat &&
             !CombatManager.IsAutoFightRespawning))
        {
            return;
        }

        if (_combatView != null)
        {
            _combatView.StopCombatForSkillActivity();
            return;
        }

        CombatManager.AbortCombatEncounter();
    }

    public void ShowHomePage()
    {
        _homeView ??=
            new HomeView(
                Player,
                CombatManager,
                ActivityManager);

        _combatView?.SetActive(false);
        _homeView.SetActive(true);
        _homeView.RefreshDisplay();

        GameContent.Content = _homeView;

        UpdateNavigationAppearance(HomeNavigationButton);
        UpdatePageTitle("Home");
    }


    public void ShowSkillsPage()
    {
        _homeView?.SetActive(false);
        _combatView?.SetActive(false);

        _skillsView ??=
            new SkillsView(
                Player,
                ActivityManager);

        _skillsView.RefreshDisplay();
        GameContent.Content = _skillsView;

        UpdateNavigationAppearance(SkillsNavigationButton);
        UpdatePageTitle(string.Empty);
    }


    public void ShowSkillPage(
        Skill skill)
    {
        _homeView?.SetActive(false);
        _combatView?.SetActive(false);

        if (!_skillPages.TryGetValue(skill, out SkillPage? skillPage))
        {
            skillPage = new SkillPage(
                skill,
                ActivityManager);
            _skillPages[skill] = skillPage;
        }

        skillPage.RefreshDisplay();
        GameContent.Content = skillPage;

        UpdateNavigationAppearance(SkillsNavigationButton);
        UpdatePageTitle(skill.Name);
    }


    public void ShowCombatPage()
    {
        _homeView?.SetActive(false);

        if (_combatView == null)
        {
            _combatView = new CombatView(
                Player,
                CombatManager);
        }
        else
        {
            _combatView.RefreshEnemyList();
        }

        _combatView.SetActive(true);
        GameContent.Content = _combatView;

        _combatView.RefreshCombatDisplay();

        UpdateNavigationAppearance(CombatNavigationButton);
        UpdatePageTitle("Combat");
    }


    public void ShowInventoryPage()
    {
        _homeView?.SetActive(false);
        _combatView?.SetActive(false);

        _inventoryView ??=
            new InventoryView(
                Player);

        _inventoryView.RefreshDisplay();

        GameContent.Content = _inventoryView;

        UpdateNavigationAppearance(InventoryNavigationButton);
        UpdatePageTitle(string.Empty);
    }


    public void ShowCollectionLogPage()
    {
        _homeView?.SetActive(false);
        _combatView?.SetActive(false);

        _collectionLogView ??=
            new CollectionLogView(
                Player.CollectionLog,
                StartCombatFromCollection);

        _collectionLogView.RefreshDisplay();

        GameContent.Content = _collectionLogView;

        Enemy? activeEnemy =
            CombatManager.CurrentEnemy;

        if (activeEnemy == null &&
            CombatManager.IsAutoFightRespawning)
        {
            activeEnemy =
                CombatManager.AutoFightEnemy;
        }

        if (activeEnemy != null)
            _ = _collectionLogView.OpenEnemyAsync(activeEnemy);

        UpdateNavigationAppearance(CollectionNavigationButton);
        UpdatePageTitle("Collection Log");
    }

    private void StartCombatFromCollection(Enemy enemy)
    {
        ShowCombatPage();
        _combatView!.StartCombatNow(enemy);
    }


    public void ShowSettingsPage()
    {
        _homeView?.SetActive(false);
        _combatView?.SetActive(false);

        _settingsView ??= new SettingsView(
            ResetCharacter,
            ReturnToCharacterSelect,
            QueueDebugNotification);
        GameContent.Content = _settingsView;

        UpdateNavigationAppearance(SettingsNavigationButton);
        UpdatePageTitle("Settings");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        Loaded -= OnGamePageLoaded;
        SizeChanged -= OnGamePageSizeChanged;
        GameClock.SpeedChanged -= OnGameSpeedChanged;

        ActivityManager.ActionCompleted -= OnActivityXPChanged;
        ActivityManager.LevelUp -= OnLevelUp;
        ActivityManager.ActivityStateChanged -= OnActivityChanged;
        CombatManager.PlayerDefeated -= OnPlayerDefeated;
        CombatManager.XPChanged -= _game.ScheduleSave;
        CombatManager.EnemyDefeated -= OnEnemyDefeated;
        CombatManager.LevelUp -= OnLevelUp;
        CombatManager.CombatStarted -= OnCombatStarted;
        Player.CollectionLog.CollectionCompleted -= OnCollectionCompleted;

        _notificationQueue.Dispose();

        _deathCancellation?.Cancel();
        _deathCancellation?.Dispose();
        _deathCancellation = null;

        if (_healthRegenerationTimer != null)
        {
            _healthRegenerationTimer.Stop();
            _healthRegenerationTimer.Tick -= OnHealthRegenerationTick;
            _healthRegenerationTimer = null;
        }

        if (_fpsTimer != null)
        {
            _fpsTimer.Stop();
            _fpsTimer.Tick -= OnFpsTimerTick;
            _fpsTimer = null;
        }

        _settingsView?.Dispose();
        _homeView?.Dispose();
        _combatView?.Dispose();
        _collectionLogView?.Dispose();
        _inventoryView?.Dispose();
        _skillsView?.Dispose();
        _activityBar?.Dispose();

        CustomDialogService.ClearHost(this);

        _game.ClearActivityManagers(ActivityManager, CombatManager);
        ActivityManager.Dispose();
        CombatManager.Dispose();

        ActivityBarContainer.Content = null;
        GameContent.Content = null;
    }

    private void ReturnToCharacterSelect()
    {
        _game.Save();
        Dispose();

        Window? window = Application.Current?.Windows.FirstOrDefault();
        if (window != null)
            window.Page = new LandingPage();
    }

    private void ResetCharacter()
    {
        Dispose();
        _game.Reset();

        Window? window = Application.Current?.Windows.FirstOrDefault();
        if (window != null)
            window.Page = new LandingPage();
    }

    private void UpdatePageTitle(string title)
    {
        Title = title;

        if (Shell.Current is AppShell appShell)
        {
            appShell.SetGamePageTitle(title);
        }
    }

    private void UpdateNavigationAppearance(Border selectedButton)
    {
        foreach (Border button in new[]
                 {
                     HomeNavigationButton,
                     SkillsNavigationButton,
                     CombatNavigationButton,
                     InventoryNavigationButton,
                     CollectionNavigationButton,
                     SettingsNavigationButton
                 })
        {
            bool isSelected = button == selectedButton;

            button.BackgroundColor = isSelected
                ? Color.FromArgb("#D92D7D46")
                : Colors.Transparent;

            button.Stroke = isSelected
                ? Color.FromArgb("#D99032")
                : Colors.Transparent;

            button.StrokeThickness = isSelected ? 1 : 0;

            button.AbortAnimation("navSelect");
            button.Scale = 1;
            if (isSelected)
            {
                new Animation
                {
                    { 0.0, 0.45, new Animation(value => button.Scale = value, 1, 1.08) },
                    { 0.45, 1.0, new Animation(value => button.Scale = value, 1.08, 1) }
                }.Commit(button, "navSelect", 16, 220, Easing.CubicOut);
            }
        }

        _ = AnimatePageTransitionAsync();
    }

    private async Task AnimatePageTransitionAsync()
    {
        try
        {
            GameContent.Opacity = 0.82;
            GameContent.TranslationX = 6;
            await Task.WhenAll(
                GameContent.FadeToAsync(1, 140, Easing.CubicOut),
                GameContent.TranslateToAsync(0, 0, 140, Easing.CubicOut));
        }
        catch
        {
            GameContent.Opacity = 1;
            GameContent.TranslationX = 0;
        }
    }


    // ============================================================
    // PLAYER DEATH
    // ============================================================

    private void OnPlayerDefeated()
    {
        MainThread.BeginInvokeOnMainThread(
            async () =>
            {
                await ShowDeathScreenAsync();
            });
    }


    // ============================================================
    // DEATH SCREEN
    // ============================================================

    private async Task ShowDeathScreenAsync(
        long offlineTicksToUse = 0)
    {
        // --------------------------------------------------------
        // Cancel any previous death sequence.
        // --------------------------------------------------------

        _deathCancellation?.Cancel();
        _deathCancellation?.Dispose();

        _deathCancellation =
            new CancellationTokenSource();

        CancellationToken token =
            _deathCancellation.Token;


        // --------------------------------------------------------
        // Make absolutely sure combat is dead.
        // --------------------------------------------------------

        CombatManager.StopCombat();


        // --------------------------------------------------------
        // Create full-screen death overlay.
        // --------------------------------------------------------

        Grid deathOverlay =
            new Grid
            {
                BackgroundColor =
                    Colors.Black,

                Opacity = 0,

                ZIndex = 999
            };


        // --------------------------------------------------------
        // Death message.
        // --------------------------------------------------------

        Label deathLabel =
            new Label
            {
                Text =
                    "OH DEAR, YOU ARE DEAD...",

                TextColor =
                    Colors.Red,

                FontSize =
                    30,

                FontAttributes =
                    FontAttributes.Bold,

                HorizontalOptions =
                    LayoutOptions.Center,

                VerticalOptions =
                    LayoutOptions.Center,

                HorizontalTextAlignment =
                    TextAlignment.Center,

                Opacity = 0
            };


        // --------------------------------------------------------
        // Respawn timer.
        // --------------------------------------------------------

        Label timerLabel =
            new Label
            {
                Text =
                    $"Respawning in {DeathRespawnTicks} ticks",

                TextColor =
                    Colors.Red,

                FontSize =
                    18,

                HorizontalOptions =
                    LayoutOptions.Center,

                VerticalOptions =
                    LayoutOptions.Center,

                VerticalTextAlignment =
                    TextAlignment.Center,

                HorizontalTextAlignment =
                    TextAlignment.Center,

                TranslationY = 55,

                Opacity = 0
            };


        // --------------------------------------------------------
        // Add everything to overlay.
        // --------------------------------------------------------

        deathOverlay.Children.Add(
            deathLabel);

        deathOverlay.Children.Add(
            timerLabel);


        // --------------------------------------------------------
        // Put overlay above the entire game.
        // --------------------------------------------------------

        MainGrid.Children.Add(
            deathOverlay);


        // --------------------------------------------------------
        // Fade screen to black.
        // --------------------------------------------------------

        await deathOverlay.FadeToAsync(
            1,
            1000);


        // --------------------------------------------------------
        // Fade death text in.
        // --------------------------------------------------------

        await deathLabel.FadeToAsync(
            1,
            500);

        await timerLabel.FadeToAsync(
            1,
            500);


        // --------------------------------------------------------
        // Tick countdown.
        // --------------------------------------------------------

        int ticksRemaining =
            DeathRespawnTicks;


        while (ticksRemaining > 0)
        {
            token.ThrowIfCancellationRequested();

            bool usingOfflineTicks = offlineTicksToUse > 0;

            timerLabel.Text = usingOfflineTicks
                ? "Using offline ticks to speed up...\n" +
                  $"Respawning in {ticksRemaining} ticks"
                : $"Respawning in {ticksRemaining} ticks";


            await Task.Delay(
                GameClock.TickInterval,
                token);


            int countdownAdvance = usingOfflineTicks
                ? Math.Min(5, ticksRemaining)
                : 1;

            if (usingOfflineTicks)
            {
                offlineTicksToUse = Math.Max(
                    0,
                    offlineTicksToUse - countdownAdvance);
            }

            ticksRemaining -= countdownAdvance;
        }


        // --------------------------------------------------------
        // Respawn player.
        // --------------------------------------------------------

        Player.CurrentHP =
            Player.GetMaxHP();

        _game.Save();


        // --------------------------------------------------------
        // Make sure combat is completely stopped.
        // --------------------------------------------------------

        CombatManager.StopCombat();


        // --------------------------------------------------------
        // Return to Home.
        // --------------------------------------------------------

        ShowHomePage();


        // --------------------------------------------------------
        // Fade the death screen away.
        // --------------------------------------------------------

        await deathOverlay.FadeToAsync(
            0,
            1000);


        // --------------------------------------------------------
        // Remove death overlay.
        // --------------------------------------------------------

        MainGrid.Children.Remove(
            deathOverlay);
    }


    // ============================================================
    // LEVEL UP EVENT
    // ============================================================

    private void OnLevelUp(
        object? sender,
        LevelUpEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _notificationQueue.Enqueue(cancellationToken =>
                ShowLevelUpPopupAsync(
                    e.Skill,
                    e.Level,
                    cancellationToken));
        });
    }


    // ============================================================
    // RARE LOOT EVENT
    // ============================================================

    private void OnEnemyDefeated(Enemy enemy)
    {
        List<LootResult> rareLoot =
            CombatManager.LastLoot
                .Where(loot => loot.Chance <= 0.01)
                .ToList();

        if (rareLoot.Count == 0)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (LootResult loot in rareLoot)
                _notificationQueue.Enqueue(cancellationToken =>
                    ShowRareLootPopupAsync(loot, cancellationToken));
        });
    }

    private void OnCollectionCompleted(Enemy enemy)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _notificationQueue.Enqueue(cancellationToken =>
                ShowCollectionCompletedPopupAsync(enemy, cancellationToken));
        });
    }


    // ============================================================
    // LEVEL UP POPUP
    // ============================================================

    private void QueueDebugNotification()
    {
        _notificationQueue.Enqueue(ShowDebugNotificationAsync);
    }

    private async Task ShowDebugNotificationAsync(CancellationToken cancellationToken)
    {
        Border popup =
            new Border
            {
                BackgroundColor = Color.FromArgb("#1C1C1C"),
                Stroke = Color.FromArgb("#FFE26A"),
                StrokeThickness = 2,
                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = 14
                    },
                Padding = new Thickness(28, 18),
                MaximumWidthRequest = 340,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = 0,
                Opacity = 0,
                Scale = 0.72,
                TranslationY = 18,
                Content =
                    new VerticalStackLayout
                    {
                        Spacing = 3,
                        HorizontalOptions = LayoutOptions.Fill,
                        Children =
                        {
                            new Label
                            {
                                Text = "TEST NOTIFICATION",
                                FontSize = 30,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#FFE26A"),
                                HorizontalTextAlignment = TextAlignment.Center,
                                HorizontalOptions = LayoutOptions.Center
                            },
                            new Label
                            {
                                Text = "Notification positioning preview",
                                FontSize = 18,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Colors.White,
                                HorizontalTextAlignment = TextAlignment.Center,
                                HorizontalOptions = LayoutOptions.Center
                            }
                        }
                    }
            };

        try
        {
            await AddCenteredNotificationAsync(popup, cancellationToken: cancellationToken);

            await Task.WhenAll(
                popup.FadeToAsync(1, 180),
                popup.ScaleToAsync(1, 260, Easing.CubicOut),
                popup.TranslateToAsync(0, 0, 260, Easing.CubicOut));

            await Task.Delay(3000, cancellationToken);

            await Task.WhenAll(
                popup.FadeToAsync(0, 350),
                popup.ScaleToAsync(0.9, 350, Easing.CubicIn),
                popup.TranslateToAsync(0, -24, 350, Easing.CubicIn));
        }
        finally
        {
            NotificationLayer.Children.Remove(popup);
        }
    }

    private async Task ShowLevelUpPopupAsync(
        Skill skill,
        int level,
        CancellationToken cancellationToken)
    {
        Label heading =
            new Label
            {
                Text = "LEVEL UP!",

                FontSize = 34,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Color.FromArgb("#FFE26A"),

                HorizontalTextAlignment =
                    TextAlignment.Center,

                HorizontalOptions =
                    LayoutOptions.Center
            };

        Label details =
            new Label
            {
                Text =
                    $"{skill.Name} {level - 1} \u2192 {level}!",

                FontSize = 22,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    Colors.White,

                HorizontalTextAlignment =
                    TextAlignment.Center,

                HorizontalOptions =
                    LayoutOptions.Center
            };

        Border levelUpPopup =
            new Border
            {
                BackgroundColor =
                    Color.FromArgb("#1C1C1C"),

                Stroke =
                    Color.FromArgb("#FFE26A"),

                StrokeThickness = 2,

                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = 14
                    },

                Padding =
                    new Thickness(28, 18),

                MaximumWidthRequest =
                    340,

                HorizontalOptions =
                    LayoutOptions.Center,

                VerticalOptions =
                    LayoutOptions.Center,

                Margin = 0,

                Opacity = 0,

                Scale = 0.72,

                TranslationY = 18,

                Content =
                    new VerticalStackLayout
                    {
                        Spacing = 3,
                        HorizontalOptions = LayoutOptions.Fill,
                        Children =
                        {
                            heading,
                            details
                        }
                    }
            };

        try
        {
            await AddCenteredNotificationAsync(levelUpPopup, cancellationToken: cancellationToken);

            await Task.WhenAll(
                levelUpPopup.FadeToAsync(1, 180),
                levelUpPopup.ScaleToAsync(1, 260, Easing.CubicOut),
                levelUpPopup.TranslateToAsync(0, 0, 260, Easing.CubicOut));

            await Task.Delay(3000, cancellationToken);

            await Task.WhenAll(
                levelUpPopup.FadeToAsync(0, 350),
                levelUpPopup.ScaleToAsync(0.9, 350, Easing.CubicIn),
                levelUpPopup.TranslateToAsync(0, -24, 350, Easing.CubicIn));
        }
        finally
        {
            NotificationLayer.Children.Remove(levelUpPopup);
        }
    }


    // ============================================================
    // RARE LOOT POPUP
    // ============================================================

    private async Task ShowRareLootPopupAsync(
        LootResult loot,
        CancellationToken cancellationToken)
    {
        if (loot.Rarity is DropRarity.SuperRare or DropRarity.MegaRare)
        {
        }

        string title;
        Color accentColor;
        double peakScale;
        int displayMilliseconds;

        if (loot.Chance <= 0.0001)
        {
            title = "MEGA RARE DROP!";
            accentColor = Color.FromArgb("#FF4DFF");
            peakScale = 1.22;
            displayMilliseconds = 3000;
        }
        else if (loot.Chance <= 0.001)
        {
            title = "SUPER RARE DROP!";
            accentColor = Color.FromArgb("#FFCC33");
            peakScale = 1.14;
            displayMilliseconds = 3000;
        }
        else
        {
            title = "RARE DROP!";
            accentColor = Color.FromArgb("#66D9FF");
            peakScale = 1.06;
            displayMilliseconds = 3000;
        }

        Label titleLabel =
            new Label
            {
                Text = title,
                FontSize = 30,
                FontAttributes = FontAttributes.Bold,
                TextColor = accentColor,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center
            };

        Label itemLabel =
            new Label
            {
                Text = $"{loot.Item.Name}  x{loot.Quantity}",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center
            };

        Border lootPopup =
            new Border
            {
                BackgroundColor = Color.FromArgb("#202020"),
                Stroke = accentColor,
                StrokeThickness = 3,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(30, 20),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                MaximumWidthRequest = 340,
                Margin = 0,
                Opacity = 0,
                Scale = 0.55,
                TranslationY = 24,
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    HorizontalOptions = LayoutOptions.Fill,
                    Children =
                    {
                        titleLabel,
                        itemLabel
                    }
                }
            };

        try
        {
            await AddCenteredNotificationAsync(lootPopup, cancellationToken: cancellationToken);

            await Task.WhenAll(
                lootPopup.FadeToAsync(1, 140),
                lootPopup.ScaleToAsync(peakScale, 260, Easing.CubicOut),
                lootPopup.TranslateToAsync(0, 0, 260, Easing.CubicOut));

            if (loot.Chance <= 0.001)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await lootPopup.TranslateToAsync(-8, 0, 55);
                await lootPopup.TranslateToAsync(8, 0, 55);
                await lootPopup.TranslateToAsync(0, 0, 55);
            }

            await Task.Delay(displayMilliseconds, cancellationToken);

            await Task.WhenAll(
                lootPopup.FadeToAsync(0, 350),
                lootPopup.ScaleToAsync(0.9, 350, Easing.CubicIn),
                lootPopup.TranslateToAsync(0, -20, 350, Easing.CubicIn));
        }
        finally
        {
            NotificationLayer.Children.Remove(lootPopup);
        }
    }


    // ============================================================
    // COLLECTION LOG COMPLETION POPUP
    // ============================================================

    private async Task ShowCollectionCompletedPopupAsync(
        Enemy enemy,
        CancellationToken cancellationToken)
    {
        Label titleLabel =
            new Label
            {
                Text = "COLLECTION LOG COMPLETE!",
                FontSize = 30,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#FFE26A"),
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center
            };

        Label enemyLabel =
            new Label
            {
                Text = enemy.Name,
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Center
            };

        Border popup =
            new Border
            {
                BackgroundColor = Color.FromArgb("#202020"),
                Stroke = Color.FromArgb("#FFE26A"),
                StrokeThickness = 3,
                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius = 14
                    },
                Padding = new Thickness(30, 20),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                MaximumWidthRequest = 340,
                Margin = 0,
                Opacity = 0,
                Scale = 0.55,
                TranslationY = 24,
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    HorizontalOptions = LayoutOptions.Fill,
                    Children =
                    {
                        titleLabel,
                        enemyLabel
                    }
                }
            };

        try
        {
            await AddCenteredNotificationAsync(popup, cancellationToken: cancellationToken);

            await Task.WhenAll(
                popup.FadeToAsync(1, 140),
                popup.ScaleToAsync(1.14, 260, Easing.CubicOut),
                popup.TranslateToAsync(0, 0, 260, Easing.CubicOut));

        await Task.Delay(3000, cancellationToken);

            await Task.WhenAll(
                popup.FadeToAsync(0, 350, Easing.CubicIn),
                popup.ScaleToAsync(0.9, 350, Easing.CubicIn),
                popup.TranslateToAsync(0, -20, 350, Easing.CubicIn));
        }
        finally
        {
            NotificationLayer.Children.Remove(popup);
        }
    }


    // ============================================================
    // CELEBRATION NOTIFICATIONS
    // ============================================================

    private async Task AddCenteredNotificationAsync(
        View notification,
        double maximumWidth = 340,
        double horizontalMargin = 24,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NotificationLayer.Children.Add(notification);

        // Wait for Android to arrange the full-window overlay before using
        // its dimensions. DeviceDisplay reports physical pixels, while MAUI
        // views use density-independent units, so the overlay's actual bounds
        // are the correct coordinate space for positioning its children.
        for (int attempt = 0;
             attempt < 30 &&
             (NotificationLayer.Width <= 0 || NotificationLayer.Height <= 0);
             attempt++)
        {
            await Task.Delay(16, cancellationToken);
        }

        double overlayWidth = NotificationLayer.Width;
        double overlayHeight = NotificationLayer.Height;

        if (overlayWidth <= 0 || overlayHeight <= 0)
        {
            overlayWidth = Math.Max(0, MainGrid.Width);
            overlayHeight = Math.Max(0, MainGrid.Height);
        }

        double widthConstraint = Math.Max(
            0,
            Math.Min(
                maximumWidth,
                overlayWidth - horizontalMargin * 2));
        double heightConstraint = Math.Max(0, overlayHeight);

        notification.MaximumWidthRequest = widthConstraint;

        AbsoluteLayout.SetLayoutFlags(
            notification,
            Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
        AbsoluteLayout.SetLayoutBounds(
            notification,
            new Rect(
                0,
                0,
                AbsoluteLayout.AutoSize,
                AbsoluteLayout.AutoSize));

        // Android can return a wider value from Measure than it ultimately
        // renders for a centered Border. Let it perform that real layout first,
        // then center the actual rendered size instead of the measured slot.
        for (int attempt = 0;
             attempt < 30 &&
             (notification.Width <= 0 || notification.Height <= 0);
             attempt++)
        {
            await Task.Delay(16, cancellationToken);
        }

        double notificationWidth = notification.Width;
        double notificationHeight = notification.Height;

        if (notificationWidth <= 0 || notificationHeight <= 0)
        {
            Size measured = notification.Measure(
                widthConstraint,
                heightConstraint);

            notificationWidth = Math.Min(
                widthConstraint,
                measured.Width);
            notificationHeight = Math.Min(
                heightConstraint,
                measured.Height);
        }

#if ANDROID
        // MAUI's Android handler leaves a dynamically-created view's native
        // horizontal scale pivot at the left edge unless the anchor mapper
        // runs after the view has a size. These popups begin scaled down, so
        // their visible bounds were left of their correctly-centered frames.
        // Set the native X pivot from the arranged pixel width before animating.
        if (notification.Handler?.PlatformView is
            Android.Views.View platformNotification)
        {
            platformNotification.PivotX =
                platformNotification.Width / 2f;
        }
#endif

        double horizontalViewportWidth = overlayWidth;

#if ANDROID
        DisplayInfo displayInfo =
            DeviceDisplay.Current.MainDisplayInfo;

        if (displayInfo.Width > 0 && displayInfo.Density > 0)
        {
            horizontalViewportWidth =
                displayInfo.Width / displayInfo.Density;
        }
#endif

        double left =
            horizontalViewportWidth / 2 -
            notificationWidth / 2;
        double top = (overlayHeight - notificationHeight) / 2;

        AbsoluteLayout.SetLayoutBounds(
            notification,
            new Rect(
                left,
                top,
                AbsoluteLayout.AutoSize,
                AbsoluteLayout.AutoSize));
    }

    // ============================================================
    // NAVIGATION BUTTONS
    // ============================================================

    private void OnHomeClicked(
        object? sender,
        TappedEventArgs e)
    {
        ShowHomePage();
    }


    private void OnSkillsClicked(
        object? sender,
        TappedEventArgs e)
    {
        ShowSkillsPage();
    }


    private void OnCombatClicked(
        object? sender,
        TappedEventArgs e)
    {
        ShowCombatPage();
    }


    private void OnInventoryClicked(
        object? sender,
        TappedEventArgs e)
    {
        ShowInventoryPage();
    }

    private void OnCollectionLogClicked(
        object? sender,
        TappedEventArgs e)
    {
        ShowCollectionLogPage();
    }


    private void OnSettingsClicked(
        object? sender,
        TappedEventArgs e)
    {
        ShowSettingsPage();
    }
}
