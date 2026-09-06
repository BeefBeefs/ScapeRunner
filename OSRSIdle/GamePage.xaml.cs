using Microsoft.Maui.Layouts;

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


    // ============================================================
    // PLAYER DEATH
    // ============================================================

    private CancellationTokenSource? _deathCancellation;

    private const int DeathRespawnTicks = 60;

    private const int HealthRegenerationTicks = 100;

    private IDispatcherTimer? _healthRegenerationTimer;

    private int _healthRegenerationTickCount;

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

        _game = ((App)Application.Current!).Game;

        CustomDialogService.SetHost(this);

        Player = _game.Player;


        // --------------------------------------------------------
        // Create game systems.
        // --------------------------------------------------------

        ActivityManager =
            new ActivityManager(
                Player);

        CombatManager =
            new CombatManager(Player);

        _game.SetActivityManagers(
            ActivityManager,
            CombatManager);

        _game.ResumeSavedSkillingActivity(ActivityManager);

        StartHealthRegenerationTimer();


        // --------------------------------------------------------
        // Create persistent Activity Bar.
        // --------------------------------------------------------

        ActivityBarContainer.Content =
            new ActivityBar(
                ActivityManager,
                CombatManager);


        // --------------------------------------------------------
        // Listen for game events.
        // --------------------------------------------------------

        ActivityManager.XPChanged +=
            (sender, e) => _game.ScheduleSave();

        ActivityManager.LevelUp +=
            OnLevelUp;

        ActivityManager.ActivityChanged +=
            OnActivityChanged;

        CombatManager.PlayerDefeated +=
            OnPlayerDefeated;

        CombatManager.XPChanged +=
            _game.ScheduleSave;

        CombatManager.EnemyDefeated +=
            OnEnemyDefeated;

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

        _healthRegenerationTimer.Tick += (sender, e) =>
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
        };

        _healthRegenerationTimer.Start();
    }

    private void OnGameSpeedChanged(object? sender, EventArgs e)
    {
        if (_healthRegenerationTimer != null)
            _healthRegenerationTimer.Interval = GameClock.TickInterval;
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

    private static void AddRainbowText(
        FormattedString formattedText,
        string text)
    {
        Color[] rainbow =
        {
            Color.FromArgb("#FF5C5C"),
            Color.FromArgb("#FFB347"),
            Color.FromArgb("#FFF45C"),
            Color.FromArgb("#68E06F"),
            Color.FromArgb("#5CB8FF"),
            Color.FromArgb("#B783FF"),
            Color.FromArgb("#FF7DC8")
        };

        for (int index = 0; index < text.Length; index++)
        {
            formattedText.Spans.Add(new Span
            {
                Text = text[index].ToString(),
                TextColor = rainbow[index % rainbow.Length]
            });
        }
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

        _homeView.RefreshDisplay();

        GameContent.Content = _homeView;

        UpdateNavigationAppearance(HomeNavigationButton);
        UpdatePageTitle("Home");
    }


    public void ShowSkillsPage()
    {
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

        GameContent.Content = _combatView;

        _combatView.RefreshCombatDisplay();

        UpdateNavigationAppearance(CombatNavigationButton);
        UpdatePageTitle("Combat");
    }


    public void ShowInventoryPage()
    {
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
        _collectionLogView ??=
            new CollectionLogView(
                Player.CollectionLog);

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


    public void ShowSettingsPage()
    {
        _settingsView ??= new SettingsView();
        GameContent.Content = _settingsView;

        UpdateNavigationAppearance(SettingsNavigationButton);
        UpdatePageTitle("Settings");
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
            ShowLevelUpPopup(
                e.Skill,
                e.Level);
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
            {
                ShowRareLootPopup(loot);
            }
        });
    }


    // ============================================================
    // LEVEL UP POPUP
    // ============================================================

    private async void ShowLevelUpPopup(
        Skill skill,
        int level)
    {
        _ = ShowLevelUpFireworksAsync();

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
                        Children =
                        {
                            heading,
                            details
                        }
                    }
            };

        AbsoluteLayout levelUpPopupHost =
            new AbsoluteLayout
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                InputTransparent = true
            };

        // A proportional anchor keeps this popup centered in the available
        // game area on Android, where a nested Grid can otherwise measure a
        // centered child from its content origin instead of its full bounds.
        AbsoluteLayout.SetLayoutFlags(
            levelUpPopup,
            AbsoluteLayoutFlags.PositionProportional);
        AbsoluteLayout.SetLayoutBounds(
            levelUpPopup,
            new Rect(
                0.5,
                0.5,
                AbsoluteLayout.AutoSize,
                AbsoluteLayout.AutoSize));

        levelUpPopupHost.Children.Add(levelUpPopup);
        NotificationLayer.Children.Add(levelUpPopupHost);


        await Task.WhenAll(
            levelUpPopup.FadeToAsync(
                1,
                180),

            levelUpPopup.ScaleToAsync(
                1,
                260,
                Easing.CubicOut),

            levelUpPopup.TranslateToAsync(
                0,
                0,
                260,
                Easing.CubicOut));


        await Task.Delay(3000);


        await Task.WhenAll(
            levelUpPopup.FadeToAsync(
                0,
                350),

            levelUpPopup.ScaleToAsync(
                0.9,
                350,
                Easing.CubicIn));


        NotificationLayer.Children.Remove(levelUpPopupHost);
    }


    // ============================================================
    // RARE LOOT POPUP
    // ============================================================

    private async void ShowRareLootPopup(
        LootResult loot)
    {
        if (loot.Rarity is DropRarity.SuperRare or DropRarity.MegaRare)
        {
            _ = ShowThreeFireworksAsync();
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
            displayMilliseconds = 4000;
        }
        else if (loot.Chance <= 0.001)
        {
            title = "SUPER RARE DROP!";
            accentColor = Color.FromArgb("#FFCC33");
            peakScale = 1.14;
            displayMilliseconds = 3500;
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
                Opacity = 0,
                Scale = 0.55,
                TranslationY = 24,
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        titleLabel,
                        itemLabel
                    }
                }
            };

        NotificationLayer.Children.Add(lootPopup);

        await Task.WhenAll(
            lootPopup.FadeToAsync(1, 140),
            lootPopup.ScaleToAsync(peakScale, 260, Easing.CubicOut),
            lootPopup.TranslateToAsync(0, 0, 260, Easing.CubicOut));

        if (loot.Chance <= 0.001)
        {
            await lootPopup.TranslateToAsync(-8, 0, 55);
            await lootPopup.TranslateToAsync(8, 0, 55);
            await lootPopup.TranslateToAsync(0, 0, 55);
        }

        await Task.Delay(displayMilliseconds);

        await Task.WhenAll(
            lootPopup.FadeToAsync(0, 350),
            lootPopup.ScaleToAsync(0.9, 350, Easing.CubicIn),
            lootPopup.TranslateToAsync(0, -20, 350, Easing.CubicIn));

        NotificationLayer.Children.Remove(lootPopup);
    }


    // ============================================================
    // CELEBRATION FIREWORKS
    // ============================================================

    private async Task ShowThreeFireworksAsync()
    {
        (Color Color, double X, double Y, int Delay)[] fireworks =
        {
            (Color.FromArgb("#FFE26A"), -92, -72, 0),
            (Color.FromArgb("#62C7FF"), 0, -118, 115),
            (Color.FromArgb("#FF7DC8"), 92, -72, 230)
        };

        await Task.WhenAll(fireworks.Select(firework =>
            ShowFireworkAsync(
                firework.Color,
                firework.X,
                firework.Y,
                firework.Delay)));
    }

    private async Task ShowLevelUpFireworksAsync()
    {
        Color[] colors =
        {
            Color.FromArgb("#FFE26A"),
            Color.FromArgb("#62C7FF"),
            Color.FromArgb("#FF7DC8"),
            Color.FromArgb("#B783FF"),
            Color.FromArgb("#68E06F")
        };

        int fireworkCount = Random.Shared.Next(10, 16);
        int[] launchTimes = Enumerable.Range(0, fireworkCount)
            .Select(_ => Random.Shared.Next(0, 3_001))
            .OrderBy(time => time)
            .ToArray();

        await Task.WhenAll(launchTimes.Select(launchTime =>
            ShowFireworkAsync(
                colors[Random.Shared.Next(colors.Length)],
                Random.Shared.Next(-118, 119),
                Random.Shared.Next(-150, -24),
                launchTime)));
    }

    private async Task ShowFireworkAsync(
        Color color,
        double x,
        double y,
        int delayMilliseconds)
    {
        if (delayMilliseconds > 0)
            await Task.Delay(delayMilliseconds);

        Grid burst = new Grid
        {
            WidthRequest = 72,
            HeightRequest = 72,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            TranslationX = x,
            TranslationY = y + 20,
            Scale = 0.2,
            Opacity = 0,
            InputTransparent = true
        };

        burst.Children.Add(new Label
        {
            Text = "✦",
            FontSize = 56,
            TextColor = color,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        });

        foreach ((double X, double Y) offset in new[]
                 {
                     (0d, -28d), (28d, 0d),
                     (0d, 28d), (-28d, 0d)
                 })
        {
            burst.Children.Add(new Label
            {
                Text = "•",
                FontSize = 15,
                TextColor = color,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TranslationX = offset.X,
                TranslationY = offset.Y,
                Opacity = 0.9
            });
        }

        NotificationLayer.Children.Add(burst);

        await Task.WhenAll(
            burst.FadeToAsync(1, 70),
            burst.ScaleToAsync(1.1, 230, Easing.CubicOut),
            burst.TranslateToAsync(x, y, 230, Easing.CubicOut));

        await Task.Delay(120);

        await Task.WhenAll(
            burst.FadeToAsync(0, 330, Easing.CubicIn),
            burst.ScaleToAsync(1.65, 330, Easing.CubicIn));

        NotificationLayer.Children.Remove(burst);
    }


    // ============================================================
    // NAVIGATION BUTTONS
    // ============================================================

    private void OnHomeClicked(
        object sender,
        TappedEventArgs e)
    {
        ShowHomePage();
    }


    private void OnSkillsClicked(
        object sender,
        TappedEventArgs e)
    {
        ShowSkillsPage();
    }


    private void OnCombatClicked(
        object sender,
        TappedEventArgs e)
    {
        ShowCombatPage();
    }


    private void OnInventoryClicked(
        object sender,
        TappedEventArgs e)
    {
        ShowInventoryPage();
    }

    private void OnCollectionLogClicked(
        object sender,
        TappedEventArgs e)
    {
        ShowCollectionLogPage();
    }


    private void OnSettingsClicked(
        object sender,
        TappedEventArgs e)
    {
        ShowSettingsPage();
    }
}
