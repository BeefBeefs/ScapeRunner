namespace OSRSIdle;

public static partial class EnemyData
{
    private static Enemy CreateAreaBoss(
        string name,
        EnemyTier tier,
        int hp,
        int attack,
        int strength,
        int defense,
        int attackSpeedTicks,
        Item primaryDrop,
        Item secondaryDrop)
    {
        return new Enemy
        {
            Name = name,
            Icon = "",
            Tier = tier,
            HP = hp,
            Attack = attack,
            Strength = strength,
            Defense = defense,
            AttackSpeedTicks = attackSpeedTicks,
            DropTable = new DropTable
            {
                Drops = new List<Drop>
                {
                    new()
                    {
                        Item = ItemData.Coins,
                        Chance = 1.0,
                        MinQuantity = Math.Max(10, hp / 2),
                        MaxQuantity = Math.Max(25, hp),
                        Rarity = DropRarity.Common
                    },
                    new()
                    {
                        Item = primaryDrop,
                        Chance = 0.02,
                        MinQuantity = 1,
                        MaxQuantity = 1,
                        Rarity = DropRarity.Rare
                    },
                    new()
                    {
                        Item = secondaryDrop,
                        Chance = 0.004,
                        MinQuantity = 1,
                        MaxQuantity = 1,
                        Rarity = DropRarity.VeryRare
                    }
                }
            }
        };
    }

    private static void AddAreaBosses()
    {
        EnemyRegistry.AddRange(
            new[]
            {
                CreateAreaBoss(
                    "Goblin Warlord", EnemyTier.Tier1,
                    18, 8, 10, 8, 6,
                    ItemData.GoblinWarclub, ItemData.GoblinHelm),
                CreateAreaBoss(
                    "Swamp King", EnemyTier.Tier2,
                    44, 25, 27, 24, 7,
                    ItemData.SwampCloak, ItemData.SwampKing),
                CreateAreaBoss(
                    "Frostmaw Leviathan", EnemyTier.Tier3,
                    70, 43, 47, 40, 7,
                    ItemData.FrostbiteStaff, ItemData.FrostGuard),
                CreateAreaBoss(
                    "The Molten Colossus", EnemyTier.Tier4,
                    105, 65, 70, 60, 8,
                    ItemData.MagmaCore, ItemData.EmberCape),
                CreateAreaBoss(
                    "Sandsoul Pharaoh", EnemyTier.Tier5,
                    145, 95, 100, 80, 7,
                    ItemData.SandsweptTablet, ItemData.ScarabAmulet),
                CreateAreaBoss(
                    "The Voidcaller", EnemyTier.Tier6,
                    175, 120, 125, 100, 6,
                    ItemData.AstralEssence, ItemData.VoidReaver),
                CreateAreaBoss(
                    "Aethereal Sovereign", EnemyTier.Tier7,
                    380, 285, 305, 270, 6,
                    ItemData.UmbralShard, ItemData.EclipseCrown)
            });
    }
}
