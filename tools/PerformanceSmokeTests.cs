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
        inventory.Dispose();

        using ActivityManager activity = new(player);
        using CombatManager combat = new(player);
        using ActivityBar bar = new(activity, combat);
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

        CombatView combatView = new(player, combat);
        host.Content = combatView;
        combatView.PreloadEnemyList();
        combatView.SetActive(true);
        await Task.Delay(200);
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
        combatView.SetActive(false);
        combat.AbortCombatEncounter();
        combat.SetAutoFightEnabled(true);
        combat.BeginAutoFightRespawn(enemy, 12);
        Check(combat.IsAutoFightRespawning && combat.AutoFightTicksRemaining == 12, "Auto-fight respawn state");
        combat.ClearAutoFightRespawn();
        combatView.Dispose();

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
