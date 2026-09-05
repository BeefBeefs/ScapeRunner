namespace OSRSIdle;

public static partial class EnemyData
{
    private static void AddSharedDrops()
    {
        for (int enemyIndex = 0; enemyIndex < AllEnemies.Count; enemyIndex++)
        {
            Enemy enemy = AllEnemies[enemyIndex];
            IReadOnlyList<Item> pool = GetSharedDropPool(enemy.Tier);
            int desiredDropCount = 1 + (enemyIndex % 3);
            int addedDropCount = 0;

            // Offset each enemy into its tier pool so adjacent enemies do not
            // receive an identical group, while every item still appears on
            // several drop tables.
            int poolIndex = (enemyIndex * 2) % pool.Count;

            for (int attempts = 0;
                 attempts < pool.Count * 2 && addedDropCount < desiredDropCount;
                 attempts++)
            {
                Item item = pool[(poolIndex + attempts) % pool.Count];

                if (enemy.DropTable.Drops.Any(drop => drop.Item == item))
                    continue;

                enemy.DropTable.Drops.Add(CreateSharedDrop(
                    item,
                    enemy.Tier));

                addedDropCount++;
            }
        }
    }

    private static IReadOnlyList<Item> GetSharedDropPool(EnemyTier tier)
    {
        return tier switch
        {
            EnemyTier.Tier1 => new Item[]
            {
                ItemData.Bones,
                ItemData.Feathers,
                ItemData.AnimalHide,
                ItemData.MonsterClaw,
                ItemData.Logs
            },
            EnemyTier.Tier2 => new Item[]
            {
                ItemData.Bones,
                ItemData.SpiderSilk,
                ItemData.WolfPelt,
                ItemData.IronOre,
                ItemData.EnchantedLeaf,
                ItemData.BogPearl,
                ItemData.EmberCore
            },
            EnemyTier.Tier3 => new Item[]
            {
                ItemData.CursedBone,
                ItemData.MonsterFang,
                ItemData.WerewolfPelt,
                ItemData.ManticoreHide,
                ItemData.AncientBone,
                ItemData.MoonstoneFragment,
                ItemData.ShadowSilk
            },
            EnemyTier.Tier4 => new Item[]
            {
                ItemData.MithrilOre,
                ItemData.MithrilBar,
                ItemData.RunicBar,
                ItemData.HarpyFeather,
                ItemData.FrostfangScale,
                ItemData.WyrmFang,
                ItemData.VoidEssence
            },
            EnemyTier.Tier5 => new Item[]
            {
                ItemData.AdamantiteOre,
                ItemData.RuniteOre,
                ItemData.DragonScale,
                ItemData.ObsidianHeart,
                ItemData.CrystalFang,
                ItemData.CelestialShard,
                ItemData.EmberCore
            },
            EnemyTier.Tier6 => new Item[]
            {
                ItemData.RuniteOre,
                ItemData.RunicBar,
                ItemData.DragonScale,
                ItemData.AncientDragonScale,
                ItemData.VoidEssence,
                ItemData.AstralDust,
                ItemData.GodFragment
            },
            _ => new Item[]
            {
                ItemData.AncientDragonScale,
                ItemData.VoidEssence,
                ItemData.AstralDust,
                ItemData.GodFragment,
                ItemData.CelestialShard
            }
        };
    }

    private static Drop CreateSharedDrop(
        Item item,
        EnemyTier tier)
    {
        (double chance, DropRarity rarity) = tier switch
        {
            EnemyTier.Tier1 => (0.15, DropRarity.Uncommon),
            EnemyTier.Tier2 => (0.08, DropRarity.Uncommon),
            EnemyTier.Tier3 => (0.04, DropRarity.Rare),
            EnemyTier.Tier4 => (0.02, DropRarity.Rare),
            EnemyTier.Tier5 => (0.01, DropRarity.VeryRare),
            EnemyTier.Tier6 => (0.005, DropRarity.VeryRare),
            _ => (0.002, DropRarity.SuperRare)
        };

        return new Drop
        {
            Item = item,
            Chance = chance,
            MinQuantity = 1,
            MaxQuantity = tier <= EnemyTier.Tier2 ? 3 : 2,
            Rarity = rarity
        };
    }
}
