namespace OSRSIdle;

public class CollectionLog
{
    private readonly Dictionary<Enemy, HashSet<Item>> _receivedDrops =
        new();

    // Item discovery is global. If an item appears on several enemy drop
    // tables, receiving it from any one of them unlocks every matching slot.
    private readonly HashSet<Item> _receivedItems =
        new();

    private readonly Dictionary<Enemy, int> _killCounts =
        new();

    private readonly HashSet<Item> _receivedSkillingPets =
        new();

    public event Action? CollectionChanged;

    public event Action<Enemy>? KillCountChanged;

    public event Action<Item>? DropDiscovered;

    public event Action<Item>? SkillingPetDiscovered;

    public event Action<Enemy>? CollectionCompleted;

    public void RecordKill(Enemy enemy)
    {
        _killCounts.TryGetValue(enemy, out int currentCount);

        _killCounts[enemy] = currentCount + 1;

        KillCountChanged?.Invoke(enemy);
        CollectionChanged?.Invoke();
    }

    public int GetKillCount(Enemy enemy)
    {
        return _killCounts.TryGetValue(enemy, out int killCount)
            ? killCount
            : 0;
    }

    public int GetTotalKillCount()
    {
        return _killCounts.Values.Sum();
    }

    public int GetCompletedEnemyCount()
    {
        IReadOnlyList<Enemy> enemies = StartupDataCache.IsInitialized
            ? StartupDataCache.Enemies
            : EnemyData.AllEnemies;

        return enemies.Count(IsComplete);
    }

    public void RecordDrops(
        Enemy enemy,
        IEnumerable<LootResult> loot)
    {
        bool wasComplete = IsComplete(enemy);
        bool changed =
            false;

        foreach (LootResult lootResult in loot)
        {
            if (RecordDrop(
                    enemy,
                    lootResult.Item,
                    notify: false))
            {
                changed =
                    true;
            }
        }

        if (changed)
        {
            CollectionChanged?.Invoke();

            if (!wasComplete && IsComplete(enemy))
                CollectionCompleted?.Invoke(enemy);
        }
    }

    public bool HasReceivedDrop(
        Enemy enemy,
        Item item)
    {
        _ = enemy;
        return _receivedItems.Contains(item);
    }

    public void RecordSkillingPet(
        Item pet)
    {
        if (_receivedSkillingPets.Add(pet))
        {
            SkillingPetDiscovered?.Invoke(pet);
            CollectionChanged?.Invoke();
        }
    }

    public bool HasReceivedSkillingPet(
        Item pet)
    {
        return _receivedSkillingPets.Contains(pet);
    }

    public int GetObtainedDropCount(
        Enemy enemy)
    {
        return StartupDataCache.GetCollectionItems(enemy)
            .Count(item => HasReceivedDrop(enemy, item));
    }

    public int GetDropCount(
        Enemy enemy)
    {
        return StartupDataCache.GetCollectionItems(enemy).Count;
    }

    public bool IsComplete(
        Enemy enemy)
    {
        int dropCount =
            GetDropCount(enemy);

        return dropCount > 0 &&
            GetObtainedDropCount(enemy) == dropCount;
    }

    public CollectionLogSaveData CreateSaveData()
    {
        return new CollectionLogSaveData
        {
            KillCounts = _killCounts.ToDictionary(
                entry => entry.Key.Name,
                entry => entry.Value),
            ReceivedDrops = _receivedDrops.ToDictionary(
                entry => entry.Key.Name,
                entry => entry.Value.Select(item => item.Name).ToList()),
            SkillingPets = _receivedSkillingPets
                .Select(item => item.Name)
                .ToList()
        };
    }

    public void RestoreSaveData(CollectionLogSaveData data)
    {
        _killCounts.Clear();
        _receivedDrops.Clear();
        _receivedItems.Clear();
        _receivedSkillingPets.Clear();

        foreach ((string enemyName, int killCount) in data.KillCounts)
        {
            Enemy? enemy = StartupDataCache.FindEnemy(enemyName);

            if (enemy != null && killCount > 0)
            {
                _killCounts[enemy] = killCount;
            }
        }

        foreach ((string enemyName, List<string> itemNames) in data.ReceivedDrops)
        {
            Enemy? enemy = StartupDataCache.FindEnemy(enemyName);

            if (enemy == null)
                continue;

            HashSet<Item> drops = itemNames
                .Select(FindItem)
                .OfType<Item>()
                .ToHashSet();

            if (drops.Count > 0)
            {
                _receivedDrops[enemy] = drops;
                _receivedItems.UnionWith(drops);
            }
        }

        foreach (string itemName in data.SkillingPets)
        {
            Item? pet = FindItem(itemName);

            if (pet != null)
            {
                _receivedSkillingPets.Add(pet);
            }
        }

        CollectionChanged?.Invoke();
    }

    private bool RecordDrop(
        Enemy enemy,
        Item item,
        bool notify)
    {
        if (!_receivedDrops.TryGetValue(
                enemy,
                out HashSet<Item>? receivedDrops))
        {
            receivedDrops =
                new HashSet<Item>();

            _receivedDrops[enemy] =
                receivedDrops;
        }

        receivedDrops.Add(item);

        bool added =
            _receivedItems.Add(item);

        if (added && notify)
        {
            DropDiscovered?.Invoke(item);
            CollectionChanged?.Invoke();
        }

        else if (added)
        {
            DropDiscovered?.Invoke(item);
        }

        return added;
    }

    private static Item? FindItem(string itemName)
    {
        return StartupDataCache.FindItem(itemName);
    }
}
