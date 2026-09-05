namespace OSRSIdle;

public static partial class EnemyData
{
    /// <summary>
    /// Keeps unusually powerful equipment from appearing as a routine drop
    /// on an enemy that is far below the gear's intended progression band.
    /// A few thematic exceptions can still appear, but only at chase-item
    /// odds rather than ordinary rare-drop odds.
    /// </summary>
    private static void BalanceEquipmentDropRates()
    {
        foreach (Enemy enemy in AllEnemies)
        {
            int maximumExpectedPower = enemy.Tier switch
            {
                EnemyTier.Tier1 => 24,
                EnemyTier.Tier2 => 55,
                EnemyTier.Tier3 => 95,
                EnemyTier.Tier4 => 155,
                EnemyTier.Tier5 => 230,
                EnemyTier.Tier6 => 340,
                _ => int.MaxValue
            };

            foreach (Drop drop in enemy.DropTable.Drops)
            {
                if (drop.Item.Type != ItemType.Equipment)
                    continue;

                int itemPower = drop.Item.AttackBonus +
                                drop.Item.StrengthBonus +
                                drop.Item.DefenseBonus +
                                drop.Item.HPBonus;

                if (itemPower <= maximumExpectedPower)
                    continue;

                // High-tier gear on a lower-tier enemy is allowed as an
                // exciting jackpot, but never as a commonly seen drop.
                drop.Chance = Math.Min(drop.Chance, 0.0001d);
                if (drop.Rarity < DropRarity.SuperRare)
                    drop.Rarity = DropRarity.SuperRare;
            }
        }
    }
}
