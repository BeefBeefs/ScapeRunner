namespace OSRSIdle;

public static partial class ItemData
{
    // ============================================================
    // BASIC ITEMS / CURRENCY
    // ============================================================

    public static Item Coins = new Item
    {
        Name = "Coins",
        Icon = "🪙",
        Value = 1,
        Type = ItemType.Currency
    };

    public static Item Bones = new Item
    {
        Name = "Bones",
        Icon = "🦴",
        Value = 1,
        Type = ItemType.Material
    };

    public static Item Feathers = new Item
    {
        Name = "Feathers",
        Icon = "🪶",
        Value = 2,
        Type = ItemType.Material
    };

    public static Item AnimalHide = new Item
    {
        Name = "Animal Hide",
        Icon = "🟫",
        Value = 8,
        Type = ItemType.Material
    };

    public static Item WolfPelt = new Item
    {
        Name = "Wolf Pelt",
        Icon = "🐺",
        Value = 20,
        Type = ItemType.Material
    };

    public static Item SpiderSilk = new Item
    {
        Name = "Spider Silk",
        Icon = "🕸️",
        Value = 15,
        Type = ItemType.Material
    };

    public static Item GoblinTooth = new Item
    {
        Name = "Goblin Tooth",
        Icon = "🦷",
        Value = 5,
        Type = ItemType.Material
    };

    public static Item MonsterClaw = new Item
    {
        Name = "Monster Claw",
        Icon = "🐾",
        Value = 15,
        Type = ItemType.Material
    };

    public static Item MonsterFang = new Item
    {
        Name = "Monster Fang",
        Icon = "🦷",
        Value = 20,
        Type = ItemType.Material
    };


    // ============================================================
    // SKILLING RESOURCES
    // ============================================================

    public static Item RawShrimp = new Item { Name = "Raw Shrimp", Icon = "🦐", Value = 3, Type = ItemType.Food, HealingAmount = 1 };
    public static Item RawTrout = new Item { Name = "Raw Trout", Icon = "🐟", Value = 12, Type = ItemType.Food, HealingAmount = 2 };
    public static Item RawSalmon = new Item { Name = "Raw Salmon", Icon = "🐟", Value = 18, Type = ItemType.Food, HealingAmount = 2 };
    public static Item RawLobster = new Item { Name = "Raw Lobster", Icon = "🦞", Value = 35, Type = ItemType.Food, HealingAmount = 3 };
    public static Item RawSwordfish = new Item { Name = "Raw Swordfish", Icon = "🐟", Value = 50, Type = ItemType.Food, HealingAmount = 3 };
    public static Item RawShark = new Item { Name = "Raw Shark", Icon = "🦈", Value = 120, Type = ItemType.Food, HealingAmount = 4 };

    public static Item CopperOre = new Item { Name = "Copper Ore", Icon = "🟤", Value = 5, Type = ItemType.Material };
    public static Item TinOre = new Item { Name = "Tin Ore", Icon = "🟤", Value = 5, Type = ItemType.Material };
    public static Item CoalOre = new Item { Name = "Coal", Icon = "⚫", Value = 18, Type = ItemType.Material };
    public static Item MithrilOre = new Item { Name = "Mithril Ore", Icon = "🔷", Value = 65, Type = ItemType.Material };
    public static Item AdamantiteOre = new Item { Name = "Adamantite Ore", Icon = "💚", Value = 120, Type = ItemType.Material };
    public static Item RuniteOre = new Item { Name = "Runite Ore", Icon = "🔵", Value = 300, Type = ItemType.Material };

    public static Item Logs = new Item { Name = "Logs", Icon = "🪵", Value = 4, Type = ItemType.Material };
    public static Item OakLogs = new Item { Name = "Oak Logs", Icon = "🪵", Value = 10, Type = ItemType.Material };
    public static Item WillowLogs = new Item { Name = "Willow Logs", Icon = "🪵", Value = 25, Type = ItemType.Material };
    public static Item YewLogs = new Item { Name = "Yew Logs", Icon = "🪵", Value = 90, Type = ItemType.Material };
    public static Item MagicLogs = new Item { Name = "Magic Logs", Icon = "🪵", Value = 180, Type = ItemType.Material };

    public static Item Silk = new Item { Name = "Silk", Icon = "🧵", Value = 40, Type = ItemType.Material };
    public static Item JeweledRelic = new Item { Name = "Jeweled Relic", Icon = "💎", Value = 350, Type = ItemType.Material };

    public static Item Clay = new Item { Name = "Clay", Icon = "🟫", Value = 8, Type = ItemType.Material };
    public static Item SoftLeatherGloves = new Item { Name = "Crafted Leather Gloves", Icon = "🧤", Value = 45, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Gloves, DefenseBonus = 2 };
    public static Item SapphireAmulet = new Item { Name = "Sapphire Amulet", Icon = "📿", Value = 300, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Amulet, AttackBonus = 4, DefenseBonus = 3 };
    public static Item DragonhideBody = new Item { Name = "Dragonhide Body", Icon = "👕", Value = 1800, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Body, DefenseBonus = 22, HPBonus = 3 };

    public static Item ArrowShafts = new Item { Name = "Arrow Shafts", Icon = "🪵", Value = 4, Type = ItemType.Material };
    public static Item HeadlessArrows = new Item { Name = "Headless Arrows", Icon = "🏹", Value = 12, Type = ItemType.Material };
    public static Item WillowShortbow = new Item { Name = "Willow Shortbow", Icon = "🏹", Value = 140, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Weapon, AttackBonus = 10, StrengthBonus = 7, AttackSpeedTicks = 3 };
    public static Item MagicLongbow = new Item { Name = "Magic Longbow", Icon = "🏹", Value = 1200, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Weapon, AttackBonus = 26, StrengthBonus = 20, AttackSpeedTicks = 4 };

    public static Item Potato = new Item { Name = "Potato", Icon = "🥔", Value = 5, Type = ItemType.Food, HealingAmount = 3 };
    public static Item GrimyHerb = new Item { Name = "Grimy Herb", Icon = "🌿", Value = 35, Type = ItemType.Food, HealingAmount = 2 };
    public static Item Watermelon = new Item { Name = "Watermelon", Icon = "🍉", Value = 90, Type = ItemType.Food, HealingAmount = 10 };
    public static Item MagicSapling = new Item { Name = "Magic Sapling", Icon = "🌱", Value = 450, Type = ItemType.Material };


    // ============================================================
    // SKILLING PETS
    // ============================================================

    public static Item Heron = new Item { Name = "Heron", Icon = "🐦", Value = 0, Type = ItemType.Pet };
    public static Item Rocky = new Item { Name = "Rocky", Icon = "🪨", Value = 0, Type = ItemType.Pet };
    public static Item Beaver = new Item { Name = "Beaver", Icon = "🦫", Value = 0, Type = ItemType.Pet };
    public static Item Squirrel = new Item { Name = "Squirrel", Icon = "🐿️", Value = 0, Type = ItemType.Pet };
    public static Item Raccoon = new Item { Name = "Raccoon", Icon = "🦝", Value = 0, Type = ItemType.Pet };
    public static Item Golem = new Item { Name = "Golem", Icon = "🗿", Value = 0, Type = ItemType.Pet };
    public static Item ArrowEagle = new Item { Name = "Arrow Eagle", Icon = "🦅", Value = 0, Type = ItemType.Pet };
    public static Item Tangleroot = new Item { Name = "Tangleroot", Icon = "🌱", Value = 0, Type = ItemType.Pet };


    // ============================================================
    // CRAFTING / SPECIAL MATERIALS
    // ============================================================

    public static Item IronOre = new Item
    {
        Name = "Iron Ore",
        Icon = "⛏️",
        Value = 20,
        Type = ItemType.Material
    };

    public static Item SteelBar = new Item
    {
        Name = "Steel Bar",
        Icon = "🔩",
        Value = 50,
        Type = ItemType.Material
    };

    public static Item MithrilBar = new Item
    {
        Name = "Mithril Bar",
        Icon = "🔷",
        Value = 150,
        Type = ItemType.Material
    };

    public static Item AdamantiteBar = new Item
    {
        Name = "Adamantite Bar",
        Icon = "💚",
        Value = 400,
        Type = ItemType.Material
    };

    public static Item RunicBar = new Item
    {
        Name = "Runic Bar",
        Icon = "🔵",
        Value = 1000,
        Type = ItemType.Material
    };

    public static Item DragonScale = new Item
    {
        Name = "Dragon Scale",
        Icon = "🔷",
        Value = 500,
        Type = ItemType.Material
    };

    public static Item DragonClaw = new Item
    {
        Name = "Dragon Claw",
        Icon = "🐉",
        Value = 750,
        Type = ItemType.Material
    };

    public static Item AncientDragonScale = new Item
    {
        Name = "Ancient Dragon Scale",
        Icon = "🔮",
        Value = 2500,
        Type = ItemType.Material
    };

    public static Item VoidEssence = new Item
    {
        Name = "Void Essence",
        Icon = "🌀",
        Value = 500,
        Type = ItemType.Material
    };

    public static Item BloodmoonShard = new Item
    {
        Name = "Bloodmoon Shard",
        Icon = "🌙",
        Value = 750,
        Type = ItemType.Material
    };

    public static Item CelestialShard = new Item
    {
        Name = "Celestial Shard",
        Icon = "✨",
        Value = 5000,
        Type = ItemType.Material
    };

    public static Item GodFragment = new Item
    {
        Name = "God Fragment",
        Icon = "💠",
        Value = 10000,
        Type = ItemType.Material
    };


    // ============================================================
    // WEAPONS - BASIC
    // ============================================================

    public static Item BronzeSword = new Item
    {
        Name = "Bronze Sword",
        Icon = "🗡️",
        Value = 15,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 2,
        StrengthBonus = 1,
        AttackSpeedTicks = 4
    };

    public static Item IronSword = new Item
    {
        Name = "Iron Sword",
        Icon = "🗡️",
        Value = 50,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 4,
        StrengthBonus = 3,
        AttackSpeedTicks = 4
    };

    public static Item SteelSword = new Item
    {
        Name = "Steel Sword",
        Icon = "⚔️",
        Value = 125,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 7,
        StrengthBonus = 6,
        AttackSpeedTicks = 4
    };

    public static Item MithrilSword = new Item
    {
        Name = "Mithril Sword",
        Icon = "⚔️",
        Value = 300,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 11,
        StrengthBonus = 9,
        AttackSpeedTicks = 4
    };

    public static Item AdamantSword = new Item
    {
        Name = "Adamant Sword",
        Icon = "⚔️",
        Value = 750,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 16,
        StrengthBonus = 13,
        AttackSpeedTicks = 4
    };

    public static Item RuneSword = new Item
    {
        Name = "Rune Sword",
        Icon = "🗡️",
        Value = 1500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 22,
        StrengthBonus = 18,
        AttackSpeedTicks = 4
    };


    // ============================================================
    // WEAPONS - SPECIAL
    // ============================================================

    public static Item GoblinBlade = new Item
    {
        Name = "Goblin Blade",
        Icon = "🗡️",
        Value = 100,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 5,
        StrengthBonus = 3,
        AttackSpeedTicks = 4
    };

    public static Item GoblinKingsBlade = new Item
    {
        Name = "Goblin King's Blade",
        Icon = "⚔️",
        Value = 2500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 18,
        StrengthBonus = 25,
        AttackSpeedTicks = 5
    };

    public static Item BoneSword = new Item
    {
        Name = "Bone Sword",
        Icon = "🦴",
        Value = 300,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 10,
        StrengthBonus = 12,
        AttackSpeedTicks = 5
    };

    public static Item Graveblade = new Item
    {
        Name = "Graveblade",
        Icon = "☠️",
        Value = 2500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 28,
        StrengthBonus = 25,
        HPBonus = 5,
        AttackSpeedTicks = 5
    };

    public static Item Bloodfang = new Item
    {
        Name = "Bloodfang",
        Icon = "🩸",
        Value = 5000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 35,
        StrengthBonus = 40,
        HPBonus = 10,
        AttackSpeedTicks = 4
    };

    public static Item Moonfang = new Item
    {
        Name = "Moonfang",
        Icon = "🌙",
        Value = 10000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 45,
        StrengthBonus = 55,
        HPBonus = 10,
        AttackSpeedTicks = 4
    };

    public static Item VoidReaper = new Item
    {
        Name = "Void Reaper",
        Icon = "☠️",
        Value = 25000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 70,
        StrengthBonus = 85,
        DefenseBonus = -10,
        AttackSpeedTicks = 5
    };

    public static Item Dragonbane = new Item
    {
        Name = "Dragonbane",
        Icon = "🐉",
        Value = 40000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 80,
        StrengthBonus = 95,
        AttackSpeedTicks = 5
    };

    public static Item Worldbreaker = new Item
    {
        Name = "Worldbreaker",
        Icon = "🌌",
        Value = 250000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 150,
        StrengthBonus = 200,
        HPBonus = 25,
        AttackSpeedTicks = 6
    };


    // ============================================================
    // WEAPONS - UNIQUE / MEGA RARE
    // ============================================================

    public static Item GoblinGodblade = new Item
    {
        Name = "Goblin Godblade",
        Icon = "⚔️",
        Value = 50000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 55,
        StrengthBonus = 70,
        AttackSpeedTicks = 4
    };

    public static Item DeathsScythe = new Item
    {
        Name = "Death's Scythe",
        Icon = "☠️",
        Value = 100000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 100,
        StrengthBonus = 125,
        HPBonus = 15,
        AttackSpeedTicks = 6
    };

    public static Item BloodmoonExecutioner = new Item
    {
        Name = "Bloodmoon Executioner",
        Icon = "🌙",
        Value = 150000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 125,
        StrengthBonus = 150,
        HPBonus = 25,
        AttackSpeedTicks = 6
    };

    public static Item VoidGodslayer = new Item
    {
        Name = "Void Godslayer",
        Icon = "🌀",
        Value = 300000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 175,
        StrengthBonus = 200,
        DefenseBonus = 25,
        HPBonus = 30,
        AttackSpeedTicks = 5
    };

    public static Item EyeOfEternity = new Item
    {
        Name = "Eye of Eternity",
        Icon = "👁️",
        Value = 1000000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Weapon,
        AttackBonus = 250,
        StrengthBonus = 300,
        DefenseBonus = 100,
        HPBonus = 100,
        AttackSpeedTicks = 4
    };


    // ============================================================
    // SHIELDS
    // ============================================================

    public static Item WoodenShield = new Item
    {
        Name = "Wooden Shield",
        Icon = "🛡️",
        Value = 20,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Shield,
        DefenseBonus = 3
    };

    public static Item BronzeShield = new Item
    {
        Name = "Bronze Shield",
        Icon = "🛡️",
        Value = 40,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Shield,
        DefenseBonus = 5
    };

    public static Item IronShield = new Item
    {
        Name = "Iron Shield",
        Icon = "🛡️",
        Value = 100,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Shield,
        DefenseBonus = 9
    };

    public static Item SteelShield = new Item
    {
        Name = "Steel Shield",
        Icon = "🛡️",
        Value = 250,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Shield,
        DefenseBonus = 14,
        HPBonus = 2
    };

    public static Item RuneShield = new Item
    {
        Name = "Rune Shield",
        Icon = "🛡️",
        Value = 1500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Shield,
        DefenseBonus = 28,
        HPBonus = 5
    };

    public static Item BoneShield = new Item
    {
        Name = "Bone Shield",
        Icon = "💀",
        Value = 2500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Shield,
        DefenseBonus = 35,
        HPBonus = 8
    };

    public static Item DragonfireShield = new Item
    {
        Name = "Dragonfire Shield",
        Icon = "🐉",
        Value = 25000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Shield,
        DefenseBonus = 65,
        HPBonus = 15
    };

    public static Item VoidAegis = new Item
    {
        Name = "Void Aegis",
        Icon = "🌀",
        Value = 100000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Shield,
        DefenseBonus = 100,
        HPBonus = 30
    };


    // ============================================================
    // HEAD EQUIPMENT
    // ============================================================

    public static Item BronzeHelm = new Item
    {
        Name = "Bronze Helm",
        Icon = "🪖",
        Value = 25,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        DefenseBonus = 2
    };

    public static Item IronHelm = new Item
    {
        Name = "Iron Helm",
        Icon = "🪖",
        Value = 75,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        DefenseBonus = 5
    };

    public static Item SteelHelm = new Item
    {
        Name = "Steel Helm",
        Icon = "🪖",
        Value = 175,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        DefenseBonus = 8
    };

    public static Item RuneHelm = new Item
    {
        Name = "Rune Helm",
        Icon = "🪖",
        Value = 1000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        DefenseBonus = 18,
        HPBonus = 3
    };

    public static Item GoblinCrown = new Item
    {
        Name = "Goblin Crown",
        Icon = "👑",
        Value = 250,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        AttackBonus = 2,
        StrengthBonus = 2,
        DefenseBonus = 4
    };

    public static Item WolfHelm = new Item
    {
        Name = "Wolf Helm",
        Icon = "🐺",
        Value = 2500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        AttackBonus = 8,
        StrengthBonus = 10,
        DefenseBonus = 8,
        HPBonus = 5
    };

    public static Item BloodmoonCrown = new Item
    {
        Name = "Bloodmoon Crown",
        Icon = "🌙",
        Value = 25000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        AttackBonus = 20,
        StrengthBonus = 25,
        DefenseBonus = 20,
        HPBonus = 15
    };

    public static Item VoidCrown = new Item
    {
        Name = "Void Crown",
        Icon = "🌀",
        Value = 75000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        AttackBonus = 35,
        StrengthBonus = 40,
        DefenseBonus = 35,
        HPBonus = 30
    };

    public static Item CelestialCrown = new Item
    {
        Name = "Celestial Crown",
        Icon = "✨",
        Value = 250000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        AttackBonus = 55,
        StrengthBonus = 60,
        DefenseBonus = 60,
        HPBonus = 50
    };


    // ============================================================
    // BODY ARMOR
    // ============================================================

    public static Item BronzePlatebody = new Item
    {
        Name = "Bronze Platebody",
        Icon = "🛡️",
        Value = 50,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        DefenseBonus = 5
    };

    public static Item IronPlatebody = new Item
    {
        Name = "Iron Platebody",
        Icon = "🛡️",
        Value = 150,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        DefenseBonus = 10
    };

    public static Item SteelPlatebody = new Item
    {
        Name = "Steel Platebody",
        Icon = "🛡️",
        Value = 400,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        DefenseBonus = 17,
        HPBonus = 2
    };

    public static Item RunePlatebody = new Item
    {
        Name = "Rune Platebody",
        Icon = "🛡️",
        Value = 2500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        DefenseBonus = 35,
        HPBonus = 5
    };

    public static Item BonePlatebody = new Item
    {
        Name = "Bone Platebody",
        Icon = "💀",
        Value = 5000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        DefenseBonus = 45,
        HPBonus = 12
    };

    public static Item WerewolfChestplate = new Item
    {
        Name = "Werewolf Chestplate",
        Icon = "🐺",
        Value = 7500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        AttackBonus = 5,
        StrengthBonus = 12,
        DefenseBonus = 30,
        HPBonus = 10
    };

    public static Item Bloodplate = new Item
    {
        Name = "Bloodplate",
        Icon = "🩸",
        Value = 35000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        AttackBonus = 12,
        StrengthBonus = 25,
        DefenseBonus = 65,
        HPBonus = 20
    };

    public static Item VoidBody = new Item
    {
        Name = "Void Body",
        Icon = "🌀",
        Value = 100000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        AttackBonus = 20,
        StrengthBonus = 35,
        DefenseBonus = 90,
        HPBonus = 35
    };

    public static Item WorldEaterPlate = new Item
    {
        Name = "World-Eater Plate",
        Icon = "🐉",
        Value = 300000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        AttackBonus = 35,
        StrengthBonus = 60,
        DefenseBonus = 150,
        HPBonus = 75
    };


    // ============================================================
    // LEG ARMOR
    // ============================================================

    public static Item BronzePlatelegs = new Item
    {
        Name = "Bronze Platelegs",
        Icon = "🦵",
        Value = 40,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Legs,
        DefenseBonus = 4
    };

    public static Item IronPlatelegs = new Item
    {
        Name = "Iron Platelegs",
        Icon = "🦵",
        Value = 120,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Legs,
        DefenseBonus = 8
    };

    public static Item SteelPlatelegs = new Item
    {
        Name = "Steel Platelegs",
        Icon = "🦵",
        Value = 300,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Legs,
        DefenseBonus = 14,
        HPBonus = 2
    };

    public static Item RunePlatelegs = new Item
    {
        Name = "Rune Platelegs",
        Icon = "🦵",
        Value = 2000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Legs,
        DefenseBonus = 30,
        HPBonus = 5
    };

    public static Item BonePlatelegs = new Item
    {
        Name = "Bone Platelegs",
        Icon = "💀",
        Value = 4000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Legs,
        DefenseBonus = 38,
        HPBonus = 10
    };

    public static Item BloodplateLegs = new Item
    {
        Name = "Bloodplate Legs",
        Icon = "🩸",
        Value = 30000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Legs,
        AttackBonus = 8,
        StrengthBonus = 15,
        DefenseBonus = 55,
        HPBonus = 18
    };

    public static Item VoidLegs = new Item
    {
        Name = "Void Legs",
        Icon = "🌀",
        Value = 85000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Legs,
        AttackBonus = 15,
        StrengthBonus = 25,
        DefenseBonus = 75,
        HPBonus = 30
    };

    public static Item WorldEaterGreaves = new Item
    {
        Name = "World-Eater Greaves",
        Icon = "🐉",
        Value = 250000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Legs,
        AttackBonus = 25,
        StrengthBonus = 45,
        DefenseBonus = 125,
        HPBonus = 60
    };


    // ============================================================
    // GLOVES
    // ============================================================

    public static Item LeatherGloves = new Item
    {
        Name = "Leather Gloves",
        Icon = "🧤",
        Value = 15,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Gloves,
        DefenseBonus = 1
    };

    public static Item IronGauntlets = new Item
    {
        Name = "Iron Gauntlets",
        Icon = "🧤",
        Value = 100,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Gloves,
        AttackBonus = 1,
        StrengthBonus = 2,
        DefenseBonus = 4
    };

    public static Item SteelGauntlets = new Item
    {
        Name = "Steel Gauntlets",
        Icon = "🧤",
        Value = 250,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Gloves,
        AttackBonus = 2,
        StrengthBonus = 4,
        DefenseBonus = 7
    };

    public static Item RuneGauntlets = new Item
    {
        Name = "Rune Gauntlets",
        Icon = "🧤",
        Value = 1500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Gloves,
        AttackBonus = 5,
        StrengthBonus = 8,
        DefenseBonus = 15
    };

    public static Item WerewolfClaws = new Item
    {
        Name = "Werewolf Claws",
        Icon = "🐺",
        Value = 7500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Gloves,
        AttackBonus = 12,
        StrengthBonus = 20,
        DefenseBonus = 3,
        HPBonus = 5
    };

    public static Item BloodfangGauntlets = new Item
    {
        Name = "Bloodfang Gauntlets",
        Icon = "🩸",
        Value = 35000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Gloves,
        AttackBonus = 20,
        StrengthBonus = 35,
        DefenseBonus = 15,
        HPBonus = 8
    };

    public static Item VoidClaws = new Item
    {
        Name = "Void Claws",
        Icon = "🌀",
        Value = 100000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Gloves,
        AttackBonus = 35,
        StrengthBonus = 55,
        DefenseBonus = 10,
        HPBonus = 15
    };


    // ============================================================
    // BOOTS
    // ============================================================

    public static Item LeatherBoots = new Item
    {
        Name = "Leather Boots",
        Icon = "👢",
        Value = 15,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Boots,
        DefenseBonus = 1
    };

    public static Item IronBoots = new Item
    {
        Name = "Iron Boots",
        Icon = "👢",
        Value = 100,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Boots,
        DefenseBonus = 4
    };

    public static Item SteelBoots = new Item
    {
        Name = "Steel Boots",
        Icon = "👢",
        Value = 250,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Boots,
        DefenseBonus = 7
    };

    public static Item RuneBoots = new Item
    {
        Name = "Rune Boots",
        Icon = "👢",
        Value = 1500,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Boots,
        DefenseBonus = 15,
        HPBonus = 2
    };

    public static Item WolfPeltBoots = new Item
    {
        Name = "Wolf Pelt Boots",
        Icon = "🐺",
        Value = 5000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Boots,
        AttackBonus = 4,
        StrengthBonus = 8,
        DefenseBonus = 10,
        HPBonus = 4
    };

    public static Item BloodstainedBoots = new Item
    {
        Name = "Bloodstained Boots",
        Icon = "🩸",
        Value = 25000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Boots,
        AttackBonus = 10,
        StrengthBonus = 18,
        DefenseBonus = 20,
        HPBonus = 8
    };

    public static Item Voidwalkers = new Item
    {
        Name = "Voidwalkers",
        Icon = "🌀",
        Value = 75000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Boots,
        AttackBonus = 20,
        StrengthBonus = 30,
        DefenseBonus = 25,
        HPBonus = 15
    };


    // ============================================================
    // AMULETS
    // ============================================================

    public static Item AmuletOfAccuracy = new Item
    {
        Name = "Amulet of Accuracy",
        Icon = "📿",
        Value = 100,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Amulet,
        AttackBonus = 8
    };

    public static Item AmuletOfStrength = new Item
    {
        Name = "Amulet of Strength",
        Icon = "📿",
        Value = 150,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Amulet,
        StrengthBonus = 10
    };

    public static Item AmuletOfDefense = new Item
    {
        Name = "Amulet of Defense",
        Icon = "📿",
        Value = 150,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Amulet,
        DefenseBonus = 10
    };

    public static Item BloodAmulet = new Item
    {
        Name = "Blood Amulet",
        Icon = "🩸",
        Value = 5000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Amulet,
        AttackBonus = 12,
        StrengthBonus = 15,
        DefenseBonus = 8,
        HPBonus = 10
    };

    public static Item MoonstoneAmulet = new Item
    {
        Name = "Moonstone Amulet",
        Icon = "🌙",
        Value = 15000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Amulet,
        AttackBonus = 20,
        StrengthBonus = 25,
        DefenseBonus = 15,
        HPBonus = 15
    };

    public static Item VoidAmulet = new Item
    {
        Name = "Void Amulet",
        Icon = "🌀",
        Value = 50000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Amulet,
        AttackBonus = 35,
        StrengthBonus = 40,
        DefenseBonus = 30,
        HPBonus = 25
    };

    public static Item DivineAmulet = new Item
    {
        Name = "Divine Amulet",
        Icon = "✨",
        Value = 150000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Amulet,
        AttackBonus = 60,
        StrengthBonus = 65,
        DefenseBonus = 55,
        HPBonus = 50
    };


    // ============================================================
    // RINGS
    // ============================================================

    public static Item RingOfAttack = new Item
    {
        Name = "Ring of Attack",
        Icon = "💍",
        Value = 100,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Ring,
        AttackBonus = 5
    };

    public static Item RingOfStrength = new Item
    {
        Name = "Ring of Strength",
        Icon = "💍",
        Value = 150,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Ring,
        StrengthBonus = 7
    };

    public static Item RingOfDefense = new Item
    {
        Name = "Ring of Defense",
        Icon = "💍",
        Value = 150,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Ring,
        DefenseBonus = 7
    };

    public static Item RingOfVitality = new Item
    {
        Name = "Ring of Vitality",
        Icon = "💍",
        Value = 250,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Ring,
        HPBonus = 10
    };

    public static Item BloodmoonRing = new Item
    {
        Name = "Bloodmoon Ring",
        Icon = "🌙",
        Value = 10000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Ring,
        AttackBonus = 10,
        StrengthBonus = 15,
        HPBonus = 10
    };

    public static Item BerserkersRing = new Item
    {
        Name = "Berserker's Ring",
        Icon = "💍",
        Value = 15000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Ring,
        AttackBonus = 8,
        StrengthBonus = 25
    };

    public static Item GuardiansRing = new Item
    {
        Name = "Guardian's Ring",
        Icon = "💍",
        Value = 15000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Ring,
        DefenseBonus = 25,
        HPBonus = 15
    };

    public static Item VoidRing = new Item
    {
        Name = "Void Ring",
        Icon = "🌀",
        Value = 75000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Ring,
        AttackBonus = 20,
        StrengthBonus = 30,
        DefenseBonus = 20,
        HPBonus = 20
    };

    public static Item RingOfTheGods = new Item
    {
        Name = "Ring of the Gods",
        Icon = "💠",
        Value = 250000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Ring,
        AttackBonus = 35,
        StrengthBonus = 40,
        DefenseBonus = 40,
        HPBonus = 35
    };


    // ============================================================
    // SPECIAL / UNIQUE EQUIPMENT
    // ============================================================

    public static Item PhoenixCloak = new Item
    {
        Name = "Phoenix Cloak",
        Icon = "🔥",
        Value = 75000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        AttackBonus = 15,
        StrengthBonus = 15,
        DefenseBonus = 35,
        HPBonus = 40
    };

    public static Item DragonscaleArmor = new Item
    {
        Name = "Dragonscale Armor",
        Icon = "🐉",
        Value = 150000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        AttackBonus = 25,
        StrengthBonus = 35,
        DefenseBonus = 90,
        HPBonus = 45
    };

    public static Item AncientDragonHelm = new Item
    {
        Name = "Ancient Dragon Helm",
        Icon = "🐉",
        Value = 100000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        AttackBonus = 25,
        StrengthBonus = 30,
        DefenseBonus = 55,
        HPBonus = 25
    };

    public static Item GodslayerArmor = new Item
    {
        Name = "Godslayer Armor",
        Icon = "✨",
        Value = 500000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Body,
        AttackBonus = 60,
        StrengthBonus = 80,
        DefenseBonus = 125,
        HPBonus = 75
    };

    public static Item StarforgedHelm = new Item
    {
        Name = "Starforged Helm",
        Icon = "⭐",
        Value = 350000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        AttackBonus = 45,
        StrengthBonus = 50,
        DefenseBonus = 80,
        HPBonus = 45
    };

    public static Item WorldEaterCrown = new Item
    {
        Name = "World-Eater Crown",
        Icon = "🐉",
        Value = 750000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Head,
        AttackBonus = 75,
        StrengthBonus = 90,
        DefenseBonus = 100,
        HPBonus = 75
    };

    public static Item FragmentOfCreation = new Item
    {
        Name = "Fragment of Creation",
        Icon = "✨",
        Value = 1000000,
        Type = ItemType.Equipment,
        EquipmentSlot = EquipmentSlot.Amulet,
        AttackBonus = 100,
        StrengthBonus = 100,
        DefenseBonus = 100,
        HPBonus = 100
    };
    // ============================================================
    // ENEMY DROP MATERIALS
    // ============================================================

    public static Item RawChicken = new Item
    {
        Name = "Raw Chicken",
        Icon = "🍗",
        Value = 3,
        Type = ItemType.Food,
        HealingAmount = 1
    };

    public static Item GoblinEar = new Item
    {
        Name = "Goblin Ear",
        Icon = "👂",
        Value = 8,
        Type = ItemType.Material
    };

    public static Item RatTail = new Item
    {
        Name = "Rat Tail",
        Icon = "🐀",
        Value = 6,
        Type = ItemType.Material
    };

    public static Item RatFur = new Item
    {
        Name = "Rat Fur",
        Icon = "🟫",
        Value = 8,
        Type = ItemType.Material
    };

    public static Item BatWing = new Item
    {
        Name = "Bat Wing",
        Icon = "🦇",
        Value = 12,
        Type = ItemType.Food,
        HealingAmount = 3
    };

    public static Item BatFang = new Item
    {
        Name = "Bat Fang",
        Icon = "🦷",
        Value = 20,
        Type = ItemType.Material
    };

    public static Item Eyeball = new Item
    {
        Name = "Eyeball",
        Icon = "👁️",
        Value = 25,
        Type = ItemType.Food,
        HealingAmount = 4
    };

    public static Item MossyMushroom = new Item
    {
        Name = "Mossy Mushroom",
        Icon = "🍄",
        Value = 15,
        Type = ItemType.Food,
        HealingAmount = 5
    };

    public static Item WitchEye = new Item
    {
        Name = "Witch Eye",
        Icon = "👁️",
        Value = 40,
        Type = ItemType.Material
    };

    public static Item WitchBroomBristle = new Item
    {
        Name = "Witch Broom Bristle",
        Icon = "🧹",
        Value = 50,
        Type = ItemType.Material
    };

    public static Item EnchantedLeaf = new Item
    {
        Name = "Enchanted Leaf",
        Icon = "🍃",
        Value = 30,
        Type = ItemType.Food,
        HealingAmount = 3
    };

    public static Item DryadBark = new Item
    {
        Name = "Dryad Bark",
        Icon = "🌳",
        Value = 45,
        Type = ItemType.Material
    };

    public static Item ThornedVine = new Item
    {
        Name = "Thorned Vine",
        Icon = "🌿",
        Value = 55,
        Type = ItemType.Food,
        HealingAmount = 2
    };

    public static Item GraveDust = new Item
    {
        Name = "Grave Dust",
        Icon = "💨",
        Value = 50,
        Type = ItemType.Material
    };

    public static Item RustedSwordFragment = new Item
    {
        Name = "Rusted Sword Fragment",
        Icon = "🗡️",
        Value = 75,
        Type = ItemType.Material
    };

    public static Item CursedBone = new Item
    {
        Name = "Cursed Bone",
        Icon = "💀",
        Value = 100,
        Type = ItemType.Material
    };

    public static Item AbyssalSlime = new Item
    {
        Name = "Abyssal Slime",
        Icon = "🟣",
        Value = 125,
        Type = ItemType.Food,
        HealingAmount = 7
    };

    public static Item CultistRobeScrap = new Item
    {
        Name = "Cultist Robe Scrap",
        Icon = "🧥",
        Value = 150,
        Type = ItemType.Material
    };

    public static Item WerewolfFang = new Item
    {
        Name = "Werewolf Fang",
        Icon = "🦷",
        Value = 175,
        Type = ItemType.Material
    };

    public static Item WerewolfPelt = new Item
    {
        Name = "Werewolf Pelt",
        Icon = "🐺",
        Value = 250,
        Type = ItemType.Material
    };

    public static Item ManticoreSpike = new Item
    {
        Name = "Manticore Spike",
        Icon = "🦂",
        Value = 300,
        Type = ItemType.Material
    };

    public static Item ManticoreHide = new Item
    {
        Name = "Manticore Hide",
        Icon = "🟫",
        Value = 350,
        Type = ItemType.Material
    };

    public static Item AncientBone = new Item
    {
        Name = "Ancient Bone",
        Icon = "🦴",
        Value = 400,
        Type = ItemType.Material
    };

    public static Item TreantHeartwood = new Item
    {
        Name = "Treant Heartwood",
        Icon = "🪵",
        Value = 500,
        Type = ItemType.Material
    };

    public static Item HarpyTalon = new Item
    {
        Name = "Harpy Talon",
        Icon = "🦅",
        Value = 550,
        Type = ItemType.Material
    };

    public static Item HarpyFeather = new Item
    {
        Name = "Harpy Feather",
        Icon = "🪶",
        Value = 250,
        Type = ItemType.Material
    };

    public static Item MoonstoneFragment = new Item
    {
        Name = "Moonstone Fragment",
        Icon = "🌙",
        Value = 750,
        Type = ItemType.Material
    };

    public static Item ShadowSilk = new Item
    {
        Name = "Shadow Silk",
        Icon = "🕸️",
        Value = 800,
        Type = ItemType.Material
    };

    public static Item FrostfangScale = new Item
    {
        Name = "Frostfang Scale",
        Icon = "❄️",
        Value = 1000,
        Type = ItemType.Material
    };

    public static Item WyrmFang = new Item
    {
        Name = "Wyrm Fang",
        Icon = "🦷",
        Value = 1250,
        Type = ItemType.Material
    };

    public static Item WyrmHeart = new Item
    {
        Name = "Wyrm Heart",
        Icon = "❤️",
        Value = 2500,
        Type = ItemType.Food,
        HealingAmount = 20
    };

    public static Item HollowSoul = new Item
    {
        Name = "Hollow Soul",
        Icon = "👻",
        Value = 2000,
        Type = ItemType.Material
    };

    public static Item HollowCrown = new Item
    {
        Name = "Hollow Crown",
        Icon = "👑",
        Value = 5000,
        Type = ItemType.Material
    };

    public static Item Godbone = new Item
    {
        Name = "Godbone",
        Icon = "💠",
        Value = 7500,
        Type = ItemType.Material
    };

    public static Item StarvedGodsEye = new Item
    {
        Name = "Starved God's Eye",
        Icon = "👁️",
        Value = 15000,
        Type = ItemType.Material
    };

    public static Item PhoenixAsh = new Item
    {
        Name = "Phoenix Ash",
        Icon = "🔥",
        Value = 10000,
        Type = ItemType.Material
    };

    public static Item WorldEaterScale = new Item
    {
        Name = "World-Eater Scale",
        Icon = "🐉",
        Value = 5000,
        Type = ItemType.Material
    };

    public static Item WorldEaterFang = new Item
    {
        Name = "World-Eater Fang",
        Icon = "🦷",
        Value = 7500,
        Type = ItemType.Material
    };

    public static Item MalakarsHeart = new Item
    {
        Name = "Malakar's Heart",
        Icon = "❤️",
        Value = 25000,
        Type = ItemType.Material
    };


    // ============================================================
    // RARE / COLLECTION LOG DROPS
    // ============================================================

    public static Item ChickenClaw = new Item
    {
        Name = "Chicken Claw",
        Icon = "🐔",
        Value = 100,
        Type = ItemType.Material
    };

    public static Item GoldenFeather = new Item
    {
        Name = "Golden Feather",
        Icon = "✨",
        Value = 500,
        Type = ItemType.Material
    };

    public static Item ChickenGodFeather = new Item
    {
        Name = "Chicken God Feather",
        Icon = "🌟",
        Value = 5000,
        Type = ItemType.Material
    };

    public static Item GoblinChampionsTooth = new Item
    {
        Name = "Goblin Champion's Tooth",
        Icon = "🦷",
        Value = 300,
        Type = ItemType.Material
    };

    public static Item GoblinCrownShard = new Item
    {
        Name = "Goblin Crown Shard",
        Icon = "👑",
        Value = 1000,
        Type = ItemType.Material
    };

    public static Item GoblinGodTooth = new Item
    {
        Name = "Goblin God Tooth",
        Icon = "💠",
        Value = 10000,
        Type = ItemType.Material
    };

    public static Item RatKingsTail = new Item
    {
        Name = "Rat King's Tail",
        Icon = "🐀",
        Value = 400,
        Type = ItemType.Material
    };

    public static Item PlagueRatFur = new Item
    {
        Name = "Plague Rat Fur",
        Icon = "☣️",
        Value = 1200,
        Type = ItemType.Material
    };

    public static Item VerminousHeart = new Item
    {
        Name = "Verminous Heart",
        Icon = "❤️",
        Value = 7500,
        Type = ItemType.Food,
        HealingAmount = 15
    };

    public static Item VampiricWing = new Item
    {
        Name = "Vampiric Wing",
        Icon = "🦇",
        Value = 500,
        Type = ItemType.Food,
        HealingAmount = 7
    };

    public static Item Bloodwing = new Item
    {
        Name = "Bloodwing",
        Icon = "🩸",
        Value = 1500,
        Type = ItemType.Material
    };

    public static Item AbyssalBatHeart = new Item
    {
        Name = "Abyssal Bat Heart",
        Icon = "❤️",
        Value = 10000,
        Type = ItemType.Food,
        HealingAmount = 18
    };

    public static Item WitchHatFragment = new Item
    {
        Name = "Witch Hat Fragment",
        Icon = "🧙",
        Value = 600,
        Type = ItemType.Material
    };

    public static Item FamiliarCore = new Item
    {
        Name = "Familiar Core",
        Icon = "🔮",
        Value = 2500,
        Type = ItemType.Material
    };

    public static Item SwampHeart = new Item
    {
        Name = "Swamp Heart",
        Icon = "💚",
        Value = 12000,
        Type = ItemType.Food,
        HealingAmount = 22
    };

    public static Item DryadHeart = new Item
    {
        Name = "Dryad Heart",
        Icon = "🌿",
        Value = 1000,
        Type = ItemType.Food,
        HealingAmount = 12
    };

    public static Item AncientSeed = new Item
    {
        Name = "Ancient Seed",
        Icon = "🌱",
        Value = 3000,
        Type = ItemType.Material
    };

    public static Item WorldTreeSeed = new Item
    {
        Name = "World Tree Seed",
        Icon = "🌳",
        Value = 15000,
        Type = ItemType.Material
    };

    public static Item GravebladeFragment = new Item
    {
        Name = "Graveblade Fragment",
        Icon = "☠️",
        Value = 1500,
        Type = ItemType.Material
    };

    public static Item DeathKnightsSigil = new Item
    {
        Name = "Death Knight's Sigil",
        Icon = "💀",
        Value = 5000,
        Type = ItemType.Material
    };

    public static Item SoulboundSkull = new Item
    {
        Name = "Soulbound Skull",
        Icon = "☠️",
        Value = 20000,
        Type = ItemType.Material
    };

    public static Item AbyssalTongue = new Item
    {
        Name = "Abyssal Tongue",
        Icon = "👅",
        Value = 2000,
        Type = ItemType.Food,
        HealingAmount = 12
    };

    public static Item AbyssalEye = new Item
    {
        Name = "Abyssal Eye",
        Icon = "👁️",
        Value = 7500,
        Type = ItemType.Material
    };

    public static Item AbyssalCore = new Item
    {
        Name = "Abyssal Core",
        Icon = "🌀",
        Value = 25000,
        Type = ItemType.Material
    };

    public static Item BloodmoonMaskFragment = new Item
    {
        Name = "Bloodmoon Mask Fragment",
        Icon = "🌙",
        Value = 2500,
        Type = ItemType.Material
    };

    public static Item CultistRelic = new Item
    {
        Name = "Cultist Relic",
        Icon = "💠",
        Value = 10000,
        Type = ItemType.Material
    };

    public static Item BloodmoonEye = new Item
    {
        Name = "Bloodmoon Eye",
        Icon = "🌙",
        Value = 30000,
        Type = ItemType.Material
    };

    public static Item WerewolfHeart = new Item
    {
        Name = "Werewolf Heart",
        Icon = "❤️",
        Value = 3500,
        Type = ItemType.Food,
        HealingAmount = 18
    };

    public static Item AlphaPelt = new Item
    {
        Name = "Alpha Pelt",
        Icon = "🐺",
        Value = 12000,
        Type = ItemType.Material
    };

    public static Item LycanthropeCore = new Item
    {
        Name = "Lycanthrope Core",
        Icon = "🔮",
        Value = 40000,
        Type = ItemType.Material
    };

    public static Item ManticoreEye = new Item
    {
        Name = "Manticore Eye",
        Icon = "👁️",
        Value = 5000,
        Type = ItemType.Material
    };

    public static Item ManticoreHeart = new Item
    {
        Name = "Manticore Heart",
        Icon = "❤️",
        Value = 15000,
        Type = ItemType.Food,
        HealingAmount = 24
    };

    public static Item CrypticCore = new Item
    {
        Name = "Cryptic Core",
        Icon = "🟣",
        Value = 50000,
        Type = ItemType.Material
    };

    public static Item BoneCollectorsMask = new Item
    {
        Name = "Bone Collector's Mask",
        Icon = "💀",
        Value = 7500,
        Type = ItemType.Material
    };

    public static Item AncientSkull = new Item
    {
        Name = "Ancient Skull",
        Icon = "☠️",
        Value = 20000,
        Type = ItemType.Material
    };

    public static Item DeathsEssence = new Item
    {
        Name = "Death's Essence",
        Icon = "🖤",
        Value = 60000,
        Type = ItemType.Material
    };

    public static Item TreantCrown = new Item
    {
        Name = "Treant Crown",
        Icon = "🌳",
        Value = 8000,
        Type = ItemType.Material
    };

    public static Item AncientHeartwood = new Item
    {
        Name = "Ancient Heartwood",
        Icon = "🪵",
        Value = 25000,
        Type = ItemType.Material
    };

    public static Item WorldrootCore = new Item
    {
        Name = "Worldroot Core",
        Icon = "🌱",
        Value = 75000,
        Type = ItemType.Material
    };

    public static Item HarpyCrest = new Item
    {
        Name = "Harpy Crest",
        Icon = "🦅",
        Value = 10000,
        Type = ItemType.Material
    };

    public static Item DreadwingFeather = new Item
    {
        Name = "Dreadwing Feather",
        Icon = "🪶",
        Value = 30000,
        Type = ItemType.Material
    };

    public static Item HarpyHeart = new Item
    {
        Name = "Harpy Heart",
        Icon = "❤️",
        Value = 100000,
        Type = ItemType.Food,
        HealingAmount = 30
    };

    public static Item VoidWarlockEye = new Item
    {
        Name = "Void Warlock Eye",
        Icon = "👁️",
        Value = 15000,
        Type = ItemType.Material
    };

    public static Item VoidTomeFragment = new Item
    {
        Name = "Void Tome Fragment",
        Icon = "📖",
        Value = 40000,
        Type = ItemType.Material
    };

    public static Item VoidWarlockCore = new Item
    {
        Name = "Void Warlock Core",
        Icon = "🌀",
        Value = 125000,
        Type = ItemType.Material
    };

    public static Item FrostfangHeart = new Item
    {
        Name = "Frostfang Heart",
        Icon = "❄️",
        Value = 10000,
        Type = ItemType.Food,
        HealingAmount = 28
    };

    public static Item WyrmCrown = new Item
    {
        Name = "Wyrm Crown",
        Icon = "🐉",
        Value = 40000,
        Type = ItemType.Material
    };

    public static Item AncientWyrmCore = new Item
    {
        Name = "Ancient Wyrm Core",
        Icon = "🔷",
        Value = 150000,
        Type = ItemType.Material
    };

    public static Item HollowKingsSigil = new Item
    {
        Name = "Hollow King's Sigil",
        Icon = "👑",
        Value = 25000,
        Type = ItemType.Material
    };

    public static Item HollowEmber = new Item
    {
        Name = "Hollow Ember",
        Icon = "🔥",
        Value = 75000,
        Type = ItemType.Material
    };

    public static Item EmperorsSoul = new Item
    {
        Name = "Emperor's Soul",
        Icon = "👑",
        Value = 250000,
        Type = ItemType.Material
    };

    public static Item StarvedGodsHeart = new Item
    {
        Name = "Starved God's Heart",
        Icon = "👁️",
        Value = 100000,
        Type = ItemType.Food,
        HealingAmount = 40
    };

    public static Item StarvedGodRelic = new Item
    {
        Name = "Starved God Relic",
        Icon = "💠",
        Value = 500000,
        Type = ItemType.Material
    };

    public static Item FragmentOfDivinity = new Item
    {
        Name = "Fragment of Divinity",
        Icon = "✨",
        Value = 1000000,
        Type = ItemType.Material
    };

    public static Item MalakarsHorn = new Item
    {
        Name = "Malakar's Horn",
        Icon = "🐉",
        Value = 100000,
        Type = ItemType.Material
    };

    public static Item WorldEaterHeart = new Item
    {
        Name = "World-Eater Heart",
        Icon = "❤️",
        Value = 500000,
        Type = ItemType.Food,
        HealingAmount = 60
    };

    public static Item WorldEatersEye = new Item
    {
        Name = "World-Eater's Eye",
        Icon = "👁️",
        Value = 2000000,
        Type = ItemType.Material
    };

    // ============================================================
    // EXPANDED ENEMY DROPS
    // ============================================================

    public static Item BogPearl = new Item { Name = "Bog Pearl", Icon = "🫧", Value = 45, Type = ItemType.Material };
    public static Item EmberCore = new Item { Name = "Ember Core", Icon = "🔥", Value = 120, Type = ItemType.Material };
    public static Item StormScale = new Item { Name = "Storm Scale", Icon = "⚡", Value = 260, Type = ItemType.Material };
    public static Item ObsidianHeart = new Item { Name = "Obsidian Heart", Icon = "🖤", Value = 600, Type = ItemType.Material };
    public static Item CrystalFang = new Item { Name = "Crystal Fang", Icon = "💎", Value = 1400, Type = ItemType.Material };
    public static Item AstralDust = new Item { Name = "Astral Dust", Icon = "✨", Value = 3200, Type = ItemType.Material };

    public static Item SandstalkerDagger = new Item { Name = "Sandstalker Dagger", Icon = "🗡️", Value = 350, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Weapon, AttackBonus = 12, StrengthBonus = 9, AttackSpeedTicks = 3 };
    public static Item TidecallerTrident = new Item { Name = "Tidecaller Trident", Icon = "🔱", Value = 1800, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Weapon, AttackBonus = 28, StrengthBonus = 23, AttackSpeedTicks = 4 };
    public static Item RuneSentinelAegis = new Item { Name = "Rune Sentinel Aegis", Icon = "🛡️", Value = 2500, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Shield, DefenseBonus = 30, HPBonus = 3 };
    public static Item InfernalGreatsword = new Item { Name = "Infernal Greatsword", Icon = "⚔️", Value = 7000, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Weapon, AttackBonus = 48, StrengthBonus = 62, AttackSpeedTicks = 5 };
    public static Item AstralCrown = new Item { Name = "Astral Crown", Icon = "👑", Value = 12000, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Head, AttackBonus = 14, DefenseBonus = 22, HPBonus = 4 };
    public static Item MonarchsMantle = new Item { Name = "Monarch's Mantle", Icon = "🧥", Value = 22000, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Body, DefenseBonus = 48, HPBonus = 9 };
    public static Item CrownlessKingsBlade = new Item { Name = "Crownless King's Blade", Icon = "⚔️", Value = 100000, Type = ItemType.Equipment, EquipmentSlot = EquipmentSlot.Weapon, AttackBonus = 92, StrengthBonus = 120, DefenseBonus = 12, AttackSpeedTicks = 4 };

    // ============================================================
    // ALL ITEMS
    // ============================================================

    private static readonly List<Item> ItemRegistry = new()
    {
        // --------------------------------------------------------
        // Basic / Materials
        // --------------------------------------------------------

        Coins,
        Bones,
        Feathers,
        AnimalHide,
        WolfPelt,
        SpiderSilk,
        GoblinTooth,
        MonsterClaw,
        MonsterFang,

        RawShrimp,
        RawTrout,
        RawSalmon,
        RawLobster,
        RawSwordfish,
        RawShark,

        CopperOre,
        TinOre,
        CoalOre,
        MithrilOre,
        AdamantiteOre,
        RuniteOre,

        Logs,
        OakLogs,
        WillowLogs,
        YewLogs,
        MagicLogs,

        Silk,
        JeweledRelic,
        Clay,
        SoftLeatherGloves,
        SapphireAmulet,
        DragonhideBody,
        ArrowShafts,
        HeadlessArrows,
        WillowShortbow,
        MagicLongbow,
        Potato,
        GrimyHerb,
        Watermelon,
        MagicSapling,

        Heron,
        Rocky,
        Beaver,
        Squirrel,
        Raccoon,
        Golem,
        ArrowEagle,
        Tangleroot,

        IronOre,
        SteelBar,
        MithrilBar,
        AdamantiteBar,
        RunicBar,
        DragonScale,
        DragonClaw,
        AncientDragonScale,
        VoidEssence,
        BloodmoonShard,
        CelestialShard,
        GodFragment,

                // --------------------------------------------------------
        // Enemy Drop Materials
        // --------------------------------------------------------

        RawChicken,
        GoblinEar,
        RatTail,
        RatFur,
        BatWing,
        BatFang,
        Eyeball,
        MossyMushroom,
        WitchEye,
        WitchBroomBristle,
        EnchantedLeaf,
        DryadBark,
        ThornedVine,
        GraveDust,
        RustedSwordFragment,
        CursedBone,
        AbyssalSlime,
        CultistRobeScrap,
        WerewolfFang,
        WerewolfPelt,
        ManticoreSpike,
        ManticoreHide,
        AncientBone,
        TreantHeartwood,
        HarpyTalon,
        HarpyFeather,
        MoonstoneFragment,
        ShadowSilk,
        FrostfangScale,
        WyrmFang,
        WyrmHeart,
        HollowSoul,
        HollowCrown,
        Godbone,
        StarvedGodsEye,
        PhoenixAsh,
        WorldEaterScale,
        WorldEaterFang,
        MalakarsHeart,

        // --------------------------------------------------------
        // Rare / Collection Log Drops
        // --------------------------------------------------------

        ChickenClaw,
        GoldenFeather,
        ChickenGodFeather,
        GoblinChampionsTooth,
        GoblinCrownShard,
        GoblinGodTooth,
        RatKingsTail,
        PlagueRatFur,
        VerminousHeart,
        VampiricWing,
        Bloodwing,
        AbyssalBatHeart,
        WitchHatFragment,
        FamiliarCore,
        SwampHeart,
        DryadHeart,
        AncientSeed,
        WorldTreeSeed,
        GravebladeFragment,
        DeathKnightsSigil,
        SoulboundSkull,
        AbyssalTongue,
        AbyssalEye,
        AbyssalCore,
        BloodmoonMaskFragment,
        CultistRelic,
        BloodmoonEye,
        WerewolfHeart,
        AlphaPelt,
        LycanthropeCore,
        ManticoreEye,
        ManticoreHeart,
        CrypticCore,
        BoneCollectorsMask,
        AncientSkull,
        DeathsEssence,
        TreantCrown,
        AncientHeartwood,
        WorldrootCore,
        HarpyCrest,
        DreadwingFeather,
        HarpyHeart,
        VoidWarlockEye,
        VoidTomeFragment,
        VoidWarlockCore,
        FrostfangHeart,
        WyrmCrown,
        AncientWyrmCore,
        HollowKingsSigil,
        HollowEmber,
        EmperorsSoul,
        StarvedGodsHeart,
        StarvedGodRelic,
        FragmentOfDivinity,
        MalakarsHorn,
        WorldEaterHeart,
        WorldEatersEye,

        // --------------------------------------------------------
        // Weapons
        // --------------------------------------------------------

        BronzeSword,
        IronSword,
        SteelSword,
        MithrilSword,
        AdamantSword,
        RuneSword,

        GoblinBlade,
        GoblinKingsBlade,
        BoneSword,
        Graveblade,
        Bloodfang,
        Moonfang,
        VoidReaper,
        Dragonbane,
        Worldbreaker,

        GoblinGodblade,
        DeathsScythe,
        BloodmoonExecutioner,
        VoidGodslayer,
        EyeOfEternity,

        // --------------------------------------------------------
        // Shields
        // --------------------------------------------------------

        WoodenShield,
        BronzeShield,
        IronShield,
        SteelShield,
        RuneShield,
        BoneShield,
        DragonfireShield,
        VoidAegis,

        // --------------------------------------------------------
        // Head
        // --------------------------------------------------------

        BronzeHelm,
        IronHelm,
        SteelHelm,
        RuneHelm,
        GoblinCrown,
        WolfHelm,
        BloodmoonCrown,
        VoidCrown,
        CelestialCrown,

        // --------------------------------------------------------
        // Body
        // --------------------------------------------------------

        BronzePlatebody,
        IronPlatebody,
        SteelPlatebody,
        RunePlatebody,
        BonePlatebody,
        WerewolfChestplate,
        Bloodplate,
        VoidBody,
        WorldEaterPlate,

        // --------------------------------------------------------
        // Legs
        // --------------------------------------------------------

        BronzePlatelegs,
        IronPlatelegs,
        SteelPlatelegs,
        RunePlatelegs,
        BonePlatelegs,
        BloodplateLegs,
        VoidLegs,
        WorldEaterGreaves,

        // --------------------------------------------------------
        // Gloves
        // --------------------------------------------------------

        LeatherGloves,
        IronGauntlets,
        SteelGauntlets,
        RuneGauntlets,
        WerewolfClaws,
        BloodfangGauntlets,
        VoidClaws,

        // --------------------------------------------------------
        // Boots
        // --------------------------------------------------------

        LeatherBoots,
        IronBoots,
        SteelBoots,
        RuneBoots,
        WolfPeltBoots,
        BloodstainedBoots,
        Voidwalkers,

        // --------------------------------------------------------
        // Amulets
        // --------------------------------------------------------

        AmuletOfAccuracy,
        AmuletOfStrength,
        AmuletOfDefense,
        BloodAmulet,
        MoonstoneAmulet,
        VoidAmulet,
        DivineAmulet,

        // --------------------------------------------------------
        // Rings
        // --------------------------------------------------------

        RingOfAttack,
        RingOfStrength,
        RingOfDefense,
        RingOfVitality,
        BloodmoonRing,
        BerserkersRing,
        GuardiansRing,
        VoidRing,
        RingOfTheGods,

        // --------------------------------------------------------
        // Unique Equipment
        // --------------------------------------------------------

        PhoenixCloak,
        DragonscaleArmor,
        AncientDragonHelm,
        GodslayerArmor,
        StarforgedHelm,
        WorldEaterCrown,
        FragmentOfCreation
    };

    public static IReadOnlyList<Item> AllItems { get; private set; } = Array.Empty<Item>();

    static ItemData()
    {
        ItemRegistry.AddRange(GetEquipmentExpansionItems());
        ItemRegistry.AddRange(GetEquipmentExpansionTwoItems());
        ItemRegistry.AddRange(GetAreaBossItems());

        foreach (Item item in ItemRegistry)
        {
            item.IsJunk = item.Type == ItemType.Material;
            item.Description = NeedsRealDescription(item.Description)
                ? CreateItemDescription(item)
                : item.Description;
        }

        foreach (Item item in ItemRegistry.Where(item =>
                     item.Type == ItemType.Equipment &&
                     item.EquipmentSlot != EquipmentSlot.None))
        {
            if (item.EquipmentSlot == EquipmentSlot.Weapon)
            {
                int weaponPower =
                    item.AttackBonus +
                    item.StrengthBonus +
                    item.DefenseBonus / 2;

                item.RequiredAttackLevel =
                    GetRequiredEquipmentLevel(weaponPower, 0.75);
            }
            else
            {
                int armorPower =
                    item.DefenseBonus +
                    item.HPBonus * 3 +
                    (item.AttackBonus + item.StrengthBonus) / 2;

                item.RequiredDefenseLevel =
                    GetRequiredEquipmentLevel(armorPower, 0.9);
            }
        }

        // Publish a read-only snapshot after generated items and derived
        // metadata have been applied.
        AllItems = Array.AsReadOnly(ItemRegistry.ToArray());
    }

    private static string CreateItemDescription(Item item)
    {
        string itemName = item.Name.ToLowerInvariant();

        return item.Type switch
        {
            ItemType.Equipment => CreateEquipmentDescription(item, itemName),
            ItemType.Food => CreateFoodDescription(item, itemName),
            ItemType.Pet => CreatePetDescription(item, itemName),
            ItemType.Currency => CreateCurrencyDescription(item, itemName),
            _ => CreateMaterialDescription(item, itemName)
        };
    }

    private static bool NeedsRealDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return true;

        string normalized = description.Trim().ToLowerInvariant();
        return normalized.Contains("found on your adventure", StringComparison.Ordinal) ||
               normalized.Contains("found on your adventures", StringComparison.Ordinal) ||
               normalized.Contains("gathered during your adventure", StringComparison.Ordinal) ||
               normalized.Contains("collected on your adventure", StringComparison.Ordinal);
    }

    private static string CreateEquipmentDescription(Item item, string itemName)
    {
        string craftsmanship = GetEquipmentCraftsmanship(itemName);
        string purpose = item.EquipmentSlot switch
        {
            EquipmentSlot.Weapon when item.AttackSpeedTicks <= 3 =>
                "built for quick, precise strikes before an opponent can answer",
            EquipmentSlot.Weapon when item.StrengthBonus > item.AttackBonus =>
                "weighted to turn every committed swing into a crushing blow",
            EquipmentSlot.Weapon =>
                "balanced to deliver reliable attacks in prolonged battles",
            EquipmentSlot.Shield =>
                "shaped to catch incoming blows and hold a defensive line",
            EquipmentSlot.Head =>
                "made to guard the head without sacrificing awareness",
            EquipmentSlot.Body =>
                "constructed to protect the torso through the harshest fights",
            EquipmentSlot.Legs =>
                "reinforced where a warrior needs protection while staying mobile",
            EquipmentSlot.Gloves =>
                "made to protect the hands while preserving a firm weapon grip",
            EquipmentSlot.Boots =>
                "designed to keep its wearer sure-footed on dangerous ground",
            EquipmentSlot.Amulet =>
                "worn close to the heart so its power can strengthen the bearer",
            EquipmentSlot.Ring =>
                "small enough for a finger yet charged with a warrior's power",
            _ =>
                "crafted to give its bearer an edge in combat"
        };

        string notableBonus = GetNotableEquipmentBonus(item);
        return $"The {item.Name} is {craftsmanship} {purpose}. {notableBonus}";
    }

    private static string GetEquipmentCraftsmanship(string itemName)
    {
        if (itemName.Contains("bronze")) return "a workmanlike piece of bronze equipment ";
        if (itemName.Contains("iron")) return "a sturdy piece of iron equipment ";
        if (itemName.Contains("steel")) return "a dependable piece of tempered steel equipment ";
        if (itemName.Contains("mithril")) return "a light, resilient piece of blue mithril equipment ";
        if (itemName.Contains("adamant")) return "a formidable piece of green adamant equipment ";
        if (itemName.Contains("rune") || itemName.Contains("runic")) return "an expertly worked piece of rune equipment ";
        if (itemName.Contains("dragon")) return "a fearsome piece fashioned with draconic materials ";
        if (itemName.Contains("bone")) return "a grim piece assembled from polished monster bone ";
        if (itemName.Contains("blood")) return "a dark crimson relic steeped in blood magic ";
        if (itemName.Contains("frost") || itemName.Contains("ice")) return "a cold-forged relic rimed with unmelting frost ";
        if (itemName.Contains("ember") || itemName.Contains("infernal") || itemName.Contains("magma") || itemName.Contains("cinder")) return "a heat-scarred relic that smoulders from within ";
        if (itemName.Contains("void") || itemName.Contains("abyss")) return "an unsettling relic shaped by power from beyond the world ";
        if (itemName.Contains("astral") || itemName.Contains("celestial") || itemName.Contains("star")) return "a luminous relic carrying the quiet force of the heavens ";
        if (itemName.Contains("moon") || itemName.Contains("eclipse")) return "a mysterious relic touched by pale lunar power ";
        if (itemName.Contains("sand") || itemName.Contains("dune") || itemName.Contains("scarab")) return "an ancient desert relic weathered by centuries of drifting sand ";
        if (itemName.Contains("swamp") || itemName.Contains("marsh") || itemName.Contains("moss") || itemName.Contains("briar") || itemName.Contains("bark")) return "a living-looking piece bound with resilient woodland materials ";
        if (itemName.Contains("goblin")) return "a brutal goblin-made piece whose crude finish hides surprising effectiveness ";
        if (itemName.Contains("wolf") || itemName.Contains("werewolf")) return "a savage trophy-piece carrying the ferocity of a great wolf ";
        if (itemName.Contains("crystal") || itemName.Contains("sapphire")) return "a finely cut relic that channels power through its crystalline facets ";
        if (itemName.Contains("god") || itemName.Contains("divine")) return "a sacred relic made potent by a trace of divine power ";
        if (itemName.Contains("leather") || itemName.Contains("hide")) return "a supple piece of carefully cured hide equipment ";
        if (itemName.Contains("obsidian")) return "a razor-edged piece carved from glossy volcanic glass ";
        return "a distinctive piece of equipment, recognizable by its unusual workmanship, ";
    }

    private static string GetNotableEquipmentBonus(Item item)
    {
        (int value, string text)[] bonuses =
        {
            (item.AttackBonus, "Its strongest enchantment improves attack accuracy"),
            (item.StrengthBonus, "Its strongest enchantment increases striking power"),
            (item.DefenseBonus, "Its strongest enchantment turns aside incoming damage"),
            (item.HPBonus * 3, "Its strongest enchantment fortifies the wearer's vitality")
        };

        (int value, string text) strongest = bonuses.MaxBy(entry => entry.value);
        return strongest.value > 0
            ? $"{strongest.text}."
            : "Its value lies in craftsmanship rather than enchantment.";
    }

    private static string CreateFoodDescription(Item item, string itemName)
    {
        string character = itemName switch
        {
            _ when itemName.StartsWith("raw ", StringComparison.Ordinal) =>
                "It is uncooked and unappetizing, but can still be swallowed in an emergency",
            _ when itemName.Contains("shark") || itemName.Contains("swordfish") =>
                "Its dense flesh makes it a substantial meal for a wounded fighter",
            _ when itemName.Contains("lobster") || itemName.Contains("crab") =>
                "The rich shellfish meat is awkward to eat quickly but surprisingly sustaining",
            _ when itemName.Contains("trout") || itemName.Contains("salmon") || itemName.Contains("shrimp") || itemName.Contains("fish") =>
                "This simple catch provides a quick mouthful of energy",
            _ when itemName.Contains("watermelon") || itemName.Contains("berry") || itemName.Contains("fruit") =>
                "Its sweet, water-rich flesh is especially refreshing after a hard fight",
            _ when itemName.Contains("potato") =>
                "This plain tuber is filling, portable, and better than fighting on an empty stomach",
            _ when itemName.Contains("herb") || itemName.Contains("leaf") =>
                "Its bitter medicinal oils make it useful despite the unpleasant taste",
            _ when itemName.Contains("potion") =>
                "The carefully mixed draught releases its restorative effect as soon as it is consumed",
            _ when itemName.Contains("meat") || itemName.Contains("chicken") || itemName.Contains("beef") =>
                "A rough portion of meat that offers immediate nourishment",
            _ =>
                "An unusual but edible provision kept ready for desperate moments"
        };

        return $"The {item.Name} restores {item.HealingAmount} HP when eaten. {character}.";
    }

    private static string CreatePetDescription(Item item, string itemName)
    {
        string personality = itemName switch
        {
            "heron" => "It watches every fishing spot with patient, practiced attention",
            "rocky" => "This nimble raccoon has a suspicious talent for locating unattended valuables",
            "beaver" => "It studies every log and tree as though planning its next ambitious dam",
            "squirrel" => "It bounds across obstacles with effortless agility and endless enthusiasm",
            "raccoon" => "Its clever paws are always ready to investigate locks, pockets, and shiny objects",
            "golem" => "Tiny stone footsteps follow its owner from one promising mineral vein to the next",
            "arrow eagle" => "Its sharp eyes inspect every shaft and fletching with a master archer's scrutiny",
            "tangleroot" => "Leaves and roots shift around it as though the soil itself has learned to walk",
            _ => $"This exceptionally rare {itemName} chooses to accompany only the most dedicated adventurers"
        };

        return $"{item.Name} is a treasured companion rather than an inventory item. {personality}.";
    }

    private static string CreateCurrencyDescription(Item item, string itemName)
    {
        if (itemName == "coins")
            return "Coins are the realm's everyday currency, accepted by merchants from Greenvale to the Umbral Expanse. They can be spent to expand inventory capacity.";

        if (itemName.Contains("stolen"))
            return $"{item.Name} bears no record of its former owner. It spends just as readily as honestly earned currency.";

        return $"{item.Name} is recognized currency with a value of {item.Value:N0} coin{(item.Value == 1 ? "" : "s")}. Merchants accept it despite its unusual origin.";
    }

    private static string CreateMaterialDescription(Item item, string itemName)
    {
        string detail = itemName switch
        {
            _ when itemName.Contains("ore") || itemName == "coal" =>
                "Mineral-rich stone ready to be refined by a skilled smith",
            _ when itemName.Contains("bar") || itemName.Contains("ingot") =>
                "A refined metal billet whose clean edges make it suitable for precise smithing",
            _ when itemName.Contains("logs") || itemName == "logs" || itemName.Contains("wood") =>
                "Seasoned timber with a grain suited to bows, handles, and sturdy construction",
            _ when itemName.Contains("bone") || itemName.Contains("skull") =>
                "A bleached remnant valued by collectors, craftsmen, and practitioners of darker arts",
            _ when itemName.Contains("feather") =>
                "A light, well-formed plume useful for fletching balanced projectiles",
            _ when itemName.Contains("hide") || itemName.Contains("pelt") || itemName.Contains("leather") =>
                "A durable animal skin that can be cured and worked into protective gear",
            _ when itemName.Contains("silk") || itemName.Contains("thread") =>
                "Fine, resilient fibre prized for light garments and delicate bindings",
            _ when itemName.Contains("scale") =>
                "A naturally armoured plate whose overlapping ridges resist blades and heat",
            _ when itemName.Contains("claw") || itemName.Contains("talon") =>
                "A keen natural weapon that retains an edge long after leaving its former owner",
            _ when itemName.Contains("fang") || itemName.Contains("tooth") =>
                "A hard, sharply pointed trophy suitable for jewellery or weapon fittings",
            _ when itemName.Contains("eye") =>
                "An unnerving specimen that seems to follow movement even after its creature's defeat",
            _ when itemName.Contains("heart") =>
                "A rare organ still carrying an echo of the creature's unnatural vitality",
            _ when itemName.Contains("essence") =>
                "Condensed magical energy that hums softly when held near another enchanted object",
            _ when itemName.Contains("core") =>
                "The concentrated inner power of a magical creature or construct, stable enough to handle with care",
            _ when itemName.Contains("shard") || itemName.Contains("fragment") =>
                "A broken piece of something far more powerful, with magic lingering along every fractured edge",
            _ when itemName.Contains("crystal") || itemName.Contains("gem") || itemName.Contains("pearl") =>
                "A naturally formed treasure whose clarity and colour make it valuable to jewellers",
            _ when itemName.Contains("dust") || itemName.Contains("ash") =>
                "Fine residue carrying traces of the strange power that produced it",
            _ when itemName.Contains("leaf") || itemName.Contains("herb") || itemName.Contains("sapling") =>
                "Living plant matter with properties useful to farmers, herbalists, and crafters",
            _ when itemName.Contains("cloth") || itemName.Contains("wool") =>
                "Workable textile material ready to be cut, stitched, or woven",
            _ when itemName.Contains("key") =>
                "A carefully cut key whose lock is likely more interesting than the metal itself",
            _ when itemName.Contains("relic") || itemName.Contains("tablet") || itemName.Contains("scroll") =>
                "An old artifact marked with clues to the people and magic that shaped it",
            _ when itemName.Contains("ear") || itemName.Contains("tongue") || itemName.Contains("tail") =>
                "A grisly monster trophy that specialist traders will purchase for their own obscure purposes",
            _ when itemName.Contains("rope") || itemName.Contains("shaft") || itemName.Contains("arrow") =>
                "A practical crafting component prepared for use in a larger piece of equipment",
            _ =>
                "A distinctive trade material whose unusual properties make it useful to collectors and specialist craftsmen"
        };

        return $"{item.Name} is {detail}. Its base market value is {item.Value:N0} coin{(item.Value == 1 ? "" : "s")}.";
    }

    private static int GetRequiredEquipmentLevel(
        int equipmentPower,
        double multiplier)
    {
        return Math.Clamp(
            (int)Math.Ceiling(equipmentPower * multiplier),
            1,
            99);
    }
}
