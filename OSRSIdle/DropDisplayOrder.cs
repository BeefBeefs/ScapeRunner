namespace OSRSIdle;

/// <summary>
/// Provides one consistent presentation order for enemy drop tables.
/// Gameplay roll order remains unchanged.
/// </summary>
public static class DropDisplayOrder
{
    public static IEnumerable<Drop> ForEnemy(Enemy enemy) =>
        enemy.DropTable.Drops
            .OrderBy(drop => drop.Rarity)
            .ThenByDescending(drop => drop.Chance)
            .ThenBy(drop => drop.Item.Name, StringComparer.OrdinalIgnoreCase);
}
