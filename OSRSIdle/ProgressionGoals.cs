namespace OSRSIdle;

public readonly record struct ProgressionGoalState(
    string Name,
    int Current,
    int Target,
    bool Complete)
{
    public string ProgressText =>
        Complete ? "COMPLETE" : $"{Current:N0} / {Target:N0}";
}

public static class ProgressionGoals
{
    public static IReadOnlyList<ProgressionGoalState> GetFor(Player player)
    {
        int collectionEntries = StartupDataCache.Enemies.Sum(enemy =>
            player.CollectionLog.GetObtainedDropCount(enemy));

        collectionEntries += SkillingPetData.AllPets.Count(pet =>
            player.CollectionLog.HasReceivedSkillingPet(pet.Item));

        bool hasUpgrade = player.Inventory.CreateSnapshot().Any(entry => entry.Item.UpgradeLevel > 0) ||
            Enum.GetValues<EquipmentSlot>()
                .Where(slot => slot != EquipmentSlot.None)
                .Select(player.GetEquippedItem)
                .OfType<Item>()
                .Any(item => item.UpgradeLevel > 0);

        return new ProgressionGoalState[]
        {
            Create("First steps", player.GetAllSkills().Max(skill => skill.Level), 10),
            Create("Proven fighter", player.CollectionLog.GetTotalKillCount(), 25),
            Create("Collector", collectionEntries, 10),
            Create("Area champion", player.CollectionLog.GetCompletedEnemyCount(), 1),
            Create("Equipment upgrade", hasUpgrade ? 1 : 0, 1)
        };
    }

    private static ProgressionGoalState Create(string name, int current, int target) =>
        new(name, Math.Min(current, target), target, current >= target);
}
