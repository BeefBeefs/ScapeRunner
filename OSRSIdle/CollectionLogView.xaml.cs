namespace OSRSIdle;

public partial class CollectionLogView : ContentView
{
    private readonly CollectionLog _collectionLog;
    private readonly Action<Enemy> _fightNowRequested;

    private Enemy? _selectedEnemy;

    private bool _hideCompleted;

    private CancellationTokenSource? _portraitSparkleCancellation;

    private static readonly Color[] PortraitSparkleColors =
    {
        Color.FromArgb("#FFE26A"),
        Color.FromArgb("#62C7FF"),
        Color.FromArgb("#FF87C2"),
        Colors.White
    };

    private readonly Dictionary<Enemy, EnemyCardState> _enemyCards = new();

    private sealed class EnemyCardState
    {
        public required Label Label { get; init; }
        public required Border Card { get; init; }
    }

    public CollectionLogView(
        CollectionLog collectionLog,
        Action<Enemy> fightNowRequested)
    {
        InitializeComponent();

        _collectionLog =
            collectionLog;
        _fightNowRequested =
            fightNowRequested;

        _collectionLog.KillCountChanged += OnKillCountChanged;
        _collectionLog.DropDiscovered += OnDropDiscovered;
        _collectionLog.SkillingPetDiscovered += OnSkillingPetDiscovered;

        TapGestureRecognizer detailTapGesture =
            new TapGestureRecognizer();

        detailTapGesture.Tapped +=
            (sender, e) => HideEnemyDetails();

        EnemyDetailCard.GestureRecognizers.Add(
            detailTapGesture);

        UpdateDisplay();
    }

    private void OnFightNowClicked(
        object sender,
        EventArgs e)
    {
        if (_selectedEnemy != null)
            _fightNowRequested(_selectedEnemy);
    }

    public void Dispose()
    {
        StopPortraitSparkles();
        _collectionLog.KillCountChanged -= OnKillCountChanged;
        _collectionLog.DropDiscovered -= OnDropDiscovered;
        _collectionLog.SkillingPetDiscovered -= OnSkillingPetDiscovered;
    }

    private void OnKillCountChanged(Enemy enemy)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_hideCompleted && _collectionLog.IsComplete(enemy))
            {
                BuildEnemyList();
                return;
            }

            UpdateEnemyCard(enemy);
        });
    }

    private void OnDropDiscovered(Item item)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_hideCompleted)
            {
                BuildEnemyList();

                if (_selectedEnemy != null &&
                    _collectionLog.IsComplete(_selectedEnemy))
                {
                    HideEnemyDetails();
                }
            }

            foreach (Enemy enemy in StartupDataCache.GetEnemiesDropping(item))
                UpdateEnemyCard(enemy);

            if (_selectedEnemy?.DropTable.Drops.Any(drop => drop.Item == item) == true)
                ShowEnemyDetails(_selectedEnemy);
        });
    }

    private void OnSkillingPetDiscovered(Item item)
    {
        _ = item;
        MainThread.BeginInvokeOnMainThread(BuildSkillingPetList);
    }

    private void OnHideCompletedToggled(
        object sender,
        ToggledEventArgs e)
    {
        _hideCompleted = e.Value;

        if (_hideCompleted &&
            _selectedEnemy != null &&
            _collectionLog.IsComplete(_selectedEnemy))
        {
            HideEnemyDetails();
        }

        BuildEnemyList();
    }

    public void RefreshDisplay()
    {
        foreach (Enemy enemy in _enemyCards.Keys)
            UpdateEnemyCard(enemy);

        if (_selectedEnemy != null)
            ShowEnemyDetails(_selectedEnemy);
    }

    private void UpdateDisplay()
    {
        BuildSkillingPetList();

        BuildEnemyList();

        if (_selectedEnemy != null)
        {
            ShowEnemyDetails(_selectedEnemy);
        }
    }

    private void BuildSkillingPetList()
    {
        SkillingPetList.Children.Clear();

        foreach (SkillingPet pet in SkillingPetData.AllPets)
        {
            bool obtained =
                _collectionLog.HasReceivedSkillingPet(
                    pet.Item);

            Label petLabel =
                new Label
                {
                    Text = obtained
                        ? $"{pet.Item.Name} — {pet.SkillName}"
                        : $"{pet.SkillIcon} ??? — {pet.SkillName}",
                    FontSize = 16,
                    TextColor = obtained
                        ? Colors.White
                        : Colors.Red,
                    VerticalOptions = LayoutOptions.Center,
                    TranslationY = 3
                };

            if (!obtained)
            {
                SkillingPetList.Children.Add(petLabel);
                continue;
            }

            SkillingPetList.Children.Add(
                new HorizontalStackLayout
                {
                    Spacing = 5,
                    Children =
                    {
                        new Image
                        {
                            Source = pet.Item.IconImage,
                            WidthRequest = 20,
                            HeightRequest = 20,
                            Aspect = Aspect.AspectFit
                        },
                        petLabel
                    }
                });
        }
    }

    private void BuildEnemyList()
    {
        EnemyList.Children.Clear();
        _enemyCards.Clear();

        foreach (Enemy enemy in StartupDataCache.OrderedEnemies)
        {
            bool complete =
                _collectionLog.IsComplete(enemy);

            if (_hideCompleted && complete)
                continue;

            int obtainedDrops =
                _collectionLog.GetObtainedDropCount(enemy);

            int totalDrops =
                _collectionLog.GetDropCount(enemy);

            int killCount =
                _collectionLog.GetKillCount(enemy);

            bool started =
                killCount > 0 ||
                obtainedDrops > 0;

            Label enemyLabel =
                new Label
                {
                    Text =
                        $"{enemy.Name} ({obtainedDrops}/{totalDrops}) — {killCount:N0} kills" +
                        (enemy.Traits.Count == 0
                            ? string.Empty
                            : $"\n{string.Join(" • ", enemy.Traits.Select(EnemyTraitRules.Name))}"),
                    FontSize = 17,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = complete
                        ? Colors.Green
                        : started
                            ? Color.FromArgb("#D99032")
                            : Colors.Red,
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

            HorizontalStackLayout enemyRow =
                new HorizontalStackLayout
                {
                    Spacing = 7,
                    Children = { enemyIcon, enemyLabel }
                };

            Border enemyCard =
                new Border
                {
                    Padding = 10,
                    BackgroundColor = complete
                        ? Color.FromArgb("#33333333")
                        : Color.FromArgb("#E64A4A4A"),
                    Stroke = Color.FromArgb("#D99032"),
                    StrokeThickness = 2,
                    Opacity = 1,
                    VerticalOptions = LayoutOptions.Start,
                    Content = enemyRow
                };

            TapGestureRecognizer tapGesture =
                new TapGestureRecognizer();

            tapGesture.Tapped +=
                async (sender, e) =>
                {
                    if (_selectedEnemy == enemy)
                    {
                        HideEnemyDetails();
                        return;
                    }

                    await OpenEnemyAsync(enemy);
                };

            enemyCard.GestureRecognizers.Add(tapGesture);

            _enemyCards[enemy] = new EnemyCardState
            {
                Label = enemyLabel,
                Card = enemyCard
            };

            EnemyList.Children.Add(enemyCard);
        }
    }

    public async Task<bool> OpenEnemyAsync(
        Enemy enemy,
        bool animated = true)
    {
        if (!_enemyCards.ContainsKey(enemy))
            return false;

        _selectedEnemy = enemy;
        ShowEnemyDetails(enemy);

        try
        {
            await CollectionScrollView.ScrollToAsync(
                EnemyDetailCard,
                ScrollToPosition.MakeVisible,
                animated);
        }
        catch
        {
            // The details are still open if the page is removed while scrolling.
        }

        return true;
    }

    private void UpdateEnemyCard(Enemy enemy)
    {
        if (!_enemyCards.TryGetValue(enemy, out EnemyCardState? state))
            return;

        bool complete = _collectionLog.IsComplete(enemy);
        int obtainedDrops = _collectionLog.GetObtainedDropCount(enemy);
        int totalDrops = _collectionLog.GetDropCount(enemy);
        int killCount = _collectionLog.GetKillCount(enemy);
        bool started = killCount > 0 || obtainedDrops > 0;

        state.Label.Text =
            $"{enemy.Name} ({obtainedDrops}/{totalDrops}) — {killCount:N0} kills";
        state.Label.TextColor = complete
            ? Colors.Green
            : started
                ? Color.FromArgb("#D99032")
                : Colors.Red;
        state.Card.BackgroundColor = complete
            ? Color.FromArgb("#33333333")
            : Color.FromArgb("#E64A4A4A");
        state.Card.Opacity = 1;

        if (_selectedEnemy == enemy)
        {
            EnemyDetailCompletionLabel.IsVisible = complete;
            if (complete && _portraitSparkleCancellation == null)
                StartPortraitSparkles();

            EnemyStatsLabel.Text =
                $"HP: {enemy.HP}   Attack: {enemy.Attack}\n" +
                $"Strength: {enemy.Strength}   Defense: {enemy.Defense}\n" +
                $"Attack speed: {enemy.AttackSpeedTicks} ticks\n" +
                $"Total kills: {killCount:N0}";
        }
    }


    private void HideEnemyDetails()
    {
        StopPortraitSparkles();
        _selectedEnemy = null;

        EnemyDetailCard.IsVisible = false;

        DropList.Children.Clear();
    }

    private void ShowEnemyDetails(
        Enemy enemy)
    {
        StopPortraitSparkles();

        EnemyDetailCard.IsVisible =
            true;

        EnemyDetailNameLabel.Text =
            enemy.Name;

        EnemyDetailCompletionLabel.IsVisible =
            _collectionLog.IsComplete(enemy);

        EnemyDetailCombatLevelLabel.Text =
            $"lvl {enemy.CombatLevel}";

        EnemyDetailTraitsLabel.FormattedText = BuildEnemyTraitsText(enemy);
        EnemyDetailTraitsLabel.IsVisible = enemy.Traits.Count > 0;

        EnemyDetailDescriptionLabel.Text =
            enemy.Description;

        EnemyDetailIcon.Source = enemy.LargeIconImage;
        if (_collectionLog.IsComplete(enemy))
            StartPortraitSparkles();

        EnemyStatsLabel.Text =
            $"HP: {enemy.HP}   Attack: {enemy.Attack}\n" +
            $"Strength: {enemy.Strength}   Defense: {enemy.Defense}\n" +
            $"Attack speed: {enemy.AttackSpeedTicks} ticks\n" +
            $"Total kills: {_collectionLog.GetKillCount(enemy):N0}";

        DropList.Children.Clear();

        foreach (Drop drop in DropDisplayOrder.ForEnemy(enemy))
        {
            bool obtained =
                _collectionLog.HasReceivedDrop(
                    enemy,
                    drop.Item);

            Grid dropRow =
                new Grid
                {
                    ColumnDefinitions =
                        new ColumnDefinitionCollection
                        {
                            new ColumnDefinition(GridLength.Auto),
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Auto)
                        }
                };

            View itemVisual = obtained
                ? RarityVisuals.CreateItemVisual(drop.Item, 18)
                : new Image
                {
                    Source = drop.Item.IconImage,
                    WidthRequest = 18,
                    HeightRequest = 18,
                    Opacity = 0.28,
                    Aspect = Aspect.AspectFit
                };
            dropRow.Add(itemVisual, 0);

            Label itemLabel = new()
            {
                Text = obtained ? drop.Item.Name : $"???  ({drop.Chance:P2})",
                FontSize = 15,
                TextColor = obtained ? GetRarityColor(drop.Rarity) : Colors.Red,
                VerticalOptions = LayoutOptions.Center,
                TranslationX = 3,
                TranslationY = 1
            };
            if (obtained && RarityVisuals.IsRainbowRare(drop.Chance))
                itemLabel.FormattedText = RarityVisuals.RainbowText(itemLabel.Text ?? string.Empty);
            dropRow.Add(itemLabel, 1);

            dropRow.Add(
                new Label
                {
                    Text = FormatRarity(
                        drop.Rarity),
                    FontSize = 13,
                    TextColor = obtained
                        ? GetRarityColor(drop.Rarity)
                        : Colors.Red,
                    VerticalOptions = LayoutOptions.Center,
                    TranslationX = -7,
                    TranslationY = 1
                },
                2);

            DropList.Children.Add(dropRow);
        }

        DropList.Opacity = 0;
        _ = DropList.FadeToAsync(1, 220, Easing.CubicOut);
    }

    private void StartPortraitSparkles()
    {
        _portraitSparkleCancellation = new CancellationTokenSource();
        _ = RunPortraitSparklesAsync(_portraitSparkleCancellation.Token);
    }

    private async Task RunPortraitSparklesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                List<Task> sparkleAnimations = new();
                for (int index = 0; index < 3; index++)
                {
                    Label sparkle = new()
                    {
                        Text = "✦",
                        FontSize = Random.Shared.Next(11, 20),
                        TextColor = PortraitSparkleColors[
                            Random.Shared.Next(PortraitSparkleColors.Length)],
                        Opacity = 0,
                        WidthRequest = 24,
                        HeightRequest = 24,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        InputTransparent = true
                    };

                    double x = Random.Shared.Next(4, 172);
                    double y = Random.Shared.Next(4, 172);
                    AbsoluteLayout.SetLayoutBounds(
                        sparkle,
                        new Rect(x, y, 24, 24));
                    EnemyDetailSparkleLayer.Children.Add(sparkle);
                    sparkleAnimations.Add(AnimatePortraitSparkleAsync(
                        sparkle,
                        cancellationToken));
                }

                await Task.WhenAll(sparkleAnimations);

                await Task.Delay(
                    Random.Shared.Next(220, 650),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Closing the expanded card intentionally stops the sparkle loop.
        }
        catch
        {
            // Visual effects should never interrupt collection-log browsing.
        }
    }

    private async Task AnimatePortraitSparkleAsync(
        Label sparkle,
        CancellationToken cancellationToken)
    {
        await sparkle.FadeToAsync(0.95, 130, Easing.CubicOut);
        await Task.Delay(Random.Shared.Next(120, 420), cancellationToken);
        await sparkle.FadeToAsync(0, 260, Easing.CubicIn);
        EnemyDetailSparkleLayer.Children.Remove(sparkle);
    }

    private void StopPortraitSparkles()
    {
        _portraitSparkleCancellation?.Cancel();
        _portraitSparkleCancellation?.Dispose();
        _portraitSparkleCancellation = null;
        EnemyDetailSparkleLayer.Children.Clear();
    }

    private static string FormatRarity(
        DropRarity rarity)
    {
        return rarity switch
        {
            DropRarity.VeryRare => "Very Rare",
            DropRarity.SuperRare => "Super Rare",
            DropRarity.MegaRare => "Mega Rare",
            _ => rarity.ToString()
        };
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

    private static Color GetRarityColor(DropRarity rarity)
    {
        return GameThemeCache.GetRarityColor(rarity);
    }
}
