using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Switch = Microsoft.Maui.Controls.Switch;

namespace OSRSIdle;

// Compiled only with -p:PerformanceSmokeTests=true. Runs inside MAUI so the
// same checks use real Windows/Android handlers rather than mocked controls.
internal static class PerformanceSmokeTests
{
    private static readonly List<string> Results = new();
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly string ReportPath = Path.Combine(
#if WINDOWS
        AppContext.BaseDirectory,
#else
        FileSystem.AppDataDirectory,
#endif
        "performance-smoke.txt");

    public static async Task<bool> RunAsync(Window window)
    {
        ContentPage host = new();
        window.Page = host;
        try
        {
            Check(StartupDataCache.IsInitialized, "Startup data validated");
            CheckRarities();
            CheckCollection();
            CheckShadows();
            await CheckViewsAsync(host);
            Results.Add("PASS: ALL CHECKS");
        }
        catch (Exception exception)
        {
            Results.Add("FAIL: " + exception);
        }
        finally
        {
            GameClock.SetSpeedUpEnabled(false);
            File.WriteAllLines(ReportPath, Results);
            host.Content = new ScrollView { Content = new Label { Text = string.Join("\n", Results), TextColor = Colors.White } };
        }
        return true;
    }

    private static void Check(bool condition, string description)
    {
        if (!condition)
            throw new InvalidOperationException(description);
        Results.Add("PASS: " + description);
        File.WriteAllLines(ReportPath, Results);
    }

    private static object? Call(object target, string method, params object?[] args) =>
        target.GetType().GetMethod(method, Private)!.Invoke(target, args);

    private static IDictionary States(object target, string field) =>
        (IDictionary)target.GetType().GetField(field, Private)!.GetValue(target)!;

    private static string AreaCompletionText(
        CombatView view,
        EnemyTier tier)
    {
        object section = States(view, "_tierSections")[tier]!;
        Label label = (Label)section.GetType()
            .GetProperty("CompletionLabel")!.GetValue(section)!;
        return label.Text ?? string.Empty;
    }

    private static void CheckRarities()
    {
        var allDrops = StartupDataCache.Enemies.SelectMany(enemy => enemy.DropTable.Drops).ToArray();
        foreach (Item item in StartupDataCache.Items)
        {
            DropRarity expectedBase = allDrops
                .Where(drop => RarityVisuals.GetBaseName(drop.Item.Name) == RarityVisuals.GetBaseName(item.Name))
                .Select(drop => drop.Rarity).DefaultIfEmpty(DropRarity.Common).Max();
            if (RarityVisuals.GetBaseRarity(item) != expectedBase ||
                !Equals(GameThemeCache.GetItemRarityColor(item), GameThemeCache.GetRarityColor(expectedBase)))
                throw new InvalidOperationException("Base rarity mismatch: " + item.Name);
            DropRarity expected = allDrops.Where(drop => drop.Item == item)
                .Select(drop => (DropRarity?)drop.Rarity).Max() ?? expectedBase;
            if (RarityVisuals.GetRarity(item) != expected)
                throw new InvalidOperationException("Exact rarity mismatch: " + item.Name);
        }
        Check(RarityVisuals.GetRarity(new Item { Name = "Unknown test item" }) == DropRarity.Common,
            $"Rarity parity for all {StartupDataCache.Items.Count} items, upgrades and unknown items");
    }

    private static void CheckCollection()
    {
        CollectionLog log = new();
        Enemy source = StartupDataCache.Enemies[0];
        Check(log.GetCompletedEnemyCount() == 0, "Empty collection bonus");
        foreach (Item item in StartupDataCache.Items)
        {
            log.RecordDrops(source, [new LootResult(item, 1, 1, DropRarity.Common)]);
            foreach (Enemy enemy in StartupDataCache.GetEnemiesDropping(item))
            {
                int expected = StartupDataCache.GetCollectionItems(enemy).Count(drop => log.HasReceivedDrop(enemy, drop));
                if (log.GetObtainedDropCount(enemy) != expected)
                    throw new InvalidOperationException("Shared discovery cache mismatch");
            }
            int completed = StartupDataCache.Enemies.Count(enemy =>
                StartupDataCache.GetCollectionItems(enemy).Count > 0 &&
                StartupDataCache.GetCollectionItems(enemy).All(drop => log.HasReceivedDrop(enemy, drop)));
            if (log.GetCompletedEnemyCount() != completed)
                throw new InvalidOperationException("Completion bonus mismatch");
        }
        CollectionLog restored = new();
        _ = restored.GetCompletedEnemyCount();
        restored.RestoreSaveData(JsonSerializer.Deserialize<CollectionLogSaveData>(
            JsonSerializer.Serialize(log.CreateSaveData()))!);
        Check(restored.GetCompletedEnemyCount() == log.GetCompletedEnemyCount(), "Collection restore invalidates cached counts");
        log.RecordKill(source);
        Check(log.GetKillCount(source) == 1, "Kills remain independent of discoveries");
        Benchmark("10000 cached completion lookups", () => { for (int i = 0; i < 10000; i++) _ = log.GetCompletedEnemyCount(); });
    }

    private static async Task CheckViewsAsync(ContentPage host)
    {
        Player player = new();
        player.Inventory.Clear();
        player.Inventory.RestoreSlotCapacity(1000);
        Item[] equipment = StartupDataCache.Items.Where(item => item.Type == ItemType.Equipment && item.UpgradeLevel == 0).Take(40).ToArray();
        foreach (Item item in equipment)
            player.Inventory.AddItem(item, 4);
        InventoryView inventory = new(player);
        host.Content = inventory;
        inventory.SetActive(true);
        await Task.Delay(250);
        await CheckPageBackgroundAsync(
            inventory,
            "background_inventory.png",
            "Inventory");
        Grid grid = inventory.FindByName<Grid>("InventoryGrid");
        Check(grid.Children.Count == equipment.Length && States(inventory, "_slots").Count == equipment.Length,
            "All 40 inventory stacks have slots");
        object[] originalSlots = grid.Children.Cast<object>().ToArray();
        player.Inventory.AddItem(equipment[0], 3);
        await UntilAsync(() => Labels(grid).Any(label => label.Text == "×7"));
        Check(originalSlots.SequenceEqual(grid.Children.Cast<object>()), "Quantity updates reuse every inventory slot");
        Check(Labels(grid).Any(label => label.Text == "×7"), "Retained slot quantity refreshed");
        Call(inventory, "OnSortQuantityClicked", null, EventArgs.Empty);
        Check(originalSlots.ToHashSet().SetEquals(grid.Children.Cast<object>()), "Sorting retains controls");
        Benchmark("100 retained inventory refreshes (40 stacks)", () => { for (int i = 0; i < 100; i++) inventory.RefreshDisplay(); });
        Benchmark("100 legacy slot rebuilds (40 stacks, construction only)", () =>
        {
            for (int i = 0; i < 100; i++)
                foreach (InventoryItem entry in player.Inventory.Items)
                    _ = Call(inventory, "CreateItemSlot", entry);
        });
        Check(player.Inventory.TryCombineEquipment(equipment[0], out Item upgraded), "Equipment combining");
        inventory.RefreshDisplay();
        Check(player.Inventory.GetQuantity(upgraded) == 1, "Combined equipment and rarity display");
        Call(inventory, "OnFoodTabClicked", null, EventArgs.Empty);
        Check(grid.Children.Count == 1 && Labels(grid).Any(label => label.Text == "No food yet."), "Empty category message");
        Item food = StartupDataCache.Items.First(item => item.Type == ItemType.Food && item.HealingAmount > 0);
        player.Inventory.AddItem(food, 10);
        await Task.Delay(100);
        Check(grid.Children.Count == 1 && grid.Children[0] is Border, "New item replaces empty state");
        Check(player.SelectFood(food), "Food selection");
        player.CurrentHP = 1;
        Check(player.TryConsumeEquippedFood() && player.CurrentHP > 1, "Food consumption and healing");
        player.Inventory.RemoveItem(food, player.Inventory.GetQuantity(food));
        await Task.Delay(100);
        Check(grid.Children.Count == 1 && Labels(grid).Any(label => label.Text == "No food yet."), "Removed item releases its slot");

        Item[] foodSortItems = StartupDataCache.Items
            .Where(item => item.Type == ItemType.Food && item.HealingAmount > 0)
            .OrderBy(item => item.HealingAmount)
            .ThenBy(item => item.Name)
            .Take(2)
            .ToArray();
        player.Inventory.AddItem(foodSortItems[1], 1);
        player.Inventory.AddItem(foodSortItems[0], 1);
        await Task.Delay(100);
        List<(InventoryItem Item, Border Card)> foodSlots = States(inventory, "_slots")
            .Cast<object>()
            .Select(entry =>
            {
                InventoryItem item = (InventoryItem)entry.GetType()
                    .GetProperty("Key")!.GetValue(entry)!;
                object slot = entry.GetType()
                    .GetProperty("Value")!.GetValue(entry)!;
                Border card = (Border)slot.GetType()
                    .GetProperty("Card")!.GetValue(slot)!;
                return (Item: item, Card: card);
            })
            .OrderBy(entry => Grid.GetRow(entry.Card))
            .ThenBy(entry => Grid.GetColumn(entry.Card))
            .ToList();
        Check(foodSlots.Select(entry => entry.Item.Item).SequenceEqual(foodSortItems),
            "Food tab defaults to lowest healing first");
        inventory.Dispose();

        using ActivityManager activity = new(player);
        using CombatManager combat = new(player);
        using ActivityBar bar = new(activity, combat);
        await CheckLiveCombatDefeatAsync(combat);
        await CheckCombatStyleLayoutAsync(host, bar, combat);
        await CheckAutoFightNavigationAsync(host);
        GameClock.SetSpeedUpEnabled(true);
        Check(
            GameClock.SpeedMultiplier == GameClock.SpeedUpMultiplier &&
            GameClock.TickInterval.TotalMilliseconds == GameClock.SpeedUpTickMilliseconds,
            "Stable speed-up clock");
        bool checkedSkillBackground = false;
        foreach (Skill skill in player.Skills)
        {
            SkillPage skillView = new(skill, activity);
            host.Content = skillView;
            skillView.SetActive(true);
            if (!checkedSkillBackground)
            {
                await CheckPageBackgroundAsync(
                    skillView,
                    "background_skill.png",
                    "Skill detail");
                checkedSkillBackground = true;
            }
            SkillActivity skillActivity = skill.Activities[0];
            activity.StartActivity(skill, skillActivity);
            double expectedDurationMilliseconds =
                ActivityMetrics.EffectiveActionTicks(skillActivity) *
                GameClock.TickInterval.TotalMilliseconds;
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(
                expectedDurationMilliseconds + 2000);
            while (skill.ActionsCompleted == 0 && DateTime.UtcNow < deadline)
                await Task.Delay(25);
            Check(skill.ActionsCompleted > 0 && skill.XP > 0, skill.Name + " action rewards and UI");
            activity.StopActivity();
            long actions = skill.ActionsCompleted;
            await Task.Delay(100);
            Check(skill.ActionsCompleted == actions, skill.Name + " cancellation");
            skillView.SetActive(false);
        }
        GameClock.SetSpeedUpEnabled(false);
        HomeView home = new(player, combat, activity);
        host.Content = home;
        home.SetActive(true);
        home.RefreshDisplay();
        await Task.Delay(100);
        await CheckPageBackgroundAsync(
            home,
            "background_home.png",
            "Home");
        home.Dispose();
        SkillsView skills = new(player, activity);
        host.Content = skills;
        skills.SetActive(true);
        skills.RefreshDisplay();
        await Task.Delay(100);
        await CheckPageBackgroundAsync(
            skills,
            "background_skills.png",
            "Skills");
        skills.Dispose();

        CollectionLogView collection = new(player.CollectionLog, _ => { });
        host.Content = collection;
        collection.SetActive(true);
        await CheckPageBackgroundAsync(
            collection,
            "background_collection_log.png",
            "Collection log");
        VerticalStackLayout enemyList = collection.FindByName<VerticalStackLayout>("EnemyList");
        object[] cards = enemyList.Children.Cast<object>().ToArray();
        Enemy enemy = StartupDataCache.Enemies[0];
        collection.FindByName<Switch>("HideCompletedSwitch").IsToggled = true;
        player.CollectionLog.RecordDrops(enemy, StartupDataCache.GetCollectionItems(enemy)
            .Select(item => new LootResult(item, 1, 1, DropRarity.Common)));
        await Task.Delay(100);
        Check(cards.SequenceEqual(enemyList.Children.Cast<object>()), "Collection discovery/filter retains cards");
        Check(!await collection.OpenEnemyAsync(enemy, false), "Completed enemy is hidden by filter");
        collection.FindByName<Switch>("HideCompletedSwitch").IsToggled = false;
        Check(await collection.OpenEnemyAsync(enemy, false), "Filter restores enemy details");
        collection.Dispose();
        await Task.Delay(100);

        CombatView combatView = new(player, combat);
        host.Content = combatView;
        combatView.PreloadEnemyList();
        Enemy overviewEnemy = StartupDataCache.Enemies.First(entry =>
            !player.CollectionLog.IsComplete(entry));
        Drop overviewDrop = overviewEnemy.DropTable.Drops.First(drop =>
            !player.CollectionLog.HasReceivedDrop(overviewEnemy, drop.Item));
        string completionBefore = AreaCompletionText(combatView, overviewEnemy.Tier);
        player.CollectionLog.RecordDrops(
            overviewEnemy,
            new[]
            {
                new LootResult(
                    overviewDrop.Item,
                    1,
                    overviewDrop.Chance,
                    overviewDrop.Rarity)
            });
        combatView.RefreshEnemyList();
        string completionAfter = AreaCompletionText(combatView, overviewEnemy.Tier);
        Check(completionAfter != completionBefore &&
              !completionAfter.EndsWith("0%", StringComparison.Ordinal),
            "Area overview refreshes collection completion percentage");
        combatView.SetActive(true);
        await Task.Delay(200);
        GraphicsView playerHitSplat = (GraphicsView)combatView.GetType()
            .GetField("_playerHitSplat", Private)!.GetValue(combatView)!;
        GraphicsView enemyHitSplat = (GraphicsView)combatView.GetType()
            .GetField("_enemyHitSplat", Private)!.GetValue(combatView)!;
        Check(
            playerHitSplat.Drawable?.GetType().Name == "HitSplatDrawable" &&
            enemyHitSplat.Drawable?.GetType().Name == "HitSplatDrawable",
            "Hit splats use the reusable native six-point drawable");
        await CheckPageBackgroundAsync(
            combatView,
            "background_combat.png",
            "Combat");
        Task abandoned = (Task)Call(combatView, "ExpandAreaAndScrollAsync", EnemyTier.Tier1)!;
        combatView.SetActive(false);
        await abandoned;
        Check(States(combatView, "_enemyCardStates").Count == 0, "Leaving combat cancels pending area construction");
        combatView.SetActive(true);
        foreach (EnemyTier tier in Enum.GetValues<EnemyTier>())
        {
            await (Task)Call(combatView, "ExpandAreaAndScrollAsync", tier)!;
            Check(StartupDataCache.GetEnemies(tier).All(entry => States(combatView, "_enemyCardStates").Contains(entry)), tier + " cards built incrementally");
        }
        int cardCount = States(combatView, "_enemyCardStates").Count;
        await (Task)Call(combatView, "ExpandAreaAndScrollAsync", EnemyTier.Tier1)!;
        Check(States(combatView, "_enemyCardStates").Count == cardCount, "Reopening area does not duplicate cards");
        player.CurrentHP = player.GetMaxHP();
        combatView.StartCombatNow(enemy);
        await Task.Delay(800);
        Check(combat.IsInCombat, "Live combat runs with native UI");

        // Collection navigation must remain safe while the combat loop is
        // still active. This also exercises the collection view's deferred
        // automatic scroll of the current enemy.
        Enemy combatLogEnemy = StartupDataCache.Enemies.First(entry =>
            !ReferenceEquals(entry, enemy));
        CollectionLogView collectionDuringCombat =
            new(player.CollectionLog, _ => { });
        combatView.SetActive(false);
        host.Content = collectionDuringCombat;
        collectionDuringCombat.SetActive(true);
        Check(await collectionDuringCombat.OpenEnemyAsync(combatLogEnemy, false),
            "Collection log opens during active combat");
        Check(combat.IsInCombat, "Collection navigation does not stop combat");
        collectionDuringCombat.Dispose();

        combatView.SetActive(false);
        combat.AbortCombatEncounter();
        combat.SetAutoFightEnabled(true);
        combat.BeginAutoFightRespawn(enemy, 12);
        Check(combat.IsAutoFightRespawning && combat.AutoFightTicksRemaining == 12, "Auto-fight respawn state");
        combat.ClearAutoFightRespawn();
        combatView.Dispose();
        await Task.Delay(100);

        SettingsView settings = new(() => { }, () => { }, () => { });
        host.Content = settings;
        await CheckPageBackgroundAsync(
            settings,
            "background_settings.png",
            "Settings");
        settings.Dispose();

        // Warm the simulator's JIT paths before measuring a representative
        // long catch-up. A maxed player versus the starter enemy survives the
        // full run without debug-only combat behavior changing the workload.
        OfflineCombatSimulation warmup = CreateOfflineCombatBenchmark(enemy, out _);
        warmup.Advance(2000);

        OfflineCombatSimulation simulation = CreateOfflineCombatBenchmark(
            enemy,
            out Player offlinePlayer);
        int inventoryNotifications = 0;
        offlinePlayer.Inventory.InventoryChanged += () => inventoryNotifications++;
        Benchmark("100000 ticks of offline combat", () => simulation.Advance(100_000));
        Check(simulation.TicksProcessed == 100_000 && simulation.Kills > 0 && !simulation.PlayerDied,
            "100,000-tick offline combat catch-up");
        Check(inventoryNotifications == 1 &&
              offlinePlayer.CollectionLog.GetKillCount(enemy) == simulation.Kills,
            "Offline combat batches notifications without losing rewards");

        // A one-kill run makes the attempt count deterministic. Verify that
        // offline combat records the awarded drop's own effective chance and
        // source text, rather than relying on a UI-only loot result.
        DebugSettings.SetInstakillEnabled(true);
        try
        {
            OfflineCombatSimulation luckSimulation =
                CreateOfflineCombatBenchmark(enemy, out Player luckPlayer);
            luckSimulation.Advance(enemy.AttackSpeedTicks / 2);
            LuckiestDrop luckiestDrop = luckPlayer.LuckiestDrop;
            Drop? recordedDrop = enemy.DropTable.Drops.FirstOrDefault(drop =>
                drop.Item.Name == luckiestDrop.ItemName);
            Check(luckSimulation.Kills == 1,
                "Offline luck test completes its first kill");
            Check(luckiestDrop.IsValid &&
                  recordedDrop != null &&
                  luckSimulation.Loot.ContainsKey(recordedDrop.Item),
                $"Offline luck test records the awarded item " +
                $"(actual '{luckiestDrop.ItemName}')");
            Check(luckiestDrop.Attempts == 1 &&
                  recordedDrop != null &&
                  luckiestDrop.Chance == Math.Clamp(
                      recordedDrop.Chance * (1d + luckPlayer.GlobalDropBoostPercent / 100d),
                      0d,
                      1d) &&
                  luckiestDrop.Source == $"{enemy.Name} kills",
                "Offline combat records the luckiest drop");
        }
        finally
        {
            DebugSettings.SetInstakillEnabled(false);
        }

        OfflineCombatSimulation cancelledSimulation =
            CreateOfflineCombatBenchmark(enemy, out Player cancellationPlayer);
        using (CancellationTokenSource cancellation = new())
        {
            // The first collection mutation occurs inside Advance, so this
            // cancels a run that has genuinely started rather than merely
            // passing an already-cancelled token.
            cancellationPlayer.CollectionLog.CollectionChanged +=
                cancellation.Cancel;
            cancelledSimulation.Advance(100_000, cancellation.Token);
        }
        Check(cancelledSimulation.TicksProcessed > 0 &&
              cancelledSimulation.TicksProcessed < 100_000 &&
              cancelledSimulation.Kills > 0 &&
              cancellationPlayer.CollectionLog.GetKillCount(enemy) ==
                  cancelledSimulation.Kills,
            "Offline combat cancellation stops an active run without losing rewards");

        Player levelingPlayer = new();
        levelingPlayer.Inventory.AddItem(ItemData.Watermelon, 1_000);
        Check(levelingPlayer.SelectFood(ItemData.Watermelon),
            "Offline combat progression test food equipped");
        levelingPlayer.CurrentHP = levelingPlayer.GetMaxHP();
        OfflineCombatSimulation levelingSimulation = new(
            levelingPlayer,
            new OfflineActivitySaveData
            {
                Kind = "Combat",
                ActivityName = enemy.Name,
                IsAutoFight = true,
                CombatStyle = CombatStyle.Attack.ToString()
            });
        levelingSimulation.Advance(20_000);
        Check(levelingSimulation.TicksProcessed == 20_000 &&
              levelingPlayer.Attack.Level > 1 &&
              !levelingSimulation.PlayerDied,
            "Offline combat refreshes cached stats after level-ups");
        PlayerSaveData save = SaveManager.CreateSaveData(player);
        Check(JsonSerializer.Deserialize<PlayerSaveData>(JsonSerializer.Serialize(save)) != null, "Save snapshot serialization (no user save touched)");
    }

    private static async Task CheckLiveCombatDefeatAsync(CombatManager combat)
    {
        Enemy enemy = StartupDataCache.Enemies[0];
        TaskCompletionSource<Enemy> defeated = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        IReadOnlyList<LootResult>? lootSnapshot = null;
        bool combatStoppedBeforeNotification = false;

        void OnEnemyDefeated(Enemy defeatedEnemy)
        {
            combatStoppedBeforeNotification = !combat.IsInCombat;
            lootSnapshot = combat.LastLoot;
            defeated.TrySetResult(defeatedEnemy);
        }

        combat.EnemyDefeated += OnEnemyDefeated;
        DebugSettings.SetInstakillEnabled(true);
        GameClock.SetSpeedUpEnabled(true);

        try
        {
            combat.StartCombat(enemy);
            Enemy result = await defeated.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Check(
                ReferenceEquals(result, enemy) &&
                combatStoppedBeforeNotification,
                "Live defeat stops combat before notifying subscribers");
            IReadOnlyList<LootResult> publishedLoot = lootSnapshot ??
                throw new InvalidOperationException("Defeat did not publish loot.");
            Check(
                publishedLoot is ICollection<LootResult> { IsReadOnly: true } &&
                publishedLoot.Count > 0,
                "Live defeat publishes a read-only loot snapshot");

            int awardedCount = publishedLoot.Count;
            combat.StartCombat(enemy);
            Check(
                combat.LastLoot.Count == 0 &&
                publishedLoot.Count == awardedCount,
                "Starting the next fight cannot mutate prior defeat loot");
        }
        finally
        {
            combat.EnemyDefeated -= OnEnemyDefeated;
            combat.AbortCombatEncounter();
            DebugSettings.SetInstakillEnabled(false);
            GameClock.SetSpeedUpEnabled(false);
        }
    }

    private static async Task CheckCombatStyleLayoutAsync(
        ContentPage host, ActivityBar bar, CombatManager combat)
    {
        host.Content = bar;
        Enemy enemy = StartupDataCache.Enemies[0];
        combat.StartCombat(enemy);
        for (int iteration = 0; iteration < 18; iteration++)
        {
            bar.WidthRequest = iteration % 2 == 0 ? 360 : 600;
            CombatStyle style = (CombatStyle)(iteration % 3);
            combat.SetCombatStyle(style);
            Call(bar, "UpdateDisplay");
            if (iteration % 3 == 0)
            {
                bar.IsVisible = false;
                await Task.Delay(20);
                bar.IsVisible = true;
            }
            await Task.Delay(40);
            foreach (string name in new[] { "AttackStyleButton", "StrengthStyleButton", "DefenseStyleButton" })
            {
                GoldSliceButton button = bar.FindByName<GoldSliceButton>(name);
                Label label = (Label)typeof(GoldSliceButton).GetField("_label", Private)!.GetValue(button)!;
                Grid content = (Grid)typeof(GoldSliceButton).GetField("_contentLayout", Private)!.GetValue(button)!;
                if (content.Width <= 0 || Math.Abs(content.Width - button.Width) > 1 ||
                    Math.Abs(content.Height - button.Height) > 1 ||
                    label.Width <= 0 || label.Height < button.Height - 1 ||
                    label.HorizontalTextAlignment != TextAlignment.Center ||
                    label.VerticalTextAlignment != TextAlignment.Center)
                    throw new InvalidOperationException($"{name} lost centered layout on iteration {iteration}: button {button.Bounds}, content {content.Bounds}, label {label.Bounds}");
            }
        }
        Check(true, "Combat style text stays centered through style, size and visibility changes");
        combat.AbortCombatEncounter();
        bar.WidthRequest = -1;
    }

    private static async Task CheckAutoFightNavigationAsync(ContentPage host)
    {
        Player player = new();
        using CombatManager combat = new(player);
        CombatView view = new(player, combat);
        Enemy enemy = StartupDataCache.Enemies[0];
        int kills = 0;
        combat.EnemyDefeated += _ => Interlocked.Increment(ref kills);
        DebugSettings.SetInstakillEnabled(true);
        GameClock.SetSpeedUpEnabled(true);
        try
        {
            host.Content = view;
            view.SetActive(true);
            view.StartCombatNow(enemy);
            Call(view, "OnAutoFightClicked", null, EventArgs.Empty);
            view.SetActive(false);
            host.Content = new Label { Text = "Away from combat" };
            await UntilAsync(() => Volatile.Read(ref kills) >= 3);
            Check(kills >= 3 && combat.IsAutoFightEnabled,
                "Auto-fight repeats multiple kills away from combat page");

            await UntilAsync(() => combat.IsAutoFightRespawning);
            // Hold the countdown long enough to inspect the restored page.
            combat.UpdateAutoFightRespawn(100);
            host.Content = view;
            view.SetActive(true);
            view.RefreshCombatDisplay();
            Check(view.FindByName<GoldSliceButton>("AutoFightButton").Variant == GoldSliceButtonVariant.Green &&
                view.FindByName<Label>("AutoFightStatusLabel").Text.StartsWith("Next enemy in") &&
                view.FindByName<GoldSliceButton>("StopCombatButton").IsVisible,
                "Returning during respawn restores Auto, countdown and Run controls");

            Call(view, "OnStopCombatClicked", null, EventArgs.Empty);
            int stoppedKills = Volatile.Read(ref kills);
            await Task.Delay(250);
            Check(!combat.IsInCombat && !combat.IsAutoFightRespawning &&
                !combat.IsAutoFightEnabled && kills == stoppedKills,
                "Run cancels the pending auto-fight restart");
            view.RefreshCombatDisplay();
            Check(view.FindByName<ScrollView>("EnemySelectionView").IsVisible,
                "Returning after Run keeps enemy selection open");

            combat.SetAutoFightEnabled(true);
            combat.BeginAutoFightRespawn(enemy, 1);
            combat.AbortCombatEncounter();
            await Task.Delay(150);
            Check(!combat.IsInCombat && !combat.IsAutoFightEnabled,
                "Starting another activity cancels auto-fight respawn");

            player.CurrentHP = player.GetMaxHP() - 1;
            int remainingHP = player.CurrentHP;
            combat.SetAutoFightEnabled(true);
            combat.BeginAutoFightRespawn(enemy, 1);
            await UntilAsync(() => combat.IsInCombat);
            Check(player.CurrentHP == remainingHP, "Auto-fight preserves HP between encounters");
            view.SetActive(false);
            Call(combat, "HandlePlayerDefeated");
            await Task.Delay(150);
            Check(!combat.IsAutoFightEnabled && !combat.IsAutoFightRespawning && !combat.IsInCombat,
                "Death disables auto-fight while the combat page is hidden");
        }
        finally
        {
            combat.AbortCombatEncounter();
            view.Dispose();
            DebugSettings.SetInstakillEnabled(false);
            GameClock.SetSpeedUpEnabled(false);
        }
    }

    private static OfflineCombatSimulation CreateOfflineCombatBenchmark(
        Enemy enemy,
        out Player player)
    {
        player = new Player();
        double level99XP = ExperienceTable.GetXPForLevel(99);
        player.HP.RestoreXP(level99XP);
        player.Attack.RestoreXP(level99XP);
        player.Strength.RestoreXP(level99XP);
        player.Defense.RestoreXP(level99XP);
        player.CurrentHP = player.GetMaxHP();

        return new OfflineCombatSimulation(player, new OfflineActivitySaveData
        {
            Kind = "Combat",
            ActivityName = enemy.Name,
            IsAutoFight = true,
            CombatStyle = CombatStyle.Attack.ToString()
        });
    }

    private static void Benchmark(string name, Action action)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        Stopwatch watch = Stopwatch.StartNew();
        action();
        Results.Add($"METRIC: {name}: {watch.Elapsed.TotalMilliseconds:F2}ms, {GC.GetAllocatedBytesForCurrentThread() - before:N0} allocated bytes");
    }

    private static void CheckShadows()
    {
        Label label = new() { FormattedText = RarityVisuals.RainbowText("Rare reward") };
        LabelShadow.SetIsEnabled(label, true);
        Grid host = new();
        host.Children.Add(label);
        Label shadow = Labels(host).First(candidate => !ReferenceEquals(candidate, label));
        FormattedString original = shadow.FormattedText;
        label.Opacity = 0.5;
        label.TranslationX = 12;
        label.ScaleX = 0.8;
        Check(ReferenceEquals(original, shadow.FormattedText) && shadow.Opacity == 0.5 &&
            shadow.TranslationX == 13 && shadow.ScaleX == 0.8,
            "Shadow animation reuses formatted spans and follows transforms");
        label.FormattedText = null;
        label.Text = "42";
        Check(shadow.Text == "42", "Shadow follows changed counters");
    }

    private static async Task UntilAsync(Func<bool> ready)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (!ready() && DateTime.UtcNow < deadline)
            await Task.Delay(50);
    }

    private static async Task CheckPageBackgroundAsync(
        ContentView view,
        string expectedSource,
        string pageName)
    {
        await UntilAsync(() => IsPageBackgroundReady(view, expectedSource));
        Check(
            IsPageBackgroundReady(view, expectedSource),
            $"{pageName} page renders {expectedSource}");
    }

    private static bool IsPageBackgroundReady(
        ContentView view,
        string expectedSource)
    {
        Image? image = view.FindByName<Image>("PageBackgroundImage");
        if (image?.Source is not FileImageSource fileSource ||
            !string.Equals(fileSource.File, expectedSource, StringComparison.OrdinalIgnoreCase) ||
            image.Opacity < 0.99 ||
            image.Width <= 0 ||
            image.Height <= 0 ||
            image.Width + 1 < view.Width ||
            image.Height + 1 < view.Height)
        {
            return false;
        }

#if WINDOWS
        return image.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Image
        {
            Source: Microsoft.UI.Xaml.Media.Imaging.BitmapSource
            {
                PixelWidth: > 0,
                PixelHeight: > 0
            }
        };
#else
        return image.Handler?.PlatformView != null;
#endif
    }

    private static IEnumerable<Label> Labels(IView view)
    {
        if (view is Label label)
            yield return label;
        if (view is Layout layout)
            foreach (IView child in layout.Children)
                foreach (Label nested in Labels(child))
                    yield return nested;
        if (view is Border { Content: not null } border)
            foreach (Label nested in Labels(border.Content))
                yield return nested;
    }
}
