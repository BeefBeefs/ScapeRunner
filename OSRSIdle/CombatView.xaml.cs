namespace OSRSIdle;

public partial class CombatView : ContentView
{
    private readonly Player _player;
    private readonly CombatManager _combatManager;

    private Enemy? _lastEnemy;
    private Enemy? _activeDropEnemy;
    private List<EnemyDropVisual> _activeEnemyDropVisuals = new();
    private readonly Dictionary<EnemyTier, TierSectionState> _tierSections = new();
    private readonly Dictionary<Enemy, EnemyCardState> _enemyCardStates = new();
    private int _areaNavigationGeneration;
    private EnemyTier? _expandedTier;
    private bool _enemyListBuilt;

    private bool _resumeDamageBarsOnRefresh;
    private double _playerAttackProgress;
    private double _enemyAttackProgress;

    private bool _autoFightEnabled;
    private CancellationTokenSource? _autoFightCancellation;
    private int _autoFightGeneration;
    private int _combatUiUpdatePending;
    private bool _disposed;
    private bool _isActive;
    private bool _hasBeenDisplayed;
    private bool _playerDamageBarInitialized;
    private bool _enemyDamageBarInitialized;

    private readonly GraphicsView _playerHitSplat;
    private readonly GraphicsView _enemyHitSplat;

    private const int AutoFightDelayTicks = 12;
    private sealed class EnemyCardState
    {
        public required Label NameLabel { get; init; }
        public required Label? RequirementLabel { get; init; }
        public required GoldSliceButton FightButton { get; init; }
        public required List<EnemyDropVisual> Drops { get; init; }
        public required Border CardPanel { get; init; }
    }

    private sealed class TierSectionState
    {
        public required Border Banner { get; init; }
        public required Label ToggleLabel { get; init; }
        public required Label CompletionLabel { get; init; }
        public required VerticalStackLayout List { get; init; }
        public required IReadOnlyList<Enemy> Enemies { get; init; }
        public Border? FirstEnemyCard { get; set; }
        public int BuiltCount { get; set; }
    }

    private sealed class EnemyDropVisual
    {
        public required Drop Drop { get; init; }
        public required Image Icon { get; init; }
        public required Grid IconHost { get; init; }
        public required Label Label { get; init; }
        public bool RarityDecorated { get; set; }
    }

    private static string FormatEnemyName(Enemy enemy)
    {
        return $"{enemy.Name} lvl {enemy.CombatLevel}";
    }

    private Color GetEnemyDifficultyColor(Enemy enemy)
    {
        int levelDifference = enemy.CombatLevel - _player.GetCombatLevel();

        return levelDifference switch
        {
            <= 0 => Colors.White,
            <= 5 => Colors.Yellow,
            <= 15 => Color.FromArgb("#FF9D2E"),
            _ => Colors.Red
        };
    }

    private void UpdateEnemyNameAppearance()
    {
        EnemyNameLabel.TextColor = _combatManager.IsAutoFightRespawning
            ? Color.FromArgb("#707070")
            : Colors.White;
    }

    private void UpdateEnemyPortraitAppearance()
    {
        // Keep the defeated enemy visible while it respawns, but make the
        // portrait clearly inactive. The drops and combat information remain
        // available for the entire countdown.
        EnemyPortraitFrame.IsVisible = true;
        EnemyIcon.Opacity = _combatManager.IsAutoFightRespawning
            ? 0.2
            : 1;
    }

    private void UpdateAutoFightIndicator(bool enabled)
    {
        AutoFightIndicator.BackgroundColor = enabled
            ? Color.FromArgb("#2D7D46")
            : Color.FromArgb("#404040");
    }

    // ============================================================
    // PLAYER DEFEATED
    // ============================================================

    private void OnPlayerDefeated()
    {
        if (_disposed || !_isActive)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_disposed || !_isActive)
                return;

            // Completely disable auto fight.
            DisableAutoFight();

            // Stop any remaining combat.
            _combatManager.StopCombat();

            // Reset combat UI.
            ResetDamageBars();

            SetAttackProgress(0, 0);

            StopCombatButton.IsVisible = false;
            FightAgainButton.IsVisible = false;
            EnemySelectButton.IsVisible = false;
            ActiveEnemyDropsHost.IsVisible = false;

            LootResults.IsVisible = false;
            LootContainer.Children.Clear();

            CombatStatusLabel.Text = "";
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _isActive = false;

        _combatManager.CombatStarted -= OnCombatStarted;
        _combatManager.CombatUpdated -= OnCombatUpdated;
        _combatManager.CombatStopped -= OnCombatStopped;
        _combatManager.AttackPerformed -= OnAttackPerformed;
        _combatManager.AutoEatPerformed -= OnAutoEatPerformed;
        _combatManager.EnemyDefeated -= OnEnemyDefeated;
        _combatManager.PlayerDefeated -= OnPlayerDefeated;
        _player.CollectionLog.DropDiscovered -= OnDropDiscovered;

        PlayerDamageFill.AbortAnimation("damageTrail");
        EnemyDamageFill.AbortAnimation("damageTrail");
        _playerHitSplat.AbortAnimation("hitSplatMove");
        _playerHitSplat.AbortAnimation("hitSplatFade");
        _enemyHitSplat.AbortAnimation("hitSplatMove");
        _enemyHitSplat.AbortAnimation("hitSplatFade");

        _autoFightCancellation?.Cancel();
        _autoFightCancellation?.Dispose();
    }

    public void SetActive(bool active)
    {
        _isActive = active;
        if (!active)
        {
            ++_areaNavigationGeneration;
            PlayerDamageFill.AbortAnimation("damageTrail");
            EnemyDamageFill.AbortAnimation("damageTrail");
        }
        if (active)
        {
            _hasBeenDisplayed = true;
            if (_expandedTier is EnemyTier tier &&
                _tierSections.TryGetValue(tier, out TierSectionState? section) &&
                section.BuiltCount < section.Enemies.Count && EnemySelectionView.IsVisible)
                _ = ExpandAreaAndScrollAsync(tier);
        }
    }

    public bool HasBeenDisplayed => _hasBeenDisplayed;

    public CombatView(
        Player player,
        CombatManager combatManager)
    {
        InitializeComponent();

        _player = player;
        _combatManager = combatManager;

        _playerHitSplat = CreateHitSplat();
        _enemyHitSplat = CreateHitSplat();
        DamagePopupLayer.Children.Add(_playerHitSplat);
        EnemyHitSplatLayer.Children.Add(_enemyHitSplat);

        PlayerHPBar.SizeChanged += (sender, e) =>
        {
            UpdateHPBars();
        };

        EnemyHPBar.SizeChanged += (sender, e) =>
        {
            UpdateHPBars();
        };

        PlayerAttackBar.SizeChanged += (sender, e) =>
        {
            UpdateAttackBars();
        };

        EnemyAttackBar.SizeChanged += (sender, e) =>
        {
            UpdateAttackBars();
        };

        _combatManager.CombatStarted += OnCombatStarted;
        _combatManager.CombatUpdated += OnCombatUpdated;
        _combatManager.CombatStopped += OnCombatStopped;
        _combatManager.AttackPerformed += OnAttackPerformed;
        _combatManager.AutoEatPerformed += OnAutoEatPerformed;
        _combatManager.EnemyDefeated += OnEnemyDefeated;
        _combatManager.PlayerDefeated += OnPlayerDefeated;
        _player.CollectionLog.DropDiscovered += OnDropDiscovered;

        if (_combatManager.IsInCombat)
        {
            EnemySelectionView.IsVisible = false;
            CombatViewLayout.IsVisible = true;

            StopCombatButton.IsVisible = true;
            FightAgainButton.IsVisible = false;
            EnemySelectButton.IsVisible = false;
            LootResults.IsVisible = false;

            Enemy? enemy = _combatManager.CurrentEnemy;

            if (enemy != null)
            {
                _lastEnemy = enemy;

                EnemyNameLabel.Text = FormatEnemyName(enemy);
                EnemyTraitsLabel.FormattedText = BuildEnemyTraitsText(enemy);
                EnemyDescriptionLabel.Text = enemy.Description;
                EnemyIcon.Source = enemy.LargeIconImage;
                UpdateEnemyPortraitAppearance();
                UpdateEnemyCombatStats(enemy);

                UpdateEnemyKillCount(enemy);

                CombatStatusLabel.Text =
                    $"Fighting {enemy.Name}";
            }

            UpdateHPBars();
            UpdateAttackBars();
        }
        else if (_combatManager.LastDefeatedEnemy != null)
        {
            Enemy defeatedEnemy = _combatManager.LastDefeatedEnemy;

            _lastEnemy = defeatedEnemy;

            EnemySelectionView.IsVisible = false;
            CombatViewLayout.IsVisible = true;

            EnemyNameLabel.Text = FormatEnemyName(defeatedEnemy);
            EnemyTraitsLabel.FormattedText = BuildEnemyTraitsText(defeatedEnemy);
            EnemyDescriptionLabel.Text = defeatedEnemy.Description;
            EnemyIcon.Source = defeatedEnemy.LargeIconImage;
            UpdateEnemyPortraitAppearance();
            UpdateEnemyCombatStats(defeatedEnemy);
            ActiveEnemyDropsHost.IsVisible = true;

            UpdateEnemyKillCount(defeatedEnemy);

            CombatStatusLabel.Text =
                $"{defeatedEnemy.Name} defeated!";

            StopCombatButton.IsVisible = false;
            FightAgainButton.IsVisible = true;
            EnemySelectButton.IsVisible = true;

            LootResults.IsVisible = true;

            BuildLootResults(defeatedEnemy);

            SetAttackProgress(0, 0);

            UpdateHPBars();
        }
        else
        {
            EnemySelectionView.IsVisible = true;
            CombatViewLayout.IsVisible = false;
        }

        UpdateEnemyNameAppearance();
    }

    public void PreloadEnemyList()
    {
        if (_enemyListBuilt)
            return;

        BuildEnemyList();
        _enemyListBuilt = true;
    }

    private void BuildEnemyList()
    {
        EnemyContainer.Children.Clear();
        _tierSections.Clear();
        _enemyCardStates.Clear();

        foreach (EnemyTier tier in Enum.GetValues<EnemyTier>())
        {
            IReadOnlyList<Enemy> enemiesInTier =
                StartupDataCache.GetEnemies(tier);

            // Don't display empty tiers.
            if (enemiesInTier.Count == 0)
                continue;

            BuildTierSection(tier, enemiesInTier);
        }
    }
    private void BuildTierSection(
    EnemyTier tier,
    IReadOnlyList<Enemy> enemies)
    {
        (string areaName, string description, string imageSource) =
            GetTierPresentation(tier);

        Label toggleLabel = new()
        {
            Text = $"▶  Tier {(int)tier + 1} • {GetTierLevelRange(tier)}",
            FontSize = 11,
            TextColor = Colors.White,
            Margin = new Thickness(8, 5, 0, 0),
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            InputTransparent = true
        };

        Label completionLabel = new()
        {
            Text = FormatAreaCompletion(enemies),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Fill,
            HorizontalTextAlignment = TextAlignment.Center,
            InputTransparent = true
        };
        UpdateAreaCompletionAppearance(completionLabel, enemies);

        VerticalStackLayout textLayout = new()
        {
            Spacing = 2,
            Padding = new Thickness(34, 8),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            Children =
            {
                new Label
                {
                    Text = areaName,
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#FFE26A"),
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Fill,
                    InputTransparent = true
                },
                new Label
                {
                    Text = description,
                    FontSize = 12,
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Fill,
                    LineBreakMode = LineBreakMode.WordWrap,
                    InputTransparent = true
                },
                completionLabel
            }
        };

        Grid bannerContent = new()
        {
            IsClippedToBounds = true
        };
        Image areaImage = new()
        {
            Source = ImageSource.FromFile(imageSource),
            Aspect = Aspect.AspectFill,
            // Tint by blending the image with the banner background. A separate
            // translucent BoxView can be composited as an opaque layer on Android,
            // which covers the successfully loaded bitmap with a solid gray panel.
            Opacity = 0.59,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = true
        };
        bannerContent.Children.Add(areaImage);

        bannerContent.Children.Add(textLayout);

        bannerContent.Children.Add(toggleLabel);

        Border tierBanner = new()
        {
            HeightRequest = UiLayoutMetrics.TierBannerHeight,
            Stroke = Color.FromArgb("#D99032"),
            StrokeThickness = 2,
            BackgroundColor = Color.FromArgb("#303030"),
            HorizontalOptions = LayoutOptions.Fill,
            Content = bannerContent,
            AutomationId = $"CombatTier{(int)tier + 1}Banner"
        };

        VerticalStackLayout enemyList =
            new VerticalStackLayout
            {
                Spacing = 12,
                IsVisible = false
            };

        _tierSections[tier] = new TierSectionState
        {
            Banner = tierBanner,
            ToggleLabel = toggleLabel,
            CompletionLabel = completionLabel,
            List = enemyList,
            Enemies = enemies
        };

        TapGestureRecognizer tapGesture = new();
        tapGesture.Tapped += async (sender, e) =>
            await ExpandAreaAndScrollAsync(tier);
        tierBanner.GestureRecognizers.Add(tapGesture);

        EnemyContainer.Children.Add(tierBanner);
        EnemyContainer.Children.Add(enemyList);
    }

    private async Task ExpandAreaAndScrollAsync(EnemyTier selectedTier)
    {
        if (!_tierSections.TryGetValue(selectedTier, out TierSectionState? selectedSection))
            return;

        int navigationGeneration = ++_areaNavigationGeneration;
        _expandedTier = selectedTier;

        foreach ((EnemyTier sectionTier, TierSectionState section) in _tierSections)
        {
            bool isSelected = sectionTier == selectedTier;
            section.List.IsVisible = isSelected;
            section.ToggleLabel.Text =
                $"{(isSelected ? "▼" : "▶")}  Tier {(int)sectionTier + 1} • " +
                GetTierLevelRange(sectionTier);
            section.Banner.Stroke = isSelected
                ? Color.FromArgb("#42A85A")
                : Color.FromArgb("#D99032");
        }

        // Show the selected banner immediately, then build one card per UI
        // turn. Interrupted areas retain their cards and resume on demand.
        await Task.Delay(16);
        while (selectedSection.BuiltCount < selectedSection.Enemies.Count)
        {
            if (_disposed || !_isActive ||
                navigationGeneration != _areaNavigationGeneration ||
                !EnemySelectionView.IsVisible)
                return;

            Enemy enemy = selectedSection.Enemies[selectedSection.BuiltCount];
            Border card = BuildEnemyCard(enemy, selectedSection.List);
            selectedSection.FirstEnemyCard ??= card;
            selectedSection.BuiltCount++;
            await Task.Delay(16);
        }

        // Let MAUI measure the newly visible list before calculating its
        // ScrollView position. The generation check prevents a rapid second
        // tap from completing an obsolete scroll operation.
        await Task.Delay(35);

        if (_disposed || !_isActive || navigationGeneration != _areaNavigationGeneration ||
            !EnemySelectionView.IsVisible ||
            !selectedSection.List.IsVisible)
        {
            return;
        }

        if (selectedSection.FirstEnemyCard != null)
        {
            try
            {
                await EnemySelectionView.ScrollToAsync(
                    selectedSection.FirstEnemyCard,
                    ScrollToPosition.Start,
                    true).WaitAsync(TimeSpan.FromSeconds(1));
            }
            catch (TimeoutException)
            {
                // Some native scroll requests finish without a completion
                // callback. The area is already open and usable.
            }
        }
    }

    private static (string Name, string Description, string ImageSource)
        GetTierPresentation(EnemyTier tier)
    {
        return tier switch
        {
            EnemyTier.Tier1 =>
                ("Greenvale", "Bright grasslands, lakes, and castle countryside.", "tier_greenvale.png"),
            EnemyTier.Tier2 =>
                ("Mirewood", "Gloomy, overgrown swamp and dead forest.", "tier_mirewood.png"),
            EnemyTier.Tier3 =>
                ("Frostpeak", "Snowy mountains and frozen mines.", "tier_frostpeak.png"),
            EnemyTier.Tier4 =>
                ("Emberfall", "Volcanic wasteland and lava fields.", "tier_emberfall.png"),
            EnemyTier.Tier5 =>
                ("Sandswept Ruins", "Ancient desert civilization and buried temples.", "tier_sandswept_ruins.png"),
            EnemyTier.Tier6 =>
                ("The Astral Reach", "Floating islands, purple skies, and arcane ruins.", "tier_astral_reach.png"),
            EnemyTier.Tier7 =>
                ("The Umbral Expanse", "Corrupted realm beneath the black sun.", "tier_umbral_expanse.png"),
            _ => (tier.ToString(), string.Empty, "tier_greenvale.png")
        };
    }

    private static string GetTierLevelRange(EnemyTier tier)
    {
        IReadOnlyList<Enemy> enemies = StartupDataCache.GetEnemies(tier);

        return enemies.Count == 0
            ? "No enemies"
            : $"lvl {enemies[0].CombatLevel}-{enemies[^1].CombatLevel}";
    }

    private string FormatAreaCompletion(IReadOnlyList<Enemy> enemies)
    {
        int total = 0;
        int obtained = 0;

        foreach (Enemy enemy in enemies)
        {
            total += _player.CollectionLog.GetDropCount(enemy);
            obtained += _player.CollectionLog.GetObtainedDropCount(enemy);
        }

        int percentage = total == 0
            ? 0
            : (int)Math.Round(obtained * 100d / total);

        return $"Collection Log: {percentage}%";
    }

    private void UpdateAreaCompletionAppearance(
        Label label,
        IReadOnlyList<Enemy> enemies)
    {
        int total = 0;
        int obtained = 0;

        foreach (Enemy enemy in enemies)
        {
            total += _player.CollectionLog.GetDropCount(enemy);
            obtained += _player.CollectionLog.GetObtainedDropCount(enemy);
        }

        double ratio = total == 0 ? 0 : obtained / (double)total;
        label.TextColor = ratio >= 1
            ? Color.FromArgb("#42A85A")
            : ratio >= 0.5
                ? Color.FromArgb("#FFE26A")
                : Color.FromArgb("#E64A4A");
    }

    private void UpdateAreaCompletionLabels()
    {
        foreach ((EnemyTier tier, TierSectionState section) in _tierSections)
        {
            IReadOnlyList<Enemy> enemies = StartupDataCache.GetEnemies(tier);
            section.CompletionLabel.Text = FormatAreaCompletion(enemies);
            UpdateAreaCompletionAppearance(
                section.CompletionLabel,
                enemies);
        }
    }

    /// <summary>
    /// Reconciles the delayed-damage overlays after this view has been hidden.
    /// The overlays are visual-only, so they should never outlive the combat
    /// screen that was displaying their animation.
    /// </summary>
    public void RefreshCombatDisplay()
    {
        PlayerDamageFill.AbortAnimation("damageTrail");
        EnemyDamageFill.AbortAnimation("damageTrail");


        // Resume the visual catch-up from its current width once layout is
        // available. SizeChanged will call UpdateHPBars again if either bar
        // has not been measured yet.
        _resumeDamageBarsOnRefresh = true;

        UpdateHPBars();
        UpdateAttackBars();
    }

    private Border BuildEnemyCard(
    Enemy enemy,
    VerticalStackLayout parent)
    {
        VerticalStackLayout enemyCard =
            new VerticalStackLayout
            {
                Spacing = 6
            };

        Label nameLabel =
            new Label
            {
                Text = FormatEnemyName(enemy),
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = GetEnemyDifficultyColor(enemy),
                VerticalOptions = LayoutOptions.Center
            };

        Image enemyIcon =
            new Image
            {
                Source = enemy.IconImage,
                WidthRequest = 32,
                HeightRequest = 32,
                Aspect = Aspect.AspectFit
            };

        Label speedLabel =
            new Label
            {
                Text =
                    $"Speed: {enemy.AttackSpeedTicks} ticks",
                FontSize = 12
            };

        Label estimateLabel = new()
        {
            Text = BuildEnemyEstimateText(enemy),
            FontSize = 10,
            TextColor = GetEnemyDifficultyColor(enemy)
        };

        Label traitsLabel = new()
        {
            FontSize = 11,
            TextColor = Color.FromArgb("#FFB347"),
            HorizontalOptions = LayoutOptions.Fill,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };
        traitsLabel.FormattedText = BuildEnemyTraitsText(enemy);

        VerticalStackLayout enemyInfoContent =
            new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    CreateEnemyStatRow("skill_hp.png", $"HP: {enemy.HP}"),
                    CreateEnemyStatRow("skill_attack.png", $"Attack: {enemy.Attack}"),
                    CreateEnemyStatRow("skill_strength.png", $"Strength: {enemy.Strength}"),
                    CreateEnemyStatRow("skill_defense.png", $"Defense: {enemy.Defense}"),
                    speedLabel,
                    estimateLabel,
                    traitsLabel
                }
            };

        Border enemyInfoPanel =
            new Border
            {
                Stroke = Color.FromArgb("#D99032"),
                StrokeThickness = 1,
                Padding = 6,
                Content = enemyInfoContent
            };

        Label? requirementLabel =
            enemy.RequiredSkillLevel <= 0 ||
            string.IsNullOrWhiteSpace(enemy.RequiredSkillName)
                ? null
                : new Label
                {
                    Text =
                        $"Requires {enemy.RequiredSkillIcon} " +
                        $"{enemy.RequiredSkillName} level " +
                        $"{enemy.RequiredSkillLevel}",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = MeetsSkillRequirement(enemy)
                        ? Colors.Green
                        : Colors.Red
                };

        Border dropTablePanel = CreateEnemyDropTable(
            enemy,
            out List<EnemyDropVisual> dropVisuals);

        Grid detailsRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(0.44, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(0.56, GridUnitType.Star))
            },
            ColumnSpacing = 6,
            HorizontalOptions = LayoutOptions.Fill
        };

        detailsRow.Add(enemyInfoPanel, 0);
        detailsRow.Add(dropTablePanel, 1);

        HorizontalStackLayout enemyHeader = new()
        {
            Spacing = 7,
            HorizontalOptions = LayoutOptions.Center,
            Children = { enemyIcon, nameLabel }
        };

        bool meetsRequirement = MeetsSkillRequirement(enemy);

        GoldSliceButton fightButton =
            new GoldSliceButton
            {
                Text = $"Fight {enemy.Name}",
                FontSize = 16,
                IsEnabled = meetsRequirement,
                Variant = meetsRequirement
                    ? GoldSliceButtonVariant.Green
                    : GoldSliceButtonVariant.Red,
                TextColor = Colors.White
            };

        fightButton.Clicked += async (sender, e) =>
        {
            if (!MeetsSkillRequirement(enemy))
            {
                await ShowSkillRequirementMessage(enemy);
                return;
            }

            SelectEnemy(enemy);
        };

        enemyCard.Children.Add(enemyHeader);
        enemyCard.Children.Add(detailsRow);

        if (requirementLabel != null)
        {
            enemyCard.Children.Add(requirementLabel);
        }

        enemyCard.Children.Add(fightButton);

        Border cardPanel = GamePanel.Create(enemyCard, 15);
        bool collectionComplete = _player.CollectionLog.IsComplete(enemy);

        cardPanel.BackgroundColor = collectionComplete
            ? Color.FromArgb("#2D7D46")
            : Color.FromArgb("#E64A4A4A");
        cardPanel.Opacity = collectionComplete ? 0.5 : 1;

        _enemyCardStates[enemy] =
            new EnemyCardState
            {
                NameLabel = nameLabel,
                RequirementLabel = requirementLabel,
                FightButton = fightButton,
                Drops = dropVisuals,
                CardPanel = cardPanel
            };

        parent.Children.Add(cardPanel);
        return cardPanel;
    }

    private Border CreateEnemyDropTable(
        Enemy enemy,
        out List<EnemyDropVisual> dropVisuals,
        bool compact = false)
    {
        VerticalStackLayout dropsList = new()
        {
            Spacing = 3
        };

        dropVisuals = new List<EnemyDropVisual>();

        foreach (Drop drop in DropDisplayOrder.ForEnemy(enemy))
        {
            bool obtained = _player.CollectionLog.HasReceivedDrop(
                enemy,
                drop.Item);

            Image dropIcon = new()
            {
                Source = drop.Item.IconImage,
                WidthRequest = 16,
                HeightRequest = 16,
                Aspect = Aspect.AspectFit,
                IsVisible = obtained,
                VerticalOptions = LayoutOptions.Start
            };

            Label dropLabel = new()
            {
                Text = compact
                    ? BuildCompactDropLabelText(drop, obtained)
                    : BuildDropLabelText(drop, obtained),
                FontSize = compact ? 9 : 11,
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = obtained
                    ? GetDropRarityColor(drop.Rarity)
                    : Colors.Red
            };
            if (obtained && RarityVisuals.IsRainbowRare(drop.Chance))
                dropLabel.FormattedText = RarityVisuals.RainbowText(dropLabel.Text ?? string.Empty);
            Grid dropRow = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 4
            };
            Grid dropIconHost = new()
            {
                WidthRequest = 16,
                HeightRequest = 16,
                IsClippedToBounds = false
            };
            dropIconHost.Children.Add(dropIcon);
            if (obtained)
                AddRarityDecoration(dropIconHost, drop.Rarity, 16);
            dropRow.Add(dropIconHost, 0);
            dropRow.Add(dropLabel, 1);
            dropsList.Children.Add(dropRow);

            dropVisuals.Add(new EnemyDropVisual
            {
                Drop = drop,
                Icon = dropIcon,
                IconHost = dropIconHost,
                RarityDecorated = obtained,
                Label = dropLabel
            });
        }

        ScrollView dropScroll = new()
        {
            Content = dropsList,
            HeightRequest = compact ? 104 : 142,
            VerticalScrollBarVisibility = ScrollBarVisibility.Default
        };

        VerticalStackLayout content = new()
        {
            Spacing = 3,
            Children =
            {
                new Label
                {
                    Text = "DROPS",
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#D99032"),
                    HorizontalOptions = LayoutOptions.Center
                },
                dropScroll
            }
        };

        return new Border
        {
            Stroke = Color.FromArgb("#D99032"),
            StrokeThickness = compact ? 2 : 1,
            Padding = compact ? 4 : 5,
            WidthRequest = compact ? 140 : -1,
            HeightRequest = compact ? 140 : -1,
            BackgroundColor = Color.FromArgb("#E6303030"),
            Content = content
        };
    }

    private static string BuildCompactDropLabelText(
        Drop drop,
        bool obtained)
    {
        string itemName = obtained ? drop.Item.Name : "???";
        return $"{itemName}\n{FormatDropRarity(drop.Rarity)}";
    }

    private void UpdateActiveEnemyDropTable(Enemy enemy)
    {
        if (_activeDropEnemy == enemy &&
            ActiveEnemyDropsHost.Content != null)
        {
            RefreshActiveEnemyDropVisuals();
            return;
        }

        ActiveEnemyDropsHost.Content = CreateEnemyDropTable(
            enemy,
            out _activeEnemyDropVisuals,
            compact: true);
        _activeDropEnemy = enemy;
    }

    private void RefreshActiveEnemyDropVisuals()
    {
        if (_activeDropEnemy == null)
            return;

        foreach (EnemyDropVisual visual in _activeEnemyDropVisuals)
        {
            bool obtained = _player.CollectionLog.HasReceivedDrop(
                _activeDropEnemy,
                visual.Drop.Item);

            visual.Icon.IsVisible = obtained;
            visual.Label.Text = BuildCompactDropLabelText(
                visual.Drop,
                obtained);
            visual.Label.TextColor = obtained
                ? GetDropRarityColor(visual.Drop.Rarity)
                : Colors.Red;
        }
    }

    private string BuildDropLabelText(Drop drop, bool obtained)
    {
        string itemName = obtained ? drop.Item.Name : "???";

        return $"{itemName}\n{FormatDropRarity(drop.Rarity)} · " +
               $"{FormatDropChance(drop.Chance)} · " +
               $"{FormatQuantity(drop.MinQuantity, drop.MaxQuantity)}";
    }

    private static Color GetDropRarityColor(DropRarity rarity)
    {
        return GameThemeCache.GetRarityColor(rarity);
    }

    public void RefreshEnemyList()
    {
        if (!_enemyListBuilt)
            return;

        foreach ((Enemy enemy, EnemyCardState state) in _enemyCardStates)
        {
            RefreshEnemyCard(enemy, state);
        }

        UpdateAreaCompletionLabels();
    }

    private void RefreshEnemyCard(Enemy enemy, EnemyCardState state)
    {
        bool collectionComplete = _player.CollectionLog.IsComplete(enemy);

        state.CardPanel.BackgroundColor = collectionComplete
            ? Color.FromArgb("#2D7D46")
            : Color.FromArgb("#E64A4A4A");
        state.CardPanel.Opacity = collectionComplete ? 0.5 : 1;

        state.NameLabel.TextColor = GetEnemyDifficultyColor(enemy);

        bool meetsRequirement = MeetsSkillRequirement(enemy);

        if (state.RequirementLabel != null)
        {
            state.RequirementLabel.TextColor = meetsRequirement
                ? Colors.Green
                : Colors.Red;
        }

        state.FightButton.IsEnabled = meetsRequirement;
        state.FightButton.Variant = meetsRequirement
            ? GoldSliceButtonVariant.Green
            : GoldSliceButtonVariant.Red;

        foreach (EnemyDropVisual dropVisual in state.Drops)
        {
            bool obtained = _player.CollectionLog.HasReceivedDrop(
                enemy,
                dropVisual.Drop.Item);

            dropVisual.Icon.IsVisible = obtained;
            dropVisual.Label.Text = BuildDropLabelText(
                dropVisual.Drop,
                obtained);
            dropVisual.Label.TextColor = obtained
                ? GetDropRarityColor(dropVisual.Drop.Rarity)
                : Colors.Red;

            if (obtained && !dropVisual.RarityDecorated)
            {
                dropVisual.RarityDecorated = true;
                AddRarityDecoration(
                    dropVisual.IconHost,
                    dropVisual.Drop.Rarity,
                    16);
            }
        }
    }

    private static HorizontalStackLayout CreateEnemyStatRow(
        string iconSource,
        string text)
    {
        return new HorizontalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Image
                {
                    Source = iconSource,
                    WidthRequest = 16,
                    HeightRequest = 16,
                    Aspect = Aspect.AspectFit
                },
                new Label
                {
                    Text = text,
                    FontSize = 12,
                    TranslationY = 3
                }
            }
        };
    }

    private string BuildEnemyEstimateText(Enemy enemy)
    {
        double playerHitChance = Math.Clamp(
            (double)_player.GetEffectiveAttackLevel() /
            (_player.GetEffectiveAttackLevel() + enemy.Defense),
            0.05d,
            0.95d);
        double playerAverageHit = (Math.Max(1, _player.GetMaxHit()) + 1d) / 2d;
        double playerDps = playerHitChance * playerAverageHit /
            (_player.GetAttackSpeedTicks() * ActivityMetrics.SecondsPerTick);
        double enemyHitChance = Math.Clamp(
            (double)enemy.Attack /
            (enemy.Attack + _player.GetEffectiveDefenseLevel()),
            0.05d,
            0.95d);
        double enemyAverageHit = (Math.Max(1, enemy.Strength / 3 + 1) + 1d) / 2d;
        double incomingDps = enemyHitChance * enemyAverageHit /
            (Math.Max(1, enemy.AttackSpeedTicks) * ActivityMetrics.SecondsPerTick);
        string killTime = playerDps <= 0
            ? "—"
            : ActivityMetrics.FormatDuration(enemy.HP / playerDps);
        string bonus = ProgressionBonuses.Format(ProgressionBonuses.CombatXpPercent(_player, enemy));
        return $"Est. kill: {killTime} • hit {playerHitChance:P0}\n" +
               $"Incoming: {incomingDps:0.0} DPS" +
               (string.IsNullOrEmpty(bonus) ? string.Empty : $"  •  {bonus}");
    }

    private static string FormatEnemyTraits(Enemy enemy)
    {
        return enemy.Traits.Count == 0
            ? string.Empty
            : "Traits: " + string.Join(" • ", enemy.Traits.Select(EnemyTraitRules.Name));
    }

    private static FormattedString BuildEnemyTraitsText(Enemy enemy)
    {
        FormattedString result = new();
        if (enemy.Traits.Count == 0)
            return result;

        result.Spans.Add(new Span { Text = "Traits:\n", TextColor = Colors.White });
        for (int index = 0; index < enemy.Traits.Count; index++)
        {
            EnemyTrait trait = enemy.Traits[index];
            result.Spans.Add(new Span
            {
                Text = $"{EnemyTraitRules.Name(trait)}: " +
                       EnemyTraitRules.Description(trait) +
                       (index < enemy.Traits.Count - 1 ? "\n" : string.Empty),
                TextColor = EnemyTraitRules.Color(trait)
            });
        }
        return result;
    }

    private void OnDropDiscovered(Item item)
    {
        if (_disposed || !_isActive)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_disposed || !_isActive)
                return;

            RefreshEnemyCardsForItem(item);
            UpdateAreaCompletionLabels();
        });
    }

    private void RefreshEnemyCardsForItem(Item item)
    {
        foreach (Enemy enemy in StartupDataCache.GetEnemiesDropping(item))
        {
            if (_enemyCardStates.TryGetValue(enemy, out EnemyCardState? state))
                RefreshEnemyCard(enemy, state);
        }

        if (_activeDropEnemy?.DropTable.Drops.Any(
                drop => drop.Item == item) == true)
        {
            RefreshActiveEnemyDropVisuals();
        }
    }


    private bool MeetsSkillRequirement(Enemy enemy)
    {
        if (enemy.RequiredSkillLevel <= 0 ||
            string.IsNullOrWhiteSpace(enemy.RequiredSkillName))
        {
            return true;
        }

        Skill? requiredSkill =
            _player.Skills.FirstOrDefault(skill =>
                skill.Name == enemy.RequiredSkillName);

        return requiredSkill != null &&
            requiredSkill.Level >= enemy.RequiredSkillLevel;
    }


    private async Task ShowSkillRequirementMessage(Enemy enemy)
    {
        await CustomDialogService.ShowAsync(
            "Skill requirement",
            $"{enemy.Name} requires {enemy.RequiredSkillIcon} " +
            $"{enemy.RequiredSkillName} level " +
            $"{enemy.RequiredSkillLevel} before you can fight it.",
            "OK");
    }

    private string FormatDropChance(double chance)
    {
        if (chance >= 1.0)
            return "100%";

        if (chance <= 0)
            return "0%";

        double percentage = chance * 100;

        if (percentage >= 0.01)
            return $"{percentage:0.##}%";

        double denominator = 1.0 / chance;

        return $"1/{Math.Round(denominator):0}";
    }

    private string FormatQuantity(int minimum, int maximum)
    {
        if (minimum == maximum)
            return minimum.ToString();

        return $"{minimum}-{maximum}";
    }

    private void ResetDamageBars()
    {
        PlayerDamageFill.AbortAnimation("damageTrail");
        EnemyDamageFill.AbortAnimation("damageTrail");

        PlayerDamageFill.ScaleX = 1;
        EnemyDamageFill.ScaleX = 1;
    }

    private void SelectEnemy(Enemy enemy)
    {
        StartCombatNow(enemy);
    }

    public void StartCombatNow(Enemy enemy)
    {
        if (!MeetsSkillRequirement(enemy))
            return;

        DisableAutoFight();

        _lastEnemy = enemy;

        ResetDamageBars();

        EnemySelectionView.IsVisible = false;
        CombatViewLayout.IsVisible = true;

        StopCombatButton.IsVisible = true;
        FightAgainButton.IsVisible = false;
        EnemySelectButton.IsVisible = false;

        LootResults.IsVisible = false;
        LootContainer.Children.Clear();

        _combatManager.StartCombat(enemy);
    }

    private void OnCombatStarted()
    {
        if (_disposed || !_isActive)
            return;

        Enemy? startedEnemy = _combatManager.CurrentEnemy;
        if (startedEnemy == null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Combat can be stopped or switched before this queued UI update
            // runs. Never let an old event overwrite the current encounter.
            if (_disposed ||
                !_isActive ||
                _combatManager.CurrentEnemy != startedEnemy ||
                !CombatViewLayout.IsVisible)
                return;

            _lastEnemy = startedEnemy;

            EnemyNameLabel.Text = FormatEnemyName(startedEnemy);
            EnemyTraitsLabel.FormattedText = BuildEnemyTraitsText(startedEnemy);
            EnemyDescriptionLabel.Text = startedEnemy.Description;
            EnemyIcon.Source = startedEnemy.LargeIconImage;
            UpdateEnemyPortraitAppearance();
            ActiveEnemyDropsHost.IsVisible = true;
            UpdateEnemyCombatStats(startedEnemy);

            UpdateEnemyNameAppearance();

            UpdateEnemyKillCount(startedEnemy);

            CombatStatusLabel.Text =
                $"Fighting {startedEnemy.Name}";

            StopCombatButton.IsVisible = true;
            FightAgainButton.IsVisible = false;
            EnemySelectButton.IsVisible = false;
            LootResults.IsVisible = false;

            ResetDamageBars();

            UpdateHPBars();
            UpdateAttackBars();
        });
    }

    private void OnCombatUpdated()
    {
        if (_disposed ||
            !_isActive ||
            Interlocked.Exchange(ref _combatUiUpdatePending, 1) != 0)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Interlocked.Exchange(ref _combatUiUpdatePending, 0);

            if (_disposed || !_isActive)
                return;

            UpdateHPBars();
            UpdateAttackBars();
        });
    }

    private void UpdateEnemyKillCount(Enemy enemy)
    {
        EnemyKillCountLabel.Text =
            $"Kills: {_player.CollectionLog.GetKillCount(enemy):N0}";
    }

    private void UpdateEnemyCombatStats(Enemy enemy)
    {
        UpdateActiveEnemyDropTable(enemy);

        EnemyCombatStatsLabel.Text =
            $"HP: {enemy.HP}\n" +
            $"Attack: {enemy.Attack}\n" +
            $"Strength: {enemy.Strength}\n" +
            $"Defense: {enemy.Defense}";
    }

    private void UpdateHPBars()
    {
        string playerName =
            $"{_combatManager.Player.Name} lvl {_combatManager.Player.GetCombatLevel()}";
        if (PlayerNameLabel.Text != playerName)
            PlayerNameLabel.Text = playerName;

        bool isWaitingForAutoFightRespawn =
            _combatManager.IsAutoFightRespawning;

        double enemyBarOpacity = isWaitingForAutoFightRespawn
            ? 0.45
            : 1;
        if (Math.Abs(EnemyHPBar.Opacity - enemyBarOpacity) > 0.001)
            EnemyHPBar.Opacity = enemyBarOpacity;

        Color enemyHPTextColor = isWaitingForAutoFightRespawn
            ? Colors.Gray
            : Colors.White;
        if (!Equals(EnemyHPLabel.TextColor, enemyHPTextColor))
            EnemyHPLabel.TextColor = enemyHPTextColor;

        // ============================================================
        // PLAYER HP
        // ============================================================

        int playerMaxHP =
            Math.Max(
                1,
                _player.GetMaxHP());

        int playerCurrentHP =
            Math.Clamp(
                _player.CurrentHP,
                0,
                playerMaxHP);


        // ------------------------------------------------------------
        // Update the actual player HP bar.
        // ------------------------------------------------------------

        double playerPercentage =
            (double)playerCurrentHP /
            playerMaxHP;

        UpdateHPBar(
            PlayerHPFill,
            PlayerDamageFill,
            playerPercentage,
            ref _playerDamageBarInitialized);


        // ------------------------------------------------------------
        // Update the player HP TEXT.
        //
        // IMPORTANT:
        // This uses CurrentHP, NOT the player's maximum HP.
        // ------------------------------------------------------------

        string playerHPText = $"{playerCurrentHP} / {playerMaxHP}";
        if (PlayerHPLabel.Text != playerHPText)
            PlayerHPLabel.Text = playerHPText;


        // ============================================================
        // ENEMY HP
        // ============================================================

        Enemy? enemy =
            _combatManager.CurrentEnemy ??
            _combatManager.LastDefeatedEnemy;


        if (enemy == null)
        {
            if (EnemyHPLabel.Text != "0 / 0")
                EnemyHPLabel.Text = "0 / 0";

            if (EnemyHPFill.ScaleX != 0)
                EnemyHPFill.ScaleX = 0;

            if (EnemyDamageFill.ScaleX != 0)
                EnemyDamageFill.ScaleX = 0;

            ResumeDamageBarsIfNeeded(includeEnemy: false);

            return;
        }


        int enemyMaxHP =
            Math.Max(
                1,
                enemy.HP);

        int enemyCurrentHP =
            Math.Clamp(
                enemy.CurrentHP,
                0,
                enemyMaxHP);


        // ------------------------------------------------------------
        // Update enemy HP bar.
        // ------------------------------------------------------------

        double enemyPercentage =
            (double)enemyCurrentHP /
            enemyMaxHP;

        UpdateHPBar(
            EnemyHPFill,
            EnemyDamageFill,
            enemyPercentage,
            ref _enemyDamageBarInitialized);


        // ------------------------------------------------------------
        // Update enemy HP TEXT.
        // ------------------------------------------------------------

        string enemyHPText = $"{enemyCurrentHP} / {enemyMaxHP}";
        if (EnemyHPLabel.Text != enemyHPText)
            EnemyHPLabel.Text = enemyHPText;

        ResumeDamageBarsIfNeeded(includeEnemy: true);
    }

    private void ResumeDamageBarsIfNeeded(bool includeEnemy)
    {
        if (!_resumeDamageBarsOnRefresh || PlayerHPBar.Width <= 0)
            return;

        if (includeEnemy && EnemyHPBar.Width <= 0)
            return;

        _resumeDamageBarsOnRefresh = false;

        AnimatePlayerDamage();

        if (includeEnemy)
            AnimateEnemyDamage();
    }

    private void UpdateHPBar(
        BoxView hpFill,
        BoxView damageFill,
        double percentage,
        ref bool damageBarInitialized)
    {
        percentage =
            Math.Clamp(
                percentage,
                0,
                1);

        Color fillColor = percentage <= 0.30
            ? Colors.Red
            : percentage <= 0.70
                ? Colors.Yellow
                : Colors.Green;

        if (!Equals(hpFill.BackgroundColor, fillColor))
            hpFill.BackgroundColor = fillColor;

        if (Math.Abs(hpFill.ScaleX - percentage) > 0.001)
            hpFill.ScaleX = percentage;

        if (!damageBarInitialized)
        {
            damageFill.ScaleX = percentage;

            Color damageColor = GetDamageColor(percentage);
            if (!Equals(damageFill.BackgroundColor, damageColor))
                damageFill.BackgroundColor = damageColor;

            damageBarInitialized = true;
        }
    }

    private Color GetDamageColor(double percentage)
    {
        if (percentage <= 0.30)
            return Color.FromArgb("#8B0000");

        if (percentage <= 0.70)
            return Color.FromArgb("#B88600");

        return Color.FromArgb("#006400");
    }

    private void AnimatePlayerDamage()
    {
        int maxHP = _player.GetMaxHP();
        int currentHP = _player.CurrentHP;

        if (maxHP <= 0 || PlayerHPBar.Width <= 0)
            return;

        double newPercentage =
            Math.Clamp(
                (double)currentHP / maxHP,
                0,
                1);

        double oldPercentage = Math.Clamp(PlayerDamageFill.ScaleX, 0, 1);

        if (newPercentage >= oldPercentage)
        {
            PlayerDamageFill.ScaleX = newPercentage;

            return;
        }

        AnimateDamageBar(
            PlayerHPBar,
            PlayerHPFill,
            PlayerDamageFill,
            oldPercentage,
            newPercentage);
    }

    private void AnimateEnemyDamage()
    {
        Enemy? enemy =
            _combatManager.CurrentEnemy ??
            _combatManager.LastDefeatedEnemy;

        if (enemy == null || EnemyHPBar.Width <= 0)
            return;

        int maxHP = enemy.HP;
        int currentHP = enemy.CurrentHP;

        if (maxHP <= 0)
            return;

        double newPercentage =
            Math.Clamp(
                (double)currentHP / maxHP,
                0,
                1);

        double oldPercentage = Math.Clamp(EnemyDamageFill.ScaleX, 0, 1);

        if (newPercentage >= oldPercentage)
        {
            EnemyDamageFill.ScaleX = newPercentage;

            return;
        }

        AnimateDamageBar(
            EnemyHPBar,
            EnemyHPFill,
            EnemyDamageFill,
            oldPercentage,
            newPercentage);
    }

    private void AnimateDamageBar(
        Grid hpBar,
        BoxView hpFill,
        BoxView damageFill,
        double oldPercentage,
        double newPercentage)
    {
        oldPercentage =
            Math.Clamp(oldPercentage, 0, 1);

        newPercentage =
            Math.Clamp(newPercentage, 0, 1);

        if (hpBar.Width <= 0)
            return;

        Color damageColor = GetDamageColor(oldPercentage);
        if (!Equals(damageFill.BackgroundColor, damageColor))
            damageFill.BackgroundColor = damageColor;

        hpFill.ScaleX = newPercentage;

        Color fillColor = newPercentage <= 0.30
            ? Colors.Red
            : newPercentage <= 0.70
                ? Colors.Yellow
                : Colors.Green;

        if (!Equals(hpFill.BackgroundColor, fillColor))
            hpFill.BackgroundColor = fillColor;

        damageFill.ScaleX = oldPercentage;

        new Animation(
            value => damageFill.ScaleX = value,
            oldPercentage,
            newPercentage)
            .Commit(
                damageFill,
                "damageTrail",
                16,
                240,
                Easing.CubicOut);
    }

    private void UpdateAttackBars()
    {
        SetAttackProgress(
            _combatManager.PlayerAttackProgress,
            _combatManager.EnemyAttackProgress);


        // ============================================================
        // PLAYER ATTACK SPEED DISPLAY
        // ============================================================

        int playerAttackSpeed =
            _combatManager.PlayerAttackSpeedTicks;

        string playerAttackText =
            $"Player Attack — {playerAttackSpeed} ticks " +
            $"({playerAttackSpeed * 0.6:0.0}s)";
        if (PlayerAttackLabel.Text != playerAttackText)
            PlayerAttackLabel.Text = playerAttackText;
    }

    private void SetAttackProgress(
        double playerProgress,
        double enemyProgress)
    {
        _playerAttackProgress = Math.Clamp(playerProgress, 0, 1);
        _enemyAttackProgress = Math.Clamp(enemyProgress, 0, 1);

        UpdateAttackProgressBar(
            PlayerAttackFill,
            _playerAttackProgress);

        UpdateAttackProgressBar(
            EnemyAttackFill,
            _enemyAttackProgress);
    }

    private static void UpdateAttackProgressBar(
        BoxView progressFill,
        double progress)
    {
        if (Math.Abs(progressFill.ScaleX - progress) > 0.001)
            progressFill.ScaleX = progress;
    }

    private static GraphicsView CreateHitSplat()
    {
        return new GraphicsView
        {
            WidthRequest = 64,
            HeightRequest = 64,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Opacity = 0,
            InputTransparent = true,
            Drawable = new HitSplatDrawable()
        };
    }

    private void OnAttackPerformed(CombatHitEventArgs e)
    {
        if (_disposed || !_isActive)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_disposed || !_isActive)
                return;

            ShowDamagePopup(e);

            // A hit should only jolt the portrait that was struck. Keeping the
            // surrounding card stationary makes the combat UI easier to read.
            VisualElement target = e.AttackerIsPlayer ? EnemyIcon : PlayerPanel;
            _ = e.Hit && e.Damage > 0
                ? PlayHitReactionAsync(target)
                : PlayMissReactionAsync(target);

            if (!e.Hit || e.Damage <= 0)
                return;

            if (e.AttackerIsPlayer)
                AnimateEnemyDamage();
            else
                AnimatePlayerDamage();
        });
    }

    private void ShowDamagePopup(CombatHitEventArgs e)
    {
        double horizontalOffset =
            Random.Shared.NextDouble() * 12 - 6;

        bool playerAttacked = e.AttackerIsPlayer;
        GraphicsView hitSplat = playerAttacked
            ? _enemyHitSplat
            : _playerHitSplat;

        bool hit = e.Hit && e.Damage > 0;
        if (hitSplat.Drawable is HitSplatDrawable drawable)
        {
            drawable.Update(
                hit,
                hit ? $"-{e.Damage}" : "MISS");
            hitSplat.Invalidate();
        }

        hitSplat.VerticalOptions = playerAttacked
            ? LayoutOptions.Center
            : LayoutOptions.End;

        hitSplat.AbortAnimation("hitSplatMove");
        hitSplat.AbortAnimation("hitSplatFade");
        hitSplat.TranslationX = horizontalOffset;
        hitSplat.TranslationY = playerAttacked ? 0 : -8;
        hitSplat.Opacity = 1;

        new Animation(
            value => hitSplat.TranslationY = value,
            hitSplat.TranslationY,
            hitSplat.TranslationY - 4)
            .Commit(
                hitSplat,
                "hitSplatMove",
                16,
                450,
                Easing.CubicOut);

        new Animation(
            value => hitSplat.Opacity = value,
            1,
            0)
            .Commit(
                hitSplat,
                "hitSplatFade",
                16,
                450,
                Easing.CubicIn);
    }


    private void OnEnemyDefeated(Enemy enemy)
    {
        if (_disposed || !_isActive)
            return;

        IReadOnlyList<LootResult> loot = _combatManager.LastLoot;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_disposed || !_isActive)
                return;

            if (_combatManager.CurrentEnemy != null &&
                _combatManager.CurrentEnemy != enemy)
            {
                return;
            }

            _lastEnemy = enemy;

            CombatStatusLabel.Text =
                $"{enemy.Name} defeated!";

            UpdateEnemyKillCount(enemy);

            Task defeatAnimation = PlayEnemyDefeatAnimation();

            SetAttackProgress(0, 0);

            StopCombatButton.IsVisible = false;
            ActiveEnemyDropsHost.IsVisible = true;

            LootResults.IsVisible = true;
            BuildLootResults(enemy, loot);
            UpdateHPBars();

            if (_autoFightEnabled)
            {
                // Keep Run available while Auto Fight is waiting to respawn.
                // Pressing it cancels both the pending respawn and combat mode.
                StopCombatButton.IsVisible = true;
                StopCombatButton.IsEnabled = true;
                FightAgainButton.IsVisible = false;
                EnemySelectButton.IsVisible = false;
                FightAgainButton.IsEnabled = false;
                EnemySelectButton.IsEnabled = false;

                _ = BeginAutoFightRespawnAfterDefeatAsync(
                    enemy,
                    defeatAnimation);
                return;
            }

            FightAgainButton.IsVisible = true;
            EnemySelectButton.IsVisible = true;

            FightAgainButton.IsEnabled = true;
            EnemySelectButton.IsEnabled = true;
        });
    }

    private void OnAutoEatPerformed(AutoEatEventArgs e)
    {
        if (_disposed || !_isActive)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_disposed || !_isActive)
                return;

            _ = PlayAutoEatFeedbackAsync(e);
        });
    }

    private static async Task PlayHitReactionAsync(VisualElement target)
    {
        try
        {
            await Task.WhenAll(
                target.ScaleToAsync(1.035, 70, Easing.CubicOut),
                target.TranslateToAsync(5, 0, 35, Easing.CubicOut));
            await target.TranslateToAsync(-5, 0, 55, Easing.CubicInOut);
            await Task.WhenAll(
                target.ScaleToAsync(1, 90, Easing.CubicIn),
                target.TranslateToAsync(0, 0, 45, Easing.CubicOut));
        }
        catch
        {
            target.Scale = 1;
            target.TranslationX = 0;
        }
    }

    private static async Task PlayMissReactionAsync(VisualElement target)
    {
        try
        {
            await Task.WhenAll(
                target.FadeToAsync(0.72, 55, Easing.CubicOut),
                target.TranslateToAsync(2, 0, 35, Easing.CubicOut));
            await Task.WhenAll(
                target.FadeToAsync(1, 100, Easing.CubicIn),
                target.TranslateToAsync(0, 0, 55, Easing.CubicInOut));
        }
        catch
        {
            target.Opacity = 1;
            target.TranslationX = 0;
        }
    }

    private async Task PlayAutoEatFeedbackAsync(AutoEatEventArgs e)
    {
        try
        {
            if (PlayerHPBar.Width > 0)
            {
                double previous = Math.Clamp(
                    (double)e.PreviousHP / _player.GetMaxHP(),
                    0,
                    1);
                double current = Math.Clamp(
                    (double)e.CurrentHP / _player.GetMaxHP(),
                    0,
                    1);

                PlayerHealFill.Opacity = 0.9;
                PlayerHealFill.ScaleX = previous;
                var healingAnimation = new Animation(
                    value => PlayerHealFill.ScaleX = value,
                    previous,
                    current);
                healingAnimation.Commit(
                    PlayerHealFill,
                    "healTrail",
                    16,
                    180,
                    Easing.CubicOut);
                await Task.Delay(180);
                await PlayerHealFill.FadeToAsync(0, 220, Easing.CubicIn);
            }

            Color originalTextColor = PlayerHPLabel.TextColor;
            PlayerHPLabel.TextColor = Color.FromArgb("#8BFF9D");
            await Task.WhenAll(
                PlayerHPBar.ScaleToAsync(1.045, 120, Easing.CubicOut),
                PlayerHPLabel.ScaleToAsync(1.12, 120, Easing.CubicOut));
            await Task.WhenAll(
                PlayerHPBar.ScaleToAsync(1, 180, Easing.CubicIn),
                PlayerHPLabel.ScaleToAsync(1, 180, Easing.CubicIn));
            PlayerHPLabel.TextColor = originalTextColor;
        }
        catch
        {
            PlayerHPBar.Scale = 1;
            PlayerHPLabel.Scale = 1;
            PlayerHealFill.Opacity = 0;
        }
    }


    private async Task BeginAutoFightRespawnAfterDefeatAsync(
        Enemy enemy,
        Task defeatAnimation)
    {
        await defeatAnimation;

        if (!_autoFightEnabled)
            return;

        _combatManager.BeginAutoFightRespawn(
            enemy,
            DebugSettings.GetAutoFightRespawnTicks(
                AutoFightDelayTicks));

        UpdateEnemyPortraitAppearance();

        UpdateEnemyNameAppearance();

        await StartAutoFightNextEnemy(enemy);
    }

    private async Task PlayEnemyDefeatAnimation()
    {
        try
        {
            await Task.WhenAll(
                EnemyPanel.FadeToAsync(0.35, 80),
                EnemyPanel.TranslateToAsync(-8, 0, 45));

            await EnemyPanel.TranslateToAsync(8, 0, 45);

            await Task.WhenAll(
                EnemyPanel.FadeToAsync(1, 130),
                EnemyPanel.TranslateToAsync(0, 0, 65));
        }
        catch
        {
            EnemyPanel.Opacity = 1;
            EnemyPanel.TranslationX = 0;
        }
    }


    private void BuildLootResults(
        Enemy enemy,
        IReadOnlyList<LootResult>? lootSnapshot = null)
    {
        LootContainer.Children.Clear();

        IReadOnlyList<LootResult> loot =
            lootSnapshot ?? _combatManager.LastLoot;

        if (loot.Count == 0)
        {
            Label noLootLabel =
                new Label
                {
                    Text = "Nothing dropped.",
                    FontSize = 16,
                    HorizontalOptions = LayoutOptions.Center
                };

            LootContainer.Children.Add(noLootLabel);

            return;
        }

        foreach (LootResult result in loot)
        {
            Label lootLabel =
                new Label
                {
                    Text =
                        $"{result.Item.Name} × " +
                        $"{result.Quantity} — " +
                        FormatDropRarity(result.Rarity),
                    FontSize = 16,
                    HorizontalOptions = LayoutOptions.Center,
                    TranslationY = 3
                };
            if (RarityVisuals.IsRainbowRare(result.Chance))
                lootLabel.FormattedText = RarityVisuals.RainbowText(lootLabel.Text ?? string.Empty);
            LootContainer.Children.Add(
                new HorizontalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 6,
                    Children =
                    {
                        RarityVisuals.CreateItemVisual(
                            result.Item,
                            20,
                            rarityOverride: result.Rarity),
                        lootLabel
                    }
                });
        }

    }

    private static void AddRarityDecoration(
        Grid host,
        DropRarity rarity,
        double size)
    {
        Color color = GameThemeCache.GetRarityColor(rarity);
        Border rarityBox = new()
        {
            Stroke = color,
            StrokeThickness = 2,
            BackgroundColor = Colors.Transparent,
            InputTransparent = true,
            WidthRequest = size,
            HeightRequest = size,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        host.Children.Add(rarityBox);
    }

    private static string FormatDropRarity(DropRarity rarity)
    {
        return rarity switch
        {
            DropRarity.VeryRare => "Very Rare",
            DropRarity.SuperRare => "Super Rare",
            DropRarity.MegaRare => "Mega Rare",
            _ => rarity.ToString()
        };
    }

    private void OnFightAgainClicked(
    object? sender,
    EventArgs e)
    {
        if (_lastEnemy == null)
            return;

        DisableAutoFight();

        ResetDamageBars();

        LootResults.IsVisible = false;
        LootContainer.Children.Clear();

        StopCombatButton.IsVisible = true;

        FightAgainButton.IsVisible = false;
        FightAgainButton.IsEnabled = false;

        EnemySelectButton.IsVisible = false;
        EnemySelectButton.IsEnabled = false;

        _combatManager.StartCombat(_lastEnemy);
    }

    private void DisableAutoFight()
    {
        _autoFightEnabled = false;
        _combatManager.SetAutoFightEnabled(false);
        _autoFightGeneration++;

        _combatManager.ClearAutoFightRespawn();

        _autoFightCancellation?.Cancel();
        _autoFightCancellation?.Dispose();
        _autoFightCancellation = null;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            AutoFightButton.Text =
                "Auto";

            AutoFightButton.Variant = GoldSliceButtonVariant.Neutral;

            AutoFightButton.TextColor = Colors.White;
            UpdateAutoFightIndicator(false);

            AutoFightStatusLabel.Text = "";

            UpdateEnemyNameAppearance();

            if (!_combatManager.IsInCombat &&
                _lastEnemy != null &&
                !EnemySelectionView.IsVisible)
            {
                FightAgainButton.IsVisible = true;
                EnemySelectButton.IsVisible = true;

                FightAgainButton.IsEnabled = true;
                EnemySelectButton.IsEnabled = true;
            }
        });
    }

    public void StopCombatForSkillActivity()
    {
        DisableAutoFight();
        _combatManager.AbortCombatEncounter();

        // A skill start is equivalent to running away: do not leave a
        // selected enemy around for Fight Again or auto-fight to resume when
        // the player returns to the combat tab.
        _lastEnemy = null;
        _activeDropEnemy = null;
        ActiveEnemyDropsHost.IsVisible = false;
        CombatViewLayout.IsVisible = false;
        EnemySelectionView.IsVisible = true;
    }


    private void OnEnemySelectClicked(
        object? sender,
        EventArgs e)
    {
        DisableAutoFight();

        ResetDamageBars();

        _combatManager.StopCombat();

        CombatViewLayout.IsVisible = false;
        EnemySelectionView.IsVisible = true;

        StopCombatButton.IsVisible = false;
        FightAgainButton.IsVisible = false;
        EnemySelectButton.IsVisible = false;

        LootResults.IsVisible = false;
        LootContainer.Children.Clear();

        CombatStatusLabel.Text = "";
    }

    private void OnCombatStopped()
    {
        if (_disposed || !_isActive)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_disposed || !_isActive)
                return;

            ResetDamageBars();

            SetAttackProgress(0, 0);

            UpdateEnemyPortraitAppearance();
            UpdateHPBars();

            if (!_combatManager.IsAutoFightEnabled)
            {
                _autoFightEnabled = false;
                AutoFightButton.Text = "Auto";
                AutoFightButton.Variant = GoldSliceButtonVariant.Neutral;
                AutoFightButton.TextColor = Colors.White;
                UpdateAutoFightIndicator(false);
                AutoFightStatusLabel.Text = string.Empty;
            }
        });
    }

    private void OnAutoFightClicked(
    object? sender,
    EventArgs e)
    {
        if (_autoFightEnabled)
        {
            DisableAutoFight();
            return;
        }

        _autoFightEnabled = true;
        _combatManager.SetAutoFightEnabled(true);

        AutoFightButton.Text =
            "Auto";

        AutoFightButton.Variant = GoldSliceButtonVariant.Green;

        AutoFightButton.TextColor = Colors.White;
        UpdateAutoFightIndicator(true);

        AutoFightStatusLabel.Text =
            "Fighting...";

        // If we're already fighting, simply let the current
        // fight continue and auto-fight will take over when it ends.
        if (_combatManager.IsInCombat)
            return;

        // If we have an enemy selected, start fighting it immediately.
        if (_lastEnemy != null)
        {
            ResetDamageBars();

            LootResults.IsVisible = false;
            LootContainer.Children.Clear();

            StopCombatButton.IsVisible = true;
            FightAgainButton.IsVisible = false;
            EnemySelectButton.IsVisible = false;

            _combatManager.StartCombat(_lastEnemy);
        }
    }

    public void ResumeAutoFight(Enemy enemy)
    {
        if (!MeetsSkillRequirement(enemy) || _player.CurrentHP <= 0)
            return;

        _lastEnemy = enemy;
        _autoFightEnabled = true;
        _combatManager.SetAutoFightEnabled(true);

        EnemySelectionView.IsVisible = false;
        CombatViewLayout.IsVisible = true;
        StopCombatButton.IsVisible = true;
        FightAgainButton.IsVisible = false;
        EnemySelectButton.IsVisible = false;
        LootResults.IsVisible = false;
        LootContainer.Children.Clear();

        AutoFightButton.Text = "Auto";
        AutoFightButton.Variant = GoldSliceButtonVariant.Green;
        AutoFightButton.TextColor = Colors.White;
        UpdateAutoFightIndicator(true);
        AutoFightStatusLabel.Text = "Fighting...";

        ResetDamageBars();
        _combatManager.StartCombat(enemy, restorePlayerHealth: false);
    }

    private async Task StartAutoFightNextEnemy(
    Enemy enemy)
    {
        if (!_autoFightEnabled)
            return;

        int generation =
            _autoFightGeneration;

        _autoFightCancellation?.Cancel();
        _autoFightCancellation?.Dispose();

        _autoFightCancellation =
            new CancellationTokenSource();

        CancellationToken token =
            _autoFightCancellation.Token;

        try
        {
            // Count down one tick at a time.
            int respawnTicks =
                DebugSettings.GetAutoFightRespawnTicks(
                    AutoFightDelayTicks);

            for (int ticksLeft = respawnTicks;
                 ticksLeft > 0;
                 ticksLeft--)
            {
                if (!_autoFightEnabled ||
                    token.IsCancellationRequested ||
                    generation != _autoFightGeneration)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _combatManager.UpdateAutoFightRespawn(
                        ticksLeft);

                    AutoFightStatusLabel.Text =
                        $"Next enemy in {ticksLeft} " +
                        $"tick{(ticksLeft == 1 ? "" : "s")}...";
                });

                await Task.Delay(
                    GameClock.TickInterval,
                    token);
            }

            if (!_autoFightEnabled ||
                token.IsCancellationRequested ||
                generation != _autoFightGeneration)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!_autoFightEnabled ||
                    token.IsCancellationRequested ||
                    generation != _autoFightGeneration)
                {
                    return;
                }

                if (EnemySelectionView.IsVisible)
                    return;

                LootResults.IsVisible = false;
                LootContainer.Children.Clear();

                StopCombatButton.IsVisible = true;
                FightAgainButton.IsVisible = false;
                EnemySelectButton.IsVisible = false;

                AutoFightStatusLabel.Text =
                    "Fighting...";

                _lastEnemy = enemy;

                _combatManager.ClearAutoFightRespawn();

                UpdateEnemyNameAppearance();

                UpdateEnemyPortraitAppearance();

                ResetDamageBars();

                _combatManager.StartCombat(
                    enemy,
                    restorePlayerHealth: false);
            });
        }
        catch (OperationCanceledException)
        {
            // Expected when Auto Fight is turned off.
        }
    }

    private void OnStopCombatClicked(
        object? sender,
        EventArgs e)
    {
        DisableAutoFight();

        ResetDamageBars();

        _combatManager.StopCombat();

        CombatViewLayout.IsVisible = false;
        EnemySelectionView.IsVisible = true;

        StopCombatButton.IsVisible = false;

        FightAgainButton.IsVisible = false;
        FightAgainButton.IsEnabled = false;

        EnemySelectButton.IsVisible = false;
        EnemySelectButton.IsEnabled = false;

        LootResults.IsVisible = false;
        LootContainer.Children.Clear();

        CombatStatusLabel.Text = "";
    }

    // ================================================================
    // NATIVE SIX-POINT HIT SPLAT
    // ================================================================

    private sealed class HitSplatDrawable : IDrawable
    {
        private Color _fillColor = Color.FromArgb("#B82626");
        private Color _highlightColor = Color.FromArgb("#F07058");
        private string _text = string.Empty;

        public void Update(bool hit, string text)
        {
            _fillColor = hit
                ? Color.FromArgb("#B82626")
                : Color.FromArgb("#2D71A8");
            _highlightColor = hit
                ? Color.FromArgb("#F07058")
                : Color.FromArgb("#74C5EE");
            _text = text;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
                return;

            canvas.Antialias = true;

            PathF star = CreateSixPointStar(dirtyRect);

            canvas.FillColor = Colors.Black;
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 5;
            canvas.FillPath(star);
            canvas.DrawPath(star);

            canvas.FillColor = _fillColor;
            canvas.FillPath(star);

            DrawArmFacets(canvas, dirtyRect);

            canvas.StrokeColor = _highlightColor;
            canvas.StrokeSize = 2;
            canvas.DrawPath(star);

            canvas.FontSize = _text.Length > 3 ? 17 : 21;
            canvas.FontColor = Colors.Black;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x != 0 || y != 0)
                        DrawText(canvas, dirtyRect, x, y);
                }
            }

            canvas.FontColor = Colors.White;
            DrawText(canvas, dirtyRect, 0, 0);
        }

        private void DrawText(
            ICanvas canvas,
            RectF bounds,
            float offsetX,
            float offsetY)
        {
            canvas.DrawString(
                _text,
                bounds.X + offsetX,
                bounds.Y + offsetY,
                bounds.Width,
                bounds.Height,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }

        private void DrawArmFacets(ICanvas canvas, RectF bounds)
        {
            (float CenterX, float CenterY, float OuterRadius, float InnerRadius) =
                GetStarMetrics(bounds);

            canvas.FillColor = _fillColor.WithAlpha(0.55f);

            for (int point = 0; point < 6; point++)
            {
                float tipAngle = -MathF.PI / 2 + point * MathF.PI / 3;
                float innerAngle = tipAngle - MathF.PI / 6;

                PathF facet = new();
                facet.MoveTo(CenterX, CenterY);
                facet.LineTo(
                    CenterX + MathF.Cos(innerAngle) * InnerRadius,
                    CenterY + MathF.Sin(innerAngle) * InnerRadius);
                facet.LineTo(
                    CenterX + MathF.Cos(tipAngle) * OuterRadius,
                    CenterY + MathF.Sin(tipAngle) * OuterRadius);
                facet.Close();
                canvas.FillPath(facet);
            }
        }

        private static PathF CreateSixPointStar(RectF bounds)
        {
            (float centerX, float centerY, float outerRadius, float innerRadius) =
                GetStarMetrics(bounds);
            PathF path = new();

            for (int vertex = 0; vertex < 12; vertex++)
            {
                float angle = -MathF.PI / 2 + vertex * MathF.PI / 6;
                float radius = vertex % 2 == 0
                    ? outerRadius
                    : innerRadius;
                float x = centerX + MathF.Cos(angle) * radius;
                float y = centerY + MathF.Sin(angle) * radius;

                if (vertex == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }

            path.Close();
            return path;
        }

        private static (
            float CenterX,
            float CenterY,
            float OuterRadius,
            float InnerRadius) GetStarMetrics(RectF bounds)
        {
            float outerRadius = MathF.Max(
                0,
                MathF.Min(bounds.Width, bounds.Height) / 2 - 4);

            return (
                bounds.Center.X,
                bounds.Center.Y,
                outerRadius,
                outerRadius * 0.68f);
        }
    }
}
