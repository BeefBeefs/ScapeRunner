using System.Text.Json;

namespace OSRSIdle;

public static class SaveManager
{
    private const int CurrentSaveVersion = 1;
    private const string SaveFileName = "osrsidle-save.json";
    private const string BackupFileName = "osrsidle-save.backup.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    private static readonly object SaveFileLock = new();
    private static long _nextSaveRequest;
    private static long _lastWrittenRequest;

    private static string SavePath =>
        Path.Combine(FileSystem.AppDataDirectory, SaveFileName);
    private static string BackupPath =>
        Path.Combine(FileSystem.AppDataDirectory, BackupFileName);

    public static string? LastError { get; private set; }
    public static bool RecoveredFromBackup { get; private set; }

    public static void Save(
        Player player,
        OfflineActivitySaveData? activeActivity = null,
        string lastCombatStyle = "Attack")
    {
        long requestId = CreateSaveRequest();

        PlayerSaveData saveData = CreateSaveData(
            player,
            activeActivity,
            lastCombatStyle);

        WriteSaveData(saveData, requestId);
    }

    public static PlayerSaveData CreateSaveData(
        Player player,
        OfflineActivitySaveData? activeActivity = null,
        string lastCombatStyle = "Attack")
    {
        lock (SaveFileLock)
        {
            return CreateSaveDataCore(
                player,
                activeActivity,
                lastCombatStyle);
        }
    }

    private static PlayerSaveData CreateSaveDataCore(
        Player player,
        OfflineActivitySaveData? activeActivity = null,
        string lastCombatStyle = "Attack")
    {
        return new PlayerSaveData
        {
            SkillXP = player.GetAllSkills()
                .ToDictionary(skill => skill.Name, skill => skill.XP),
            SkillActions = player.GetAllSkills()
                .ToDictionary(skill => skill.Name, skill => skill.ActionsCompleted),
            CharacterName = player.Name,
            CharacterPortraitIndex = PlayerPortraits.NormalizeIndex(
                player.PortraitIndex),
            AutoEatThresholdPercent = player.AutoEatThresholdPercent,
            InventorySlots = player.Inventory.SlotCapacity,
            Inventory = player.Inventory.CreateSnapshot()
                .Select(entry => new InventorySaveItem
                {
                    ItemName = entry.Item.Name,
                    Quantity = entry.Quantity,
                    UpgradeLevel = entry.Item.UpgradeLevel
                })
                .ToList(),
            Equipment = Enum.GetValues<EquipmentSlot>()
                .Where(slot => slot != EquipmentSlot.None)
                .ToDictionary(
                    slot => slot.ToString(),
                    slot => player.GetEquippedItem(slot)?.Name),
            CurrentHP = player.CurrentHP,
            CollectionLog = player.CollectionLog.CreateSaveData(),
            LuckiestDrop = player.LuckiestDrop,
            SavedAtUtc = DateTime.UtcNow,
            ActiveActivity = activeActivity,
            LastCombatStyle = lastCombatStyle
        };
    }

    public static long CreateSaveRequest() =>
        Interlocked.Increment(ref _nextSaveRequest);

    public static Task SaveAsync(PlayerSaveData saveData, long requestId)
    {
        return Task.Run(() => WriteSaveData(saveData, requestId));
    }

    private static void WriteSaveData(PlayerSaveData saveData, long requestId)
    {
        string temporaryPath = SavePath + ".tmp";

        try
        {
            string json = JsonSerializer.Serialize(saveData, JsonOptions);

            lock (SaveFileLock)
            {
                if (requestId < _lastWrittenRequest)
                    return;

                // Write the new snapshot separately, then replace the main
                // file. A crash during serialization or the write cannot
                // truncate the last known-good save.
                File.WriteAllText(temporaryPath, json);
                if (File.Exists(SavePath))
                    File.Copy(SavePath, BackupPath, true);
                File.Move(temporaryPath, SavePath, true);
                _lastWrittenRequest = requestId;
                LastError = null;
            }
        }
        catch (Exception exception)
        {
            LastError = $"Save failed: {exception.Message}";
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // The next save can clean up a leftover temporary snapshot.
            }
        }
    }

    public static OfflineProgressLoadResult Load(Player player)
    {
        LastError = null;
        RecoveredFromBackup = false;

        lock (SaveFileLock)
        {
            if (!File.Exists(SavePath) && !File.Exists(BackupPath))
                return OfflineProgressLoadResult.None;

            try
            {
                PlayerSaveData? saveData = ReadSaveData(SavePath);
                if (saveData == null)
                    throw new InvalidDataException("The primary save is invalid.");

                return ApplySaveData(player, saveData);
            }
            catch (Exception exception)
            {
                LastError = $"Save could not be loaded: {exception.Message}";
            }

            try
            {
                PlayerSaveData? backup = ReadSaveData(BackupPath);
                if (backup == null)
                    throw new InvalidDataException("The backup save is invalid.");

                File.Copy(BackupPath, SavePath, true);
                RecoveredFromBackup = true;
                LastError = null;
                return ApplySaveData(player, backup);
            }
            catch (Exception backupException)
            {
                LastError = $"Save recovery failed: {backupException.Message}";
            }

            return OfflineProgressLoadResult.None;
        }
    }

    private static PlayerSaveData? ReadSaveData(string path)
    {
        if (!File.Exists(path))
            return null;

        PlayerSaveData? saveData = JsonSerializer.Deserialize<PlayerSaveData>(
            File.ReadAllText(path),
            JsonOptions);

        return saveData?.Version == CurrentSaveVersion
            ? saveData
            : null;
    }

    private static OfflineProgressLoadResult ApplySaveData(
        Player player,
        PlayerSaveData saveData)
    {
        foreach (Skill skill in player.GetAllSkills())
        {
            if (saveData.SkillXP.TryGetValue(skill.Name, out double xp))
                skill.RestoreXP(xp);

            if (saveData.SkillActions.TryGetValue(skill.Name, out long actions))
                skill.RestoreActions(actions);
        }

        if (!string.IsNullOrWhiteSpace(saveData.CharacterName))
            player.Name = saveData.CharacterName.Trim();

        player.PortraitIndex = PlayerPortraits.NormalizeIndex(
            saveData.CharacterPortraitIndex);
        player.RestoreAutoEatThresholdPercent(
            saveData.AutoEatThresholdPercent);

        player.Inventory.Clear();

        int savedSlotCapacity = Math.Max(
            Inventory.StartingSlotCapacity,
            saveData.InventorySlots);

        int occupiedSavedSlots = saveData.Inventory.Count(entry =>
            entry.Quantity > 0 &&
            FindItem(entry.ItemName)?.Type is not
                (ItemType.Pet or ItemType.Currency));

        player.Inventory.RestoreSlotCapacity(
            Math.Max(savedSlotCapacity, occupiedSavedSlots));

        foreach (InventorySaveItem entry in saveData.Inventory)
        {
            Item? item = RestoreUpgrade(
                FindItem(entry.ItemName),
                entry.UpgradeLevel);

            if (item != null && entry.Quantity > 0)
                player.Inventory.AddItem(item, entry.Quantity);
        }

        player.ClearEquippedItems();

        foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>())
        {
            if (slot == EquipmentSlot.None)
                continue;

            saveData.Equipment.TryGetValue(
                slot.ToString(),
                out string? itemName);

            Item? item = string.IsNullOrWhiteSpace(itemName)
                ? null
                : FindItem(itemName);

            item = RestoreUpgrade(item, ParseUpgradeLevel(itemName));

            if (item == null ||
                (slot != EquipmentSlot.Food && item.EquipmentSlot != slot) ||
                (slot == EquipmentSlot.Food && item.Type != ItemType.Food))
            {
                continue;
            }

            player.RestoreEquippedItem(slot, item);
        }

        player.CurrentHP = Math.Clamp(
            saveData.CurrentHP,
            0,
            player.GetMaxHP());

        player.CollectionLog.RestoreSaveData(saveData.CollectionLog);
        player.RestoreLuckiestDrop(saveData.LuckiestDrop);

        return new OfflineProgressLoadResult(
            GetElapsedTicks(saveData.SavedAtUtc),
            saveData.ActiveActivity,
            saveData.LastCombatStyle);
    }

    public static long GetElapsedTicks(DateTime savedAtUtc)
    {
        if (savedAtUtc == default)
            return 0;

        double elapsedMilliseconds =
            (DateTime.UtcNow - savedAtUtc).TotalMilliseconds;

        return elapsedMilliseconds <= 0
            ? 0
            : (long)(elapsedMilliseconds / 600d);
    }

    public static SavePreview GetPreview(Player freshPlayer)
    {
        int defaultSkillTotal = freshPlayer.GetAllSkills()
            .Sum(skill => skill.Level);

        int totalCollectionEntries = StartupDataCache.TotalCollectionSlots;

        try
        {
            if (!File.Exists(SavePath) && !File.Exists(BackupPath))
            {
                return new SavePreview(
                    freshPlayer.Name,
                    freshPlayer.PortraitIndex,
                    defaultSkillTotal,
                    0,
                    0,
                    totalCollectionEntries,
                    "No activity",
                    0,
                    default);
            }

            PlayerSaveData? saveData;
            lock (SaveFileLock)
            {
                try
                {
                    saveData = ReadSaveData(SavePath);
                }
                catch
                {
                    saveData = null;
                }

                if (saveData == null)
                {
                    try
                    {
                        saveData = ReadSaveData(BackupPath);
                    }
                    catch
                    {
                        saveData = null;
                    }

                    if (saveData != null)
                        RecoveredFromBackup = true;
                }
            }

            if (saveData == null)
                throw new InvalidDataException();

            int skillTotal = freshPlayer.GetAllSkills()
                .Sum(skill => saveData.SkillXP.TryGetValue(skill.Name, out double xp)
                    ? ExperienceTable.GetLevel(xp)
                    : skill.Level);

            int totalKills = saveData.CollectionLog.KillCounts.Values.Sum();

            HashSet<string> obtainedItemNames = saveData.CollectionLog.ReceivedDrops.Values
                .SelectMany(itemNames => itemNames)
                .ToHashSet(StringComparer.Ordinal);

            int obtainedEntries = StartupDataCache.Enemies
                .Sum(enemy => StartupDataCache.GetCollectionItems(enemy)
                    .Count(item => obtainedItemNames.Contains(item.Name))) +
                saveData.CollectionLog.SkillingPets.Count;

            return new SavePreview(
                string.IsNullOrWhiteSpace(saveData.CharacterName)
                    ? freshPlayer.Name
                    : saveData.CharacterName.Trim(),
                PlayerPortraits.NormalizeIndex(
                    saveData.CharacterPortraitIndex),
                skillTotal,
                totalKills,
                obtainedEntries,
                totalCollectionEntries,
                GetActivitySummary(saveData.ActiveActivity),
                GetElapsedTicks(saveData.SavedAtUtc),
                saveData.SavedAtUtc);
        }
        catch
        {
            return new SavePreview(
                freshPlayer.Name,
                freshPlayer.PortraitIndex,
                defaultSkillTotal,
                0,
                0,
                totalCollectionEntries,
                "No activity",
                0,
                default);
        }
    }

    private static string GetActivitySummary(
        OfflineActivitySaveData? activity)
    {
        if (activity == null)
            return "No activity";

        if (activity.Kind == "Skilling" &&
            !string.IsNullOrWhiteSpace(activity.SkillName))
        {
            return activity.SkillName;
        }

        if (activity.Kind == "Combat" &&
            !string.IsNullOrWhiteSpace(activity.ActivityName))
        {
            return $"Fighting {activity.ActivityName}";
        }

        return "No activity";
    }

    private static Item? FindItem(string itemName)
    {
        string baseName = itemName;
        int marker = itemName.LastIndexOf(" +", StringComparison.Ordinal);
        if (marker >= 0 && int.TryParse(itemName[(marker + 2)..], out _))
            baseName = itemName[..marker];

        // Stolen Coins was replaced by stackable regular Coins. Resolve the
        // old name so existing character saves are upgraded on load.
        if (string.Equals(baseName, "Stolen Coins", StringComparison.Ordinal))
            return ItemData.Coins;

        return StartupDataCache.FindItem(baseName);
    }

    private static Item? RestoreUpgrade(Item? item, int upgradeLevel)
    {
        if (item == null)
            return null;

        for (int level = 0; level < Math.Clamp(upgradeLevel, 0, 10); level++)
            item = item.CreateUpgrade();

        return item;
    }

    private static int ParseUpgradeLevel(string? itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return 0;

        int marker = itemName.LastIndexOf(" +", StringComparison.Ordinal);
        return marker >= 0 && int.TryParse(itemName[(marker + 2)..], out int level)
            ? Math.Clamp(level, 0, 10)
            : 0;
    }

    public static void DeleteSave()
    {
        lock (SaveFileLock)
        {
            // Invalidate any already queued async write before removing the
            // files. Otherwise a delayed save could resurrect a reset
            // character after this method returns.
            _lastWrittenRequest = Interlocked.Increment(ref _nextSaveRequest);

            try
            {
                if (File.Exists(SavePath))
                    File.Delete(SavePath);

                if (File.Exists(BackupPath))
                    File.Delete(BackupPath);

                string temporaryPath = SavePath + ".tmp";
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                LastError = null;
                RecoveredFromBackup = false;
            }
            catch (Exception exception)
            {
                LastError = $"Save deletion failed: {exception.Message}";
            }
        }
    }

    public static void UpdateCharacterProfile(
        string? characterName = null,
        int? portraitIndex = null)
    {
        long requestId = CreateSaveRequest();

        lock (SaveFileLock)
        {
            try
            {
                PlayerSaveData saveData;
                if (File.Exists(SavePath))
                {
                    try
                    {
                        saveData = ReadSaveData(SavePath)
                            ?? throw new InvalidDataException(
                                "The existing save is invalid.");
                    }
                    catch when (File.Exists(BackupPath))
                    {
                        saveData = ReadSaveData(BackupPath)
                            ?? throw new InvalidDataException(
                                "The backup save is invalid.");
                        File.Copy(BackupPath, SavePath, true);
                        RecoveredFromBackup = true;
                    }
                }
                else if (File.Exists(BackupPath))
                {
                    saveData = ReadSaveData(BackupPath)
                        ?? throw new InvalidDataException(
                            "The backup save is invalid.");
                }
                else
                {
                    saveData = new PlayerSaveData();
                }

                if (!string.IsNullOrWhiteSpace(characterName))
                    saveData.CharacterName = characterName.Trim();

                if (portraitIndex.HasValue)
                {
                    saveData.CharacterPortraitIndex =
                        PlayerPortraits.NormalizeIndex(portraitIndex.Value);
                }

                // Use the same ordered, atomic write path as gameplay saves.
                WriteSaveData(saveData, requestId);
            }
            catch (Exception exception)
            {
                LastError = $"Profile update failed: {exception.Message}";
            }
        }
    }
}

public sealed class PlayerSaveData
{
    public int Version { get; set; } = 1;

    public Dictionary<string, double> SkillXP { get; set; } = new();

    public Dictionary<string, long> SkillActions { get; set; } = new();

    public string CharacterName { get; set; } = "Adventurer";

    public int CharacterPortraitIndex { get; set; }

    public int AutoEatThresholdPercent { get; set; } = 50;

    public int InventorySlots { get; set; } = global::OSRSIdle.Inventory.StartingSlotCapacity;

    public List<InventorySaveItem> Inventory { get; set; } = new();

    public Dictionary<string, string?> Equipment { get; set; } = new();

    public int CurrentHP { get; set; }

    public CollectionLogSaveData CollectionLog { get; set; } = new();

    public LuckiestDrop? LuckiestDrop { get; set; }

    public DateTime SavedAtUtc { get; set; }

    public OfflineActivitySaveData? ActiveActivity { get; set; }

    public string LastCombatStyle { get; set; } = "Attack";
}

public sealed class OfflineActivitySaveData
{
    public string Kind { get; set; } = "";

    public string SkillName { get; set; } = "";

    public string ActivityName { get; set; } = "";

    public bool IsAutoFight { get; set; }

    public bool IsRespawning { get; set; }

    public int RespawnTicksRemaining { get; set; }

    public int EnemyCurrentHP { get; set; }

    public double PlayerAttackProgress { get; set; }

    public double EnemyAttackProgress { get; set; }

    public int AutoEatCooldownTicks { get; set; }

    public string CombatStyle { get; set; } = "Attack";
}

public readonly record struct OfflineProgressLoadResult(
    long ElapsedTicks,
    OfflineActivitySaveData? ActiveActivity,
    string LastCombatStyle)
{
    public static OfflineProgressLoadResult None => new(0, null, "Attack");
}

public sealed class InventorySaveItem
{
    public string ItemName { get; set; } = "";

    public int Quantity { get; set; }

    public int UpgradeLevel { get; set; }
}

public sealed class CollectionLogSaveData
{
    public Dictionary<string, int> KillCounts { get; set; } = new();

    public Dictionary<string, List<string>> ReceivedDrops { get; set; } = new();

    public List<string> SkillingPets { get; set; } = new();
}

public readonly record struct SavePreview(
    string CharacterName,
    int PortraitIndex,
    int SkillTotal,
    int TotalKills,
    int ObtainedCollectionEntries,
    int TotalCollectionEntries,
    string LastActivity,
    long OfflineTicks,
    DateTime SavedAtUtc);
