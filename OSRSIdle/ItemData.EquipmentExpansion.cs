namespace OSRSIdle;

public static partial class ItemData
{
    private static Item CreateExpansionEquipment(
        string name,
        EquipmentSlot slot,
        int value,
        int attack,
        int strength,
        int defense,
        int hp = 0,
        int attackSpeedTicks = 4)
    {
        return new Item
        {
            Name = name,
            Icon = "",
            Value = value,
            Type = ItemType.Equipment,
            EquipmentSlot = slot,
            AttackBonus = attack,
            StrengthBonus = strength,
            DefenseBonus = defense,
            HPBonus = hp,
            AttackSpeedTicks = attackSpeedTicks
        };
    }

    public static Item FeatheredCap = CreateExpansionEquipment("Feathered Cap", EquipmentSlot.Head, 15, 0, 0, 1);
    public static Item RoosterSpurBoots = CreateExpansionEquipment("Rooster-Spur Boots", EquipmentSlot.Boots, 20, 0, 1, 1);
    public static Item MosswoodBuckler = CreateExpansionEquipment("Mosswood Buckler", EquipmentSlot.Shield, 25, 0, 0, 2);
    public static Item RatboneRing = CreateExpansionEquipment("Ratbone Ring", EquipmentSlot.Ring, 30, 1, 0, 0);
    public static Item ImpishGloves = CreateExpansionEquipment("Impish Gloves", EquipmentSlot.Gloves, 35, 1, 1, 1);
    public static Item ShellplateVest = CreateExpansionEquipment("Shellplate Vest", EquipmentSlot.Body, 45, 0, 0, 4, 1);
    public static Item SilkweaveLeggings = CreateExpansionEquipment("Silkweave Leggings", EquipmentSlot.Legs, 50, 1, 1, 3);
    public static Item ScoutsShortblade = CreateExpansionEquipment("Scout's Shortblade", EquipmentSlot.Weapon, 65, 4, 3, 0, attackSpeedTicks: 3);
    public static Item CinderfangAmulet = CreateExpansionEquipment("Cinderfang Amulet", EquipmentSlot.Amulet, 70, 2, 3, 0);
    public static Item TrollhideBoots = CreateExpansionEquipment("Trollhide Boots", EquipmentSlot.Boots, 80, 0, 1, 5);

    public static Item GloomwingCowl = CreateExpansionEquipment("Gloomwing Cowl", EquipmentSlot.Head, 110, 3, 2, 6);
    public static Item HexwoodWand = CreateExpansionEquipment("Hexwood Wand", EquipmentSlot.Weapon, 130, 7, 5, 0);
    public static Item BriarbarkBody = CreateExpansionEquipment("Briarbark Body", EquipmentSlot.Body, 150, 1, 2, 10, 1);
    public static Item GraveguardShield = CreateExpansionEquipment("Graveguard Shield", EquipmentSlot.Shield, 175, 1, 0, 12, 1);
    public static Item CoralRing = CreateExpansionEquipment("Coral Ring", EquipmentSlot.Ring, 190, 4, 3, 3);
    public static Item AshenMageRobes = CreateExpansionEquipment("Ashen Mage Robes", EquipmentSlot.Body, 220, 5, 6, 8, 1);
    public static Item IronbackGreaves = CreateExpansionEquipment("Ironback Greaves", EquipmentSlot.Legs, 240, 1, 3, 14, 1);
    public static Item MoonveilAmulet = CreateExpansionEquipment("Moonveil Amulet", EquipmentSlot.Amulet, 275, 7, 7, 5);
    public static Item DuneplateHelm = CreateExpansionEquipment("Duneplate Helm", EquipmentSlot.Head, 300, 3, 4, 15);
    public static Item GrovekeeperGloves = CreateExpansionEquipment("Grovekeeper Gloves", EquipmentSlot.Gloves, 325, 5, 5, 8);

    public static Item AbyssalTongueWhip = CreateExpansionEquipment("Abyssal Tongue Whip", EquipmentSlot.Weapon, 450, 16, 14, 0, attackSpeedTicks: 3);
    public static Item BloodmoonHood = CreateExpansionEquipment("Bloodmoon Hood", EquipmentSlot.Head, 500, 8, 10, 16, 2);
    public static Item IronfangClaws = CreateExpansionEquipment("Ironfang Claws", EquipmentSlot.Gloves, 550, 12, 16, 8);
    public static Item ManticoreTailSpear = CreateExpansionEquipment("Manticore-Tail Spear", EquipmentSlot.Weapon, 625, 20, 18, 0);
    public static Item BonecollectorBoots = CreateExpansionEquipment("Bonecollector Boots", EquipmentSlot.Boots, 675, 7, 8, 18, 2);
    public static Item StormscaleShield = CreateExpansionEquipment("Stormscale Shield", EquipmentSlot.Shield, 750, 5, 5, 25, 3);
    public static Item ObsidianPlatebody = CreateExpansionEquipment("Obsidian Platebody", EquipmentSlot.Body, 850, 4, 8, 35, 4);
    public static Item DuneAssassinsDirk = CreateExpansionEquipment("Dune Assassin's Dirk", EquipmentSlot.Weapon, 950, 26, 22, 0, attackSpeedTicks: 3);
    public static Item PlagueMask = CreateExpansionEquipment("Plague Mask", EquipmentSlot.Head, 1050, 12, 12, 24, 3);
    public static Item TidecallerLegguards = CreateExpansionEquipment("Tidecaller Legguards", EquipmentSlot.Legs, 1150, 10, 14, 30, 4);

    public static Item RunicGauntlets = CreateExpansionEquipment("Runic Gauntlets", EquipmentSlot.Gloves, 1350, 16, 18, 22);
    public static Item HeartwoodBulwark = CreateExpansionEquipment("Heartwood Bulwark", EquipmentSlot.Shield, 1550, 8, 8, 45, 6);
    public static Item DreadwingBoots = CreateExpansionEquipment("Dreadwing Boots", EquipmentSlot.Boots, 1750, 20, 20, 20, 2);
    public static Item VoidtongueStaff = CreateExpansionEquipment("Voidtongue Staff", EquipmentSlot.Weapon, 2100, 38, 32, 5, 2);
    public static Item FrostfangNecklace = CreateExpansionEquipment("Frostfang Necklace", EquipmentSlot.Amulet, 2400, 24, 30, 22, 3);
    public static Item CrystalGazeRing = CreateExpansionEquipment("Crystal Gaze Ring", EquipmentSlot.Ring, 2800, 30, 28, 28, 3);
    public static Item InfernalPlatelegs = CreateExpansionEquipment("Infernal Platelegs", EquipmentSlot.Legs, 3400, 22, 38, 50, 5);
    public static Item EclipseMantle = CreateExpansionEquipment("Eclipse Mantle", EquipmentSlot.Body, 4100, 32, 35, 55, 6);
    public static Item ColossusHelm = CreateExpansionEquipment("Colossus Helm", EquipmentSlot.Head, 4800, 20, 30, 65, 7);
    public static Item VortexBlade = CreateExpansionEquipment("Vortex Blade", EquipmentSlot.Weapon, 5600, 55, 58, 10, attackSpeedTicks: 3);

    public static Item FallenChampionsAegis = CreateExpansionEquipment("Fallen Champion's Aegis", EquipmentSlot.Shield, 6800, 28, 28, 85, 10);
    public static Item HollowEmperorCrown = CreateExpansionEquipment("Hollow Emperor Crown", EquipmentSlot.Head, 8000, 45, 50, 70, 10);
    public static Item StarvedGodsGrasp = CreateExpansionEquipment("Starved God's Grasp", EquipmentSlot.Gloves, 9500, 55, 65, 45, 6);
    public static Item MalakarsRavagerGreaves = CreateExpansionEquipment("Malakar's Ravager Greaves", EquipmentSlot.Legs, 11500, 50, 70, 90, 12);
    public static Item RiftwalkerBoots = CreateExpansionEquipment("Riftwalker Boots", EquipmentSlot.Boots, 13500, 60, 60, 60, 8);
    public static Item LeviathanScaleArmor = CreateExpansionEquipment("Leviathan Scale Armor", EquipmentSlot.Body, 16000, 45, 55, 125, 15);
    public static Item TitanbreakerMaul = CreateExpansionEquipment("Titanbreaker Maul", EquipmentSlot.Weapon, 22000, 95, 135, 20, attackSpeedTicks: 5);
    public static Item AstralSignet = CreateExpansionEquipment("Astral Signet", EquipmentSlot.Ring, 26000, 85, 90, 80, 12);
    public static Item AbyssalSovereignAmulet = CreateExpansionEquipment("Abyssal Sovereign Amulet", EquipmentSlot.Amulet, 32000, 100, 110, 90, 15);
    public static Item CrownlessRegalia = CreateExpansionEquipment("Crownless Regalia", EquipmentSlot.Body, 50000, 105, 125, 160, 20);

    public static IReadOnlyList<Item> GetEquipmentExpansionItems() =>
        new Item[]
        {
            FeatheredCap, RoosterSpurBoots, MosswoodBuckler, RatboneRing, ImpishGloves,
            ShellplateVest, SilkweaveLeggings, ScoutsShortblade, CinderfangAmulet, TrollhideBoots,
            GloomwingCowl, HexwoodWand, BriarbarkBody, GraveguardShield, CoralRing,
            AshenMageRobes, IronbackGreaves, MoonveilAmulet, DuneplateHelm, GrovekeeperGloves,
            AbyssalTongueWhip, BloodmoonHood, IronfangClaws, ManticoreTailSpear, BonecollectorBoots,
            StormscaleShield, ObsidianPlatebody, DuneAssassinsDirk, PlagueMask, TidecallerLegguards,
            RunicGauntlets, HeartwoodBulwark, DreadwingBoots, VoidtongueStaff, FrostfangNecklace,
            CrystalGazeRing, InfernalPlatelegs, EclipseMantle, ColossusHelm, VortexBlade,
            FallenChampionsAegis, HollowEmperorCrown, StarvedGodsGrasp, MalakarsRavagerGreaves, RiftwalkerBoots,
            LeviathanScaleArmor, TitanbreakerMaul, AstralSignet, AbyssalSovereignAmulet, CrownlessRegalia
        };
}
