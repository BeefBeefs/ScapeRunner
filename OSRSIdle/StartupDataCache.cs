namespace OSRSIdle;

public readonly record struct StartupProgress(
    double Value,
    string Stage);

/// <summary>
/// Holds immutable, derived views of the game's authoritative static data.
/// Player-owned state is deliberately excluded.
/// </summary>
public static class StartupDataCache
{
    private static Task? _initializationTask;

    public static IReadOnlyList<Item> Items { get; private set; } = Array.Empty<Item>();
    public static IReadOnlyList<Enemy> Enemies { get; private set; } = Array.Empty<Enemy>();
    public static IReadOnlyList<Enemy> OrderedEnemies { get; private set; } = Array.Empty<Enemy>();
    public static IReadOnlyDictionary<string, Item> ItemsByName { get; private set; } =
        new Dictionary<string, Item>();
    public static IReadOnlyDictionary<string, Enemy> EnemiesByName { get; private set; } =
        new Dictionary<string, Enemy>();
    public static IReadOnlyDictionary<EnemyTier, IReadOnlyList<Enemy>> EnemiesByTier { get; private set; } =
        new Dictionary<EnemyTier, IReadOnlyList<Enemy>>();
    public static IReadOnlyDictionary<Enemy, IReadOnlyList<Item>> CollectionItemsByEnemy { get; private set; } =
        new Dictionary<Enemy, IReadOnlyList<Item>>();
    public static IReadOnlyDictionary<Item, IReadOnlyList<Enemy>> EnemiesByDropItem { get; private set; } =
        new Dictionary<Item, IReadOnlyList<Enemy>>();
    public static IReadOnlyDictionary<string, IReadOnlyList<SkillActivity>> SkillActivitiesByName { get; private set; } =
        new Dictionary<string, IReadOnlyList<SkillActivity>>();
    public static int TotalCollectionSlots { get; private set; }
    public static bool IsInitialized { get; private set; }

    public static Task InitializeAsync(IProgress<StartupProgress> progress)
    {
        return _initializationTask ??= InitializeCoreAsync(progress);
    }

    public static Item? FindItem(string name)
    {
        if (ItemsByName.TryGetValue(name, out Item? item))
            return item;

        return IsInitialized
            ? null
            : ItemData.AllItems.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static Enemy? FindEnemy(string name)
    {
        if (EnemiesByName.TryGetValue(name, out Enemy? enemy))
            return enemy;

        return IsInitialized
            ? null
            : EnemyData.AllEnemies.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<Enemy> GetEnemies(EnemyTier tier)
    {
        if (EnemiesByTier.TryGetValue(tier, out IReadOnlyList<Enemy>? enemies))
            return enemies;

        return IsInitialized
            ? Array.Empty<Enemy>()
            : EnemyData.AllEnemies
                .Where(enemy => enemy.Tier == tier)
                .OrderBy(enemy => enemy.CombatLevel)
                .ThenBy(enemy => enemy.Name)
                .ToArray();
    }

    public static IReadOnlyList<Item> GetCollectionItems(Enemy enemy)
    {
        if (CollectionItemsByEnemy.TryGetValue(enemy, out IReadOnlyList<Item>? items))
            return items;

        return enemy.DropTable.Drops
            .Select(drop => drop.Item)
            .Distinct()
            .ToArray();
    }

    public static IReadOnlyList<Enemy> GetEnemiesDropping(Item item)
    {
        return EnemiesByDropItem.TryGetValue(item, out IReadOnlyList<Enemy>? enemies)
            ? enemies
            : Array.Empty<Enemy>();
    }

    public static IReadOnlyList<SkillActivity> GetSkillActivities(string skillName)
    {
        if (SkillActivitiesByName.TryGetValue(
            skillName,
            out IReadOnlyList<SkillActivity>? activities))
        {
            return activities;
        }

        return skillName.ToLowerInvariant() switch
        {
            "fishing" => GameData.Fishing,
            "mining" => GameData.Mining,
            "woodcutting" => GameData.Woodcutting,
            "agility" => GameData.Agility,
            "thieving" => GameData.Thieving,
            "crafting" => GameData.Crafting,
            "fletching" => GameData.Fletching,
            "farming" => GameData.Farming,
            _ => Array.Empty<SkillActivity>()
        };
    }

    private static async Task InitializeCoreAsync(IProgress<StartupProgress> progress)
    {
        progress.Report(new StartupProgress(0.08, "Preparing core data"));
        await Task.Yield();

        await Task.Run(() =>
        {
            _ = ExperienceTable.GetXPForLevel(99);
        });

        progress.Report(new StartupProgress(0.18, "Loading items"));
        Items = await Task.Run(() => (IReadOnlyList<Item>)ItemData.AllItems);
        ItemsByName = await Task.Run(() =>
            Items.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase));

        progress.Report(new StartupProgress(0.36, "Loading enemies and drops"));
        Enemies = await Task.Run(() => (IReadOnlyList<Enemy>)EnemyData.AllEnemies);

        progress.Report(new StartupProgress(0.52, "Building combat lookups"));
        await Task.Run(() =>
        {
            EnemiesByName = Enemies.ToDictionary(
                enemy => enemy.Name,
                StringComparer.OrdinalIgnoreCase);
            OrderedEnemies = Enemies
                .OrderBy(enemy => enemy.Tier)
                .ThenBy(enemy => enemy.CombatLevel)
                .ThenBy(enemy => enemy.Name)
                .ToArray();
            EnemiesByTier = Enum.GetValues<EnemyTier>()
                .ToDictionary(
                    tier => tier,
                    tier => (IReadOnlyList<Enemy>)Enemies
                        .Where(enemy => enemy.Tier == tier)
                        .OrderBy(enemy => enemy.CombatLevel)
                        .ThenBy(enemy => enemy.Name)
                        .ToArray());
        });

        progress.Report(new StartupProgress(0.66, "Indexing Collection Log"));
        await Task.Run(() =>
        {
            CollectionItemsByEnemy = Enemies.ToDictionary(
                enemy => enemy,
                enemy => (IReadOnlyList<Item>)enemy.DropTable.Drops
                    .Select(drop => drop.Item)
                    .Distinct()
                    .ToArray());
            EnemiesByDropItem = Enemies
                .SelectMany(enemy => GetCollectionItems(enemy)
                    .Select(item => (item, enemy)))
                .GroupBy(pair => pair.item)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<Enemy>)group
                        .Select(pair => pair.enemy)
                        .Distinct()
                        .ToArray());
            TotalCollectionSlots = CollectionItemsByEnemy.Values.Sum(items => items.Count) +
                SkillingPetData.AllPets.Count;
        });

        progress.Report(new StartupProgress(0.78, "Loading skills and activities"));
        await Task.Run(() =>
        {
            SkillActivitiesByName = new Dictionary<string, IReadOnlyList<SkillActivity>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Fishing"] = GameData.Fishing,
                ["Mining"] = GameData.Mining,
                ["Woodcutting"] = GameData.Woodcutting,
                ["Agility"] = GameData.Agility,
                ["Thieving"] = GameData.Thieving,
                ["Crafting"] = GameData.Crafting,
                ["Fletching"] = GameData.Fletching,
                ["Farming"] = GameData.Farming
            };
        });

        progress.Report(new StartupProgress(0.90, "Caching shared resources"));
        GameThemeCache.WarmUp();
        await Task.Run(() =>
        {
            // Materialize repeatedly-used resource paths once during startup.
            foreach (Item item in Items)
                _ = item.IconImage;

            foreach (Enemy enemy in Enemies)
            {
                _ = enemy.IconImage;
                _ = enemy.LargeIconImage;
            }
        });

        IsInitialized = true;
        progress.Report(new StartupProgress(0.96, "Validating game data"));
    }
}
