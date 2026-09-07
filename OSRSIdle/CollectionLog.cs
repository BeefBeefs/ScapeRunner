namespace OSRSIdle;

public class CollectionLog
{
    private readonly object _stateLock = new();

    private readonly Dictionary<Enemy, int> _obtainedDropCounts = new();
    private int? _completedEnemyCount;
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
        RecordKills(enemy, 1);
    }

    public void RecordKills(Enemy enemy, long count)
    {
        if (count <= 0)
            return;

        lock (_stateLock)
        {
            _killCounts.TryGetValue(enemy, out int currentCount);
            int updatedCount = currentCount + (int)Math.Min(
                count,
                int.MaxValue - (long)currentCount);

            if (updatedCount == currentCount)
                return;

            _killCounts[enemy] = updatedCount;
        }

        KillCountChanged?.Invoke(enemy);
        CollectionChanged?.Invoke();
    }

    public int GetKillCount(Enemy enemy)
    {
        lock (_stateLock)
        {
            return _killCounts.TryGetValue(enemy, out int killCount)
                ? killCount
                : 0;
        }
    }

    public int GetTotalKillCount()
    {
        lock (_stateLock)
            return _killCounts.Values.Sum();
    }

    public int GetCompletedEnemyCount()
    {
        lock (_stateLock)
        {
            if (_completedEnemyCount is int cachedCount)
                return cachedCount;

            IReadOnlyList<Enemy> enemies = StartupDataCache.IsInitialized
                ? StartupDataCache.Enemies
                : EnemyData.AllEnemies;

            int count = enemies.Count(IsCompleteCore);
            _completedEnemyCount = count;
            return count;
        }
    }

    public void RecordDrops(
        Enemy enemy,
        IEnumerable<LootResult> loot)
    {
        List<Item>? discoveredItems = null;
        bool completed;
        bool changed;

        lock (_stateLock)
        {
            bool wasComplete = IsCompleteCore(enemy);
            changed = false;

            foreach (LootResult lootResult in loot)
            {
                if (!RecordDropCore(enemy, lootResult.Item))
                    continue;

                changed = true;
                (discoveredItems ??= new List<Item>())
                    .Add(lootResult.Item);
            }

            completed = changed &&
                !wasComplete &&
                IsCompleteCore(enemy);
        }

        if (!changed)
            return;

        if (discoveredItems != null)
        {
            foreach (Item item in discoveredItems)
                DropDiscovered?.Invoke(item);
        }

        CollectionChanged?.Invoke();

        if (completed)
            CollectionCompleted?.Invoke(enemy);
    }

    public bool HasReceivedDrop(
        Enemy enemy,
        Item item)
    {
        _ = enemy;
        lock (_stateLock)
            return _receivedItems.Contains(item);
    }

    internal bool HasRecordedDrop(Enemy enemy, Item item)
    {
        lock (_stateLock)
        {
            return _receivedDrops.TryGetValue(
                enemy,
                out HashSet<Item>? receivedDrops) &&
                receivedDrops.Contains(item);
        }
    }

    public void RecordSkillingPet(
        Item pet)
    {
        lock (_stateLock)
        {
            if (!_receivedSkillingPets.Add(pet))
                return;
        }

        SkillingPetDiscovered?.Invoke(pet);
        CollectionChanged?.Invoke();
    }

    public bool HasReceivedSkillingPet(
        Item pet)
    {
        lock (_stateLock)
            return _receivedSkillingPets.Contains(pet);
    }

    public int GetObtainedDropCount(
        Enemy enemy)
    {
        lock (_stateLock)
            return GetObtainedDropCountCore(enemy);
    }

    public int GetDropCount(
        Enemy enemy)
    {
        return StartupDataCache.GetCollectionItems(enemy).Count;
    }

    public bool IsComplete(
        Enemy enemy)
    {
        lock (_stateLock)
            return IsCompleteCore(enemy);
    }

    public CollectionLogSaveData CreateSaveData()
    {
        lock (_stateLock)
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
    }

    public void RestoreSaveData(CollectionLogSaveData data)
    {
        lock (_stateLock)
        {
            InvalidateCompletionCache();
            _killCounts.Clear();
            _receivedDrops.Clear();
            _receivedItems.Clear();
            _receivedSkillingPets.Clear();

            foreach ((string enemyName, int killCount) in data.KillCounts)
            {
                Enemy? enemy = StartupDataCache.FindEnemy(enemyName);

                if (enemy != null && killCount > 0)
                    _killCounts[enemy] = killCount;
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
                    _receivedSkillingPets.Add(pet);
            }
        }

        CollectionChanged?.Invoke();
    }

    private bool RecordDropCore(
        Enemy enemy,
        Item item)
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

        // Discovery is global across shared drop tables. Invalidate before
        // notifying listeners so combat bonuses and every screen see it.
        if (added)
            InvalidateCompletionCache();

        return added;
    }

    private int GetObtainedDropCountCore(Enemy enemy)
    {
        if (_obtainedDropCounts.TryGetValue(enemy, out int count))
            return count;

        count = 0;
        foreach (Item item in StartupDataCache.GetCollectionItems(enemy))
            if (_receivedItems.Contains(item))
                count++;

        _obtainedDropCounts[enemy] = count;
        return count;
    }

    private bool IsCompleteCore(Enemy enemy)
    {
        int dropCount = GetDropCount(enemy);

        return dropCount > 0 &&
            GetObtainedDropCountCore(enemy) == dropCount;
    }

    private static Item? FindItem(string itemName)
    {
        return StartupDataCache.FindItem(itemName);
    }

    private void InvalidateCompletionCache()
    {
        _obtainedDropCounts.Clear();
        _completedEnemyCount = null;
    }
}
