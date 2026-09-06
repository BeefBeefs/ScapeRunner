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
    private bool _enemyListBuilt;

    private CancellationTokenSource? _playerDamageAnimationCancellation;
    private CancellationTokenSource? _enemyDamageAnimationCancellation;
    private bool _resumeDamageBarsOnRefresh;
    private double _playerAttackProgress;
    private double _enemyAttackProgress;

    private bool _autoFightEnabled;
    private CancellationTokenSource? _autoFightCancellation;
    private int _autoFightGeneration;

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
        public required Border FirstEnemyCard { get; init; }
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
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
        _combatManager.CombatStarted -= OnCombatStarted;
        _combatManager.CombatUpdated -= OnCombatUpdated;
        _combatManager.CombatStopped -= OnCombatStopped;
        _combatManager.AttackPerformed -= OnAttackPerformed;
        _combatManager.AutoEatPerformed -= OnAutoEatPerformed;
        _combatManager.EnemyDefeated -= OnEnemyDefeated;
        _combatManager.PlayerDefeated -= OnPlayerDefeated;
        _player.CollectionLog.DropDiscovered -= OnDropDiscovered;

        _playerDamageAnimationCancellation?.Cancel();
        _enemyDamageAnimationCancellation?.Cancel();

        _playerDamageAnimationCancellation?.Dispose();
        _enemyDamageAnimationCancellation?.Dispose();

        _autoFightCancellation?.Cancel();
        _autoFightCancellation?.Dispose();
    }

    public CombatView(
        Player player,
        CombatManager combatManager)
    {
        InitializeComponent();

        _player = player;
        _combatManager = combatManager;

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

        // Building every enemy card and drop row is relatively expensive.
        // Defer it one UI turn so returning from offline progress can render
        // the combat page immediately, especially on Android devices.
        Dispatcher.Dispatch(() =>
        {
            if (_enemyListBuilt)
                return;

            BuildEnemyList();
            _enemyListBuilt = true;
        });

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

        Border? firstEnemyCard = null;
        foreach (Enemy enemy in enemies)
        {
            Border enemyCard = BuildEnemyCard(
                enemy,
                enemyList);
            firstEnemyCard ??= enemyCard;
        }

        if (firstEnemyCard == null)
            return;

        _tierSections[tier] = new TierSectionState
        {
            Banner = tierBanner,
            ToggleLabel = toggleLabel,
            CompletionLabel = completionLabel,
            List = enemyList,
            FirstEnemyCard = firstEnemyCard
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

        // Let MAUI measure the newly visible list before calculating its
        // ScrollView position. The generation check prevents a rapid second
        // tap from completing an obsolete scroll operation.
        await Task.Delay(35);

        if (navigationGeneration != _areaNavigationGeneration ||
            !EnemySelectionView.IsVisible ||
            !selectedSection.List.IsVisible)
        {
            return;
        }

        await EnemySelectionView.ScrollToAsync(
            selectedSection.FirstEnemyCard,
            ScrollToPosition.Start,
            true);
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
        _playerDamageAnimationCancellation?.Cancel();
        _enemyDamageAnimationCancellation?.Cancel();

        _playerDamageAnimationCancellation?.Dispose();
        _enemyDamageAnimationCancellation?.Dispose();

        _playerDamageAnimationCancellation = null;
        _enemyDamageAnimationCancellation = null;

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
            HorizontalTextAlignment = TextAlignment.Center
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
                _ = RevealDropIconAsync(dropVisual.Icon);
            }
        }
    }

    private static async Task RevealDropIconAsync(Image icon)
    {
        try
        {
            icon.Opacity = 0;
            await icon.FadeToAsync(1, 260, Easing.CubicOut);
        }
        catch
        {
            icon.Opacity = 1;
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

        result.Spans.Add(new Span { Text = "Traits: ", TextColor = Colors.White });
        for (int index = 0; index < enemy.Traits.Count; index++)
        {
            if (index > 0)
                result.Spans.Add(new Span { Text = " • ", TextColor = Colors.White });
            EnemyTrait trait = enemy.Traits[index];
            result.Spans.Add(new Span
            {
                Text = EnemyTraitRules.Name(trait),
                TextColor = EnemyTraitRules.Color(trait)
            });
        }
        return result;
    }

    private void OnDropDiscovered(Item item)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
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
        _playerDamageAnimationCancellation?.Cancel();
        _enemyDamageAnimationCancellation?.Cancel();

        _playerDamageAnimationCancellation?.Dispose();
        _enemyDamageAnimationCancellation?.Dispose();

        _playerDamageAnimationCancellation = null;
        _enemyDamageAnimationCancellation = null;

        PlayerDamageFill.WidthRequest =
            PlayerHPBar.Width > 0
                ? PlayerHPBar.Width
                : 0;

        EnemyDamageFill.WidthRequest =
            EnemyHPBar.Width > 0
                ? EnemyHPBar.Width
                : 0;
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
        Enemy? startedEnemy = _combatManager.CurrentEnemy;
        if (startedEnemy == null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Combat can be stopped or switched before this queued UI update
            // runs. Never let an old event overwrite the current encounter.
            if (_combatManager.CurrentEnemy != startedEnemy ||
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
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
        PlayerNameLabel.Text =
            $"{_combatManager.Player.Name} lvl {_combatManager.Player.GetCombatLevel()}";

        bool isWaitingForAutoFightRespawn =
            _combatManager.IsAutoFightRespawning;

        EnemyHPBar.Opacity = isWaitingForAutoFightRespawn
            ? 0.45
            : 1;

        EnemyHPLabel.TextColor = isWaitingForAutoFightRespawn
            ? Colors.Gray
            : Colors.White;

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
            PlayerHPBar,
            PlayerHPFill,
            PlayerDamageFill,
            playerPercentage);


        // ------------------------------------------------------------
        // Update the player HP TEXT.
        //
        // IMPORTANT:
        // This uses CurrentHP, NOT the player's maximum HP.
        // ------------------------------------------------------------

        PlayerHPLabel.Text =
            $"{playerCurrentHP} / {playerMaxHP}";


        // ============================================================
        // ENEMY HP
        // ============================================================

        Enemy? enemy =
            _combatManager.CurrentEnemy ??
            _combatManager.LastDefeatedEnemy;


        if (enemy == null)
        {
            EnemyHPLabel.Text =
                "0 / 0";

            EnemyHPFill.WidthRequest =
                0;

            EnemyDamageFill.WidthRequest =
                0;

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
            EnemyHPBar,
            EnemyHPFill,
            EnemyDamageFill,
            enemyPercentage);


        // ------------------------------------------------------------
        // Update enemy HP TEXT.
        // ------------------------------------------------------------

        EnemyHPLabel.Text =
            $"{enemyCurrentHP} / {enemyMaxHP}";

        ResumeDamageBarsIfNeeded(includeEnemy: true);
    }

    private void ResumeDamageBarsIfNeeded(bool includeEnemy)
    {
        if (!_resumeDamageBarsOnRefresh || PlayerHPBar.Width <= 0)
            return;

        if (includeEnemy && EnemyHPBar.Width <= 0)
            return;

        _resumeDamageBarsOnRefresh = false;

        _ = AnimatePlayerDamage();

        if (includeEnemy)
            _ = AnimateEnemyDamage();
    }

    private void UpdateHPBar(
        Grid hpBar,
        BoxView hpFill,
        BoxView damageFill,
        double percentage)
    {
        percentage =
            Math.Clamp(
                percentage,
                0,
                1);

        hpFill.BackgroundColor = percentage <= 0.30
            ? Colors.Red
            : percentage <= 0.70
                ? Colors.Yellow
                : Colors.Green;

        hpFill.HorizontalOptions = LayoutOptions.Start;

        hpFill.WidthRequest =
            hpBar.Width * percentage;

        if (damageFill.WidthRequest <= 0)
        {
            damageFill.WidthRequest =
                hpBar.Width * percentage;

            damageFill.BackgroundColor =
                GetDamageColor(percentage);
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

    private async Task AnimatePlayerDamage()
    {
        _playerDamageAnimationCancellation?.Cancel();
        _playerDamageAnimationCancellation?.Dispose();

        _playerDamageAnimationCancellation =
            new CancellationTokenSource();

        CancellationToken token =
            _playerDamageAnimationCancellation.Token;

        int maxHP = _player.GetMaxHP();
        int currentHP = _player.CurrentHP;

        if (maxHP <= 0 || PlayerHPBar.Width <= 0)
            return;

        double newPercentage =
            Math.Clamp(
                (double)currentHP / maxHP,
                0,
                1);

        double oldPercentage =
            PlayerDamageFill.WidthRequest /
            PlayerHPBar.Width;

        oldPercentage =
            Math.Clamp(
                oldPercentage,
                0,
                1);

        if (newPercentage >= oldPercentage)
        {
            PlayerDamageFill.WidthRequest =
                PlayerHPBar.Width * newPercentage;

            return;
        }

        try
        {
            await AnimateRecentDamage(
                PlayerHPBar,
                PlayerHPFill,
                PlayerDamageFill,
                oldPercentage,
                newPercentage,
                token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task AnimateEnemyDamage()
    {
        _enemyDamageAnimationCancellation?.Cancel();
        _enemyDamageAnimationCancellation?.Dispose();

        _enemyDamageAnimationCancellation =
            new CancellationTokenSource();

        CancellationToken token =
            _enemyDamageAnimationCancellation.Token;

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

        double oldPercentage =
            EnemyDamageFill.WidthRequest /
            EnemyHPBar.Width;

        oldPercentage =
            Math.Clamp(
                oldPercentage,
                0,
                1);

        if (newPercentage >= oldPercentage)
        {
            EnemyDamageFill.WidthRequest =
                EnemyHPBar.Width * newPercentage;

            return;
        }

        try
        {
            await AnimateRecentDamage(
                EnemyHPBar,
                EnemyHPFill,
                EnemyDamageFill,
                oldPercentage,
                newPercentage,
                token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task AnimateRecentDamage(
        Grid hpBar,
        BoxView hpFill,
        BoxView damageFill,
        double oldPercentage,
        double newPercentage,
        CancellationToken cancellationToken)
    {
        oldPercentage =
            Math.Clamp(oldPercentage, 0, 1);

        newPercentage =
            Math.Clamp(newPercentage, 0, 1);

        if (hpBar.Width <= 0)
            return;

        damageFill.BackgroundColor =
            GetDamageColor(oldPercentage);

        hpFill.HorizontalOptions =
            LayoutOptions.Start;

        hpFill.WidthRequest =
            hpBar.Width * newPercentage;

        hpFill.BackgroundColor = newPercentage <= 0.30
            ? Colors.Red
            : newPercentage <= 0.70
                ? Colors.Yellow
                : Colors.Green;

        double startingWidth =
            hpBar.Width * oldPercentage;

        double targetWidth =
            hpBar.Width * newPercentage;

        damageFill.HorizontalOptions =
            LayoutOptions.Start;

        damageFill.WidthRequest =
            startingWidth;

        const int animationDuration = 500;
        const int frameTime = 16;

        int elapsed = 0;

        while (elapsed < animationDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(
                frameTime,
                cancellationToken);

            elapsed += frameTime;

            double progress =
                Math.Clamp(
                    (double)elapsed / animationDuration,
                    0,
                    1);

            double width =
                startingWidth +
                ((targetWidth - startingWidth) * progress);

            damageFill.WidthRequest = width;
        }

        cancellationToken.ThrowIfCancellationRequested();

        damageFill.WidthRequest = targetWidth;
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

        PlayerAttackLabel.Text =
            $"Player Attack — {playerAttackSpeed} ticks " +
            $"({playerAttackSpeed * 0.6:0.0}s)";
    }

    private void SetAttackProgress(
        double playerProgress,
        double enemyProgress)
    {
        _playerAttackProgress = Math.Clamp(playerProgress, 0, 1);
        _enemyAttackProgress = Math.Clamp(enemyProgress, 0, 1);

        UpdateAttackProgressBar(
            PlayerAttackBar,
            PlayerAttackFill,
            _playerAttackProgress);

        UpdateAttackProgressBar(
            EnemyAttackBar,
            EnemyAttackFill,
            _enemyAttackProgress);
    }

    private static void UpdateAttackProgressBar(
        Grid progressBar,
        BoxView progressFill,
        double progress)
    {
        progressFill.WidthRequest = progressBar.Width * progress;
    }

    private void OnAttackPerformed(CombatHitEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            ShowDamagePopup(e);

            // A hit should only jolt the portrait that was struck. Keeping the
            // surrounding card stationary makes the combat UI easier to read.
            VisualElement target = e.AttackerIsPlayer ? EnemyIcon : PlayerPanel;
            _ = e.Hit && e.Damage > 0
                ? PlayHitReactionAsync(target)
                : PlayMissReactionAsync(target);

            if (!e.Hit || e.Damage <= 0)
                return;

            try
            {
                if (e.AttackerIsPlayer)
                {
                    await AnimateEnemyDamage();
                }
                else
                {
                    await AnimatePlayerDamage();
                }
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    private async void ShowDamagePopup(CombatHitEventArgs e)
    {
        double horizontalOffset =
            Random.Shared.NextDouble() * 12 - 6;

        bool playerAttacked = e.AttackerIsPlayer;

        Layout targetLayer = playerAttacked
            ? EnemyHitSplatLayer
            : DamagePopupLayer;

        Grid hitSplat =
            new Grid
            {
                WidthRequest = 64,
                HeightRequest = 64,

                HorizontalOptions =
                    LayoutOptions.Center,

                VerticalOptions =
                    LayoutOptions.Center,

                TranslationX =
                    horizontalOffset
            };

        if (!playerAttacked)
        {
            // Damage from the enemy belongs at the bottom of the combat
            // area, directly above the persistent activity display.
            hitSplat.VerticalOptions = LayoutOptions.End;
            hitSplat.TranslationY = -8;
        }

        GraphicsView hitsplatGraphic =
            new GraphicsView
            {
                WidthRequest = 64,
                HeightRequest = 64,
                HorizontalOptions =
                    LayoutOptions.Center,
                VerticalOptions =
                    LayoutOptions.Center,
                Drawable = new HitSplatDrawable(
                    e.Hit,
                    e.Hit ? $"-{e.Damage}" : "MISS")
            };

        hitSplat.Children.Add(hitsplatGraphic);
        targetLayer.Children.Add(hitSplat);


        try
        {
            // --------------------------------------------------------
            // Hold position almost completely; a subtle lift keeps the
            // effect alive without obscuring which side produced it.
            // --------------------------------------------------------

            await Task.WhenAll(

                hitSplat.TranslateToAsync(
                    horizontalOffset,
                    hitSplat.TranslationY - 4,
                    2000,
                    Easing.Linear),

                hitSplat.FadeToAsync(
                    0,
                    2000));
        }
        catch
        {
            // Animation was interrupted.
        }


        targetLayer.Children.Remove(hitSplat);
    }


    private void OnEnemyDefeated(Enemy enemy)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
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
            BuildLootResults(enemy);
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
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
                PlayerHealFill.WidthRequest = PlayerHPBar.Width * previous;
                var healingAnimation = new Animation(
                    value => PlayerHealFill.WidthRequest = PlayerHPBar.Width * value,
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


    private void BuildLootResults(Enemy enemy)
    {
        LootContainer.Children.Clear();

        List<LootResult> loot =
            _combatManager.LastLoot;

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
        if (rarity == DropRarity.Common)
            return;

        Color color = GameThemeCache.GetRarityColor(rarity);
        Border glow = new()
        {
            Stroke = color,
            StrokeThickness = 1,
            BackgroundColor = color.WithAlpha(0.08f),
            Opacity = 0.55,
            InputTransparent = true,
            WidthRequest = size * 0.86,
            HeightRequest = size * 0.86,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        host.Children.Add(glow);

        Label sparkle = new()
        {
            Text = "✦",
            FontSize = Math.Max(7, size * 0.42),
            TextColor = color,
            Opacity = 0.25,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            TranslationX = 2,
            TranslationY = -2
        };
        host.Children.Add(sparkle);

        new Animation
        {
            { 0, 0.5, new Animation(value => glow.Opacity = value, 0.2, 0.75) },
            { 0.5, 1, new Animation(value => glow.Opacity = value, 0.75, 0.2) }
        }.Commit(glow, "dropGlow", 16, 1100, Easing.SinInOut, repeat: () => true);
        new Animation
        {
            { 0, 0.5, new Animation(value => sparkle.Opacity = value, 0.1, 1) },
            { 0.5, 1, new Animation(value => sparkle.Opacity = value, 1, 0.1) }
        }.Commit(sparkle, "dropSparkle", 16, 900, Easing.SinInOut, repeat: () => true);
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
    object sender,
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
        object sender,
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
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
    object sender,
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
    object sender,
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
    // OSRS-STYLE HIT SPLAT DRAWABLE
    // ================================================================

    private sealed class HitSplatDrawable : IDrawable
    {
        private readonly Color _color;
        private readonly Color _highlight;
        private readonly string _damageText;

        public HitSplatDrawable(bool hit, string damageText)
        {
            _color = hit
                ? Color.FromArgb("#B82626")
                : Color.FromArgb("#2D71A8");
            _highlight = hit
                ? Color.FromArgb("#F07058")
                : Color.FromArgb("#74C5EE");
            _damageText = damageText;
        }

        public void Draw(
            ICanvas canvas,
            RectF dirtyRect)
        {
            canvas.Antialias = false;

            PathF splat = CreateNinjaStarShape(dirtyRect);

            canvas.FillColor = Colors.Black;
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 4;
            canvas.DrawPath(splat);
            canvas.FillPath(splat);

            canvas.FillColor = _color;
            canvas.FillPath(splat);

            // Four darker triangular blades give the splat its clear
            // shuriken silhouette instead of a rounded blob.
            canvas.FillColor = _color.WithAlpha(0.62f);
            DrawBladeFacet(canvas, dirtyRect, (32, 32), (39, 17), (32, 1), (25, 17));
            DrawBladeFacet(canvas, dirtyRect, (32, 32), (47, 39), (63, 32), (47, 25));
            DrawBladeFacet(canvas, dirtyRect, (32, 32), (25, 47), (32, 63), (39, 47));
            DrawBladeFacet(canvas, dirtyRect, (32, 32), (17, 25), (1, 32), (17, 39));

            canvas.StrokeColor = _highlight;
            canvas.StrokeSize = 2;
            canvas.DrawPath(splat);

            canvas.FontSize = 21;
            canvas.FontColor = Colors.Black;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    DrawText(canvas, dirtyRect, x, y);
                }
            }

            canvas.FontColor = Colors.White;
            DrawText(canvas, dirtyRect, 0, 0);
        }

        private void DrawText(
            ICanvas canvas,
            RectF dirtyRect,
            float offsetX,
            float offsetY)
        {
            canvas.DrawString(
                _damageText,
                dirtyRect.X + offsetX,
                dirtyRect.Y + offsetY,
                dirtyRect.Width,
                dirtyRect.Height,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }

        private static void DrawBladeFacet(
            ICanvas canvas,
            RectF bounds,
            (float X, float Y) first,
            (float X, float Y) second,
            (float X, float Y) third,
            (float X, float Y) fourth)
        {
            PathF facet = new();
            (float X, float Y)[] points = { first, second, third, fourth };

            for (int index = 0; index < points.Length; index++)
            {
                float x = bounds.X + points[index].X * bounds.Width / 64f;
                float y = bounds.Y + points[index].Y * bounds.Height / 64f;

                if (index == 0)
                    facet.MoveTo(x, y);
                else
                    facet.LineTo(x, y);
            }

            facet.Close();
            canvas.FillPath(facet);
        }

        private static PathF CreateNinjaStarShape(RectF bounds)
        {
            (float X, float Y)[] points =
            {
                (25, 17), (32, 1), (39, 17), (49, 15),
                (47, 25), (63, 32), (47, 39), (49, 49),
                (39, 47), (32, 63), (25, 47), (15, 49),
                (17, 39), (1, 32), (17, 25), (15, 15)
            };

            PathF path = new();

            for (int index = 0; index < points.Length; index++)
            {
                float x = bounds.X + points[index].X * bounds.Width / 64f;
                float y = bounds.Y + points[index].Y * bounds.Height / 64f;

                if (index == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }

            path.Close();
            return path;
        }
    }
}
