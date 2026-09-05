namespace OSRSIdle;

public static partial class ItemData
{
    public static Item GoblinWarclub = CreateExpansionEquipment(
        "Goblin Warclub", EquipmentSlot.Weapon, 180, 7, 9, 1, attackSpeedTicks: 5);
    public static Item GoblinHelm = CreateExpansionEquipment(
        "Goblin Helm", EquipmentSlot.Head, 160, 1, 2, 6, 1);

    public static Item SwampCloak = CreateExpansionEquipment(
        "Swamp Cloak", EquipmentSlot.Body, 650, 3, 4, 14, 3);
    public static Item SwampKing = CreateExpansionEquipment(
        "Swamp King", EquipmentSlot.Amulet, 725, 6, 7, 7, 2);

    public static Item FrostbiteStaff = CreateExpansionEquipment(
        "Frostbite Staff", EquipmentSlot.Weapon, 1800, 20, 18, 6, 2);
    public static Item FrostGuard = CreateExpansionEquipment(
        "Frost Guard", EquipmentSlot.Shield, 1950, 5, 6, 25, 5);

    public static Item MagmaCore = CreateExpansionEquipment(
        "Magma Core", EquipmentSlot.Ring, 3900, 15, 22, 12, 5);
    public static Item EmberCape = CreateExpansionEquipment(
        "Ember Cape", EquipmentSlot.Body, 4200, 10, 18, 35, 8);

    public static Item SandsweptTablet = CreateExpansionEquipment(
        "Sandswept Tablet", EquipmentSlot.Shield, 8500, 18, 16, 48, 9);
    public static Item ScarabAmulet = CreateExpansionEquipment(
        "Scarab Amulet", EquipmentSlot.Amulet, 9200, 25, 28, 25, 7);

    public static Item AstralEssence = CreateExpansionEquipment(
        "Astral Essence", EquipmentSlot.Ring, 18000, 38, 42, 35, 10);
    public static Item VoidReaver = CreateExpansionEquipment(
        "Void Reaver", EquipmentSlot.Weapon, 22000, 62, 70, 15, 6, attackSpeedTicks: 3);

    public static Item UmbralShard = CreateExpansionEquipment(
        "Umbral Shard", EquipmentSlot.Amulet, 42000, 80, 90, 75, 15);
    public static Item EclipseCrown = CreateExpansionEquipment(
        "Eclipse Crown", EquipmentSlot.Head, 50000, 75, 85, 110, 20);

    private static IReadOnlyList<Item> GetAreaBossItems() =>
        new Item[]
        {
            GoblinWarclub, GoblinHelm,
            SwampCloak, SwampKing,
            FrostbiteStaff, FrostGuard,
            MagmaCore, EmberCape,
            SandsweptTablet, ScarabAmulet,
            AstralEssence, VoidReaver,
            UmbralShard, EclipseCrown
        };
}
