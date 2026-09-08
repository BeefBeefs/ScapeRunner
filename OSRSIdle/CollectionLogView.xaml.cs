namespace OSRSIdle;

public partial class CollectionLogView : ContentView
{
    private readonly CollectionLog _collectionLog;
    private readonly Action<Enemy> _fightNowRequested;

    private Enemy? _selectedEnemy;

    private bool _hideCompleted;
    private bool _isActive;
    private int _activationGeneration;

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

        TapGestureRecognizer detailTapGesture =
            new TapGestureRecognizer();

        detailTapGesture.Tapped +=
            (sender, e) => HideEnemyDetails();

        EnemyDetailCard.GestureRecognizers.Add(
            detailTapGesture);

        UpdateDisplay();
    }

    public void SetActive(bool active)
    {
        if (_isActive == active)
            return;

        _isActive = active;
        _activationGeneration++;

        if (active)
        {
            _collectionLog.KillCountChanged += OnKillCountChanged;
            _collectionLog.DropDiscovered += OnDropDiscovered;
            _collectionLog.SkillingPetDiscovered += OnSkillingPetDiscovered;
        }
        else
        {
            _collectionLog.KillCountChanged -= OnKillCountChanged;
            _collectionLog.DropDiscovered -= OnDropDiscovered;
            _collectionLog.SkillingPetDiscovered -= OnSkillingPetDiscovered;
        }
    }

    private void OnFightNowClicked(
        object? sender,
        EventArgs e)
    {
        if (_selectedEnemy != null)
            _fightNowRequested(_selectedEnemy);
    }

    public void Dispose()
    {
        SetActive(false);
    }

    private void OnKillCountChanged(Enemy enemy)
    {
        if (!_isActive)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_isActive)
                return;

            UpdateEnemyCard(enemy);
        });
    }

    private void OnDropDiscovered(Item item)
    {
        if (!_isActive)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_isActive)
                return;

            if (_hideCompleted)
            {
                if (_selectedEnemy != null &&
                    _collectionLog.IsComplete(_selectedEnemy))
                {
                    HideEnemyDetails();
                }
            }

            foreach (Enemy enemy in StartupDataCache.GetEnemiesDropping(item))
                UpdateEnemyCard(enemy);

            VisualEffects.PlayParticles(
                CollectionEffectLayer,
                VisualEffectKind.Burst,
                durationMilliseconds: 850,
                particleCount: 16);

            if (_selectedEnemy?.DropTable.Drops.Any(drop => drop.Item == item) == true)
                ShowEnemyDetails(_selectedEnemy);
        });
    }

    private void OnSkillingPetDiscovered(Item item)
    {
        if (!_isActive)
            return;

        _ = item;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_isActive)
                BuildSkillingPetList();
        });
    }

    private void OnHideCompletedToggled(
        object? sender,
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
        BuildSkillingPetList();
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
                        : $"??? — {pet.SkillName}",
                    FontSize = 16,
                    TextColor = obtained
                        ? Colors.White
                        : Colors.Red,
                    VerticalOptions = LayoutOptions.Center,
                    TranslationY = 3
                };

            SkillingPetList.Children.Add(
                new HorizontalStackLayout
                {
                    Spacing = 5,
                    Children =
                    {
                        new Image
                        {
                            Source = obtained
                                ? pet.Item.IconImage
                                : Skill.GetIconImage(pet.SkillName),
                            WidthRequest = 20,
                            HeightRequest = 20,
                            Aspect = Aspect.AspectFit,
                            Opacity = obtained ? 1 : 0.65
                        },
                        petLabel
                    }
                });
        }
    }

    private void BuildEnemyList()
    {
        if (_enemyCards.Count > 0)
        {
            foreach (Enemy enemy in _enemyCards.Keys)
                UpdateEnemyCard(enemy);
            return;
        }

        foreach (Enemy enemy in StartupDataCache.OrderedEnemies)
        {
            bool complete =
                _collectionLog.IsComplete(enemy);

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
                    IsVisible = !_hideCompleted || !complete,
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
        if (!_enemyCards.TryGetValue(enemy, out EnemyCardState? state) || !state.Card.IsVisible)
            return false;

        int activationGeneration = _activationGeneration;
        _selectedEnemy = enemy;
        ShowEnemyDetails(enemy);

        // A collection log can be opened while combat is still running. The
        // view is attached to the page before its native ScrollView has
        // necessarily completed its first layout, so scrolling immediately can
        // race the Windows handler. The details are already visible; delay
        // only the optional automatic scroll until the view is settled.
        await Task.Delay(35);

        if (!_isActive ||
            activationGeneration != _activationGeneration ||
            !ReferenceEquals(_selectedEnemy, enemy) ||
            CollectionScrollView.Width <= 0 ||
            CollectionScrollView.Height <= 0 ||
            EnemyDetailCard.Height <= 0)
        {
            return true;
        }

        try
        {
            await CollectionScrollView.ScrollToAsync(
                EnemyDetailCard,
                ScrollToPosition.MakeVisible,
                animated).WaitAsync(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Details remain open if the page is removed, or the native
            // scroll handler never reports completion at its destination.
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
            $"{enemy.Name} ({obtainedDrops}/{totalDrops}) — {killCount:N0} kills" +
            (enemy.Traits.Count == 0 ? string.Empty :
                $"\n{string.Join(" • ", enemy.Traits.Select(EnemyTraitRules.Name))}");
        state.Label.TextColor = complete
            ? Colors.Green
            : started
                ? Color.FromArgb("#D99032")
                : Colors.Red;
        state.Card.BackgroundColor = complete
            ? Color.FromArgb("#33333333")
            : Color.FromArgb("#E64A4A4A");
        state.Card.Opacity = 1;
        state.Card.IsVisible = !_hideCompleted || !complete;

        if (_selectedEnemy == enemy && !state.Card.IsVisible)
            HideEnemyDetails();

        if (_selectedEnemy == enemy)
        {
            EnemyDetailCompletionLabel.IsVisible = complete;
            EnemyDetailPortraitFrame.Stroke = complete
                ? Color.FromArgb("#42A85A")
                : Color.FromArgb("#D99032");

            EnemyStatsLabel.Text =
                $"HP: {enemy.HP}   Attack: {enemy.Attack}\n" +
                $"Strength: {enemy.Strength}   Defense: {enemy.Defense}\n" +
                $"Attack speed: {enemy.AttackSpeedTicks} ticks\n" +
                $"Total kills: {killCount:N0}";
        }
    }


    private void HideEnemyDetails()
    {
        _selectedEnemy = null;

        EnemyDetailCard.IsVisible = false;

        DropList.Children.Clear();
    }

    private void ShowEnemyDetails(
        Enemy enemy)
    {
        EnemyDetailCard.IsVisible =
            true;

        EnemyDetailNameLabel.Text =
            enemy.Name;

        bool complete = _collectionLog.IsComplete(enemy);
        EnemyDetailCompletionLabel.IsVisible = complete;
        EnemyDetailPortraitFrame.Stroke = complete
            ? Color.FromArgb("#42A85A")
            : Color.FromArgb("#D99032");

        EnemyDetailCombatLevelLabel.Text =
            $"lvl {enemy.CombatLevel}";

        EnemyDetailTraitsLabel.FormattedText = BuildEnemyTraitsText(enemy);
        EnemyDetailTraitsLabel.IsVisible = enemy.Traits.Count > 0;

        EnemyDetailDescriptionLabel.Text =
            enemy.Description;

        EnemyDetailIcon.Source = enemy.LargeIconImage;

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
                : RarityVisuals.CreateUndiscoveredItemVisual(18);
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

        DropList.Opacity = 1;
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

    private static Color GetRarityColor(DropRarity rarity)
    {
        return GameThemeCache.GetRarityColor(rarity);
    }
}
