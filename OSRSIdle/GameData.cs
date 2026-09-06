namespace OSRSIdle;

public class SkillActivity
{
    public string Name { get; set; } = "";

    public string Icon { get; set; } = "";

    public int RequiredLevel { get; set; }

    public double XP { get; set; }

    private const double TickDurationSeconds = 0.6;

    public int ActionTicks { get; set; }

    // Existing activity definitions use this setter as a compact migration
    // path. Runtime timing and UI use only whole ActionTicks.
    public double ActionTime
    {
        set => ActionTicks = Math.Max(
            1,
            (int)Math.Ceiling(value / TickDurationSeconds));
    }

    public Item? ItemReward { get; set; }
}

public static class GameData
{
    // ============================================================
    // FISHING
    // ============================================================

    public static List<SkillActivity> Fishing = new()
    {
        new SkillActivity
        {
            Name = "Shrimp",
            Icon = "🦐",
            RequiredLevel = 1,
            XP = 10,
            ActionTicks = 6,
            ItemReward = ItemData.RawShrimp
        },

        new SkillActivity
        {
            Name = "Trout",
            Icon = "🐟",
            RequiredLevel = 20,
            XP = 50,
            ActionTime = 5,
            ItemReward = ItemData.RawTrout
        },

        new SkillActivity
        {
            Name = "Salmon",
            Icon = "🐟",
            RequiredLevel = 30,
            XP = 70,
            ActionTime = 5,
            ItemReward = ItemData.RawSalmon
        },

        new SkillActivity
        {
            Name = "Lobster",
            Icon = "🦞",
            RequiredLevel = 40,
            XP = 100,
            ActionTime = 6,
            ItemReward = ItemData.RawLobster
        },

        new SkillActivity
        {
            Name = "Swordfish",
            Icon = "🐟",
            RequiredLevel = 50,
            XP = 140,
            ActionTime = 7,
            ItemReward = ItemData.RawSwordfish
        },

        new SkillActivity
        {
            Name = "Shark",
            Icon = "🦈",
            RequiredLevel = 76,
            XP = 200,
            ActionTime = 10,
            ItemReward = ItemData.RawShark
        }
    };


    // ============================================================
    // MINING
    // ============================================================

    public static List<SkillActivity> Mining = new()
    {
        new SkillActivity
        {
            Name = "Copper",
            Icon = "🟤",
            RequiredLevel = 1,
            XP = 10,
            ActionTime = 3,
            ItemReward = ItemData.CopperOre
        },

        new SkillActivity
        {
            Name = "Tin",
            Icon = "🟤",
            RequiredLevel = 1,
            XP = 10,
            ActionTime = 3,
            ItemReward = ItemData.TinOre
        },

        new SkillActivity
        {
            Name = "Iron",
            Icon = "🟤",
            RequiredLevel = 15,
            XP = 35,
            ActionTime = 5,
            ItemReward = ItemData.IronOre
        },

        new SkillActivity
        {
            Name = "Coal",
            Icon = "🟤",
            RequiredLevel = 30,
            XP = 50,
            ActionTime = 6,
            ItemReward = ItemData.CoalOre
        },

        new SkillActivity
        {
            Name = "Mithril",
            Icon = "🟤",
            RequiredLevel = 55,
            XP = 80,
            ActionTime = 8,
            ItemReward = ItemData.MithrilOre
        },

        new SkillActivity
        {
            Name = "Adamantite",
            Icon = "🟤",
            RequiredLevel = 70,
            XP = 95,
            ActionTime = 10,
            ItemReward = ItemData.AdamantiteOre
        },

        new SkillActivity
        {
            Name = "Runite",
            Icon = "🟤",
            RequiredLevel = 85,
            XP = 125,
            ActionTime = 12,
            ItemReward = ItemData.RuniteOre
        }
    };


    // ============================================================
    // WOODCUTTING
    // ============================================================

    public static List<SkillActivity> Woodcutting = new()
    {
        new SkillActivity
        {
            Name = "Normal Tree",
            Icon = "🌳",
            RequiredLevel = 1,
            XP = 25,
            ActionTime = 4,
            ItemReward = ItemData.Logs
        },

        new SkillActivity
        {
            Name = "Oak",
            Icon = "🌳",
            RequiredLevel = 15,
            XP = 37,
            ActionTime = 5,
            ItemReward = ItemData.OakLogs
        },

        new SkillActivity
        {
            Name = "Willow",
            Icon = "🌳",
            RequiredLevel = 30,
            XP = 67,
            ActionTime = 6,
            ItemReward = ItemData.WillowLogs
        },

        new SkillActivity
        {
            Name = "Yew",
            Icon = "🌳",
            RequiredLevel = 60,
            XP = 175,
            ActionTime = 10,
            ItemReward = ItemData.YewLogs
        },

        new SkillActivity
        {
            Name = "Magic Tree",
            Icon = "🌳",
            RequiredLevel = 75,
            XP = 250,
            ActionTime = 12,
            ItemReward = ItemData.MagicLogs
        }
    };


    // ============================================================
    // AGILITY
    // ============================================================

    public static List<SkillActivity> Agility = new()
    {
        new SkillActivity
        {
            Name = "Gnome Agility Course",
            Icon = "🏃",
            RequiredLevel = 1,
            XP = 86,
            ActionTime = 60
        },

        new SkillActivity
        {
            Name = "Varrock Agility Course",
            Icon = "🏃",
            RequiredLevel = 30,
            XP = 175,
            ActionTime = 60
        },

        new SkillActivity
        {
            Name = "Canifis Agility Course",
            Icon = "🏃",
            RequiredLevel = 40,
            XP = 240,
            ActionTime = 60
        },

        new SkillActivity
        {
            Name = "Seers' Village Agility Course",
            Icon = "🏃",
            RequiredLevel = 60,
            XP = 570,
            ActionTime = 60
        },

        new SkillActivity
        {
            Name = "Ardougne Agility Course",
            Icon = "🏃",
            RequiredLevel = 90,
            XP = 793,
            ActionTime = 60
        }
    };


    // ============================================================
    // THIEVING
    // ============================================================

    public static List<SkillActivity> Thieving = new()
    {
        new SkillActivity { Name = "Pickpocket Villager", Icon = "🧑", RequiredLevel = 1, XP = 12, ActionTime = 3, ItemReward = ItemData.Coins },
        new SkillActivity { Name = "Steal from Market Stall", Icon = "🏪", RequiredLevel = 20, XP = 55, ActionTime = 5, ItemReward = ItemData.Coins },
        new SkillActivity { Name = "Pilfer Silk Stall", Icon = "🧵", RequiredLevel = 45, XP = 120, ActionTime = 7, ItemReward = ItemData.Silk },
        new SkillActivity { Name = "Crack Palace Chest", Icon = "🗝️", RequiredLevel = 75, XP = 260, ActionTime = 10, ItemReward = ItemData.JeweledRelic }
    };


    // ============================================================
    // CRAFTING
    // ============================================================

    public static List<SkillActivity> Crafting = new()
    {
        new SkillActivity { Name = "Shape Clay Pot", Icon = "🏺", RequiredLevel = 1, XP = 15, ActionTime = 3, ItemReward = ItemData.Clay },
        new SkillActivity { Name = "Craft Leather Gloves", Icon = "🧤", RequiredLevel = 20, XP = 60, ActionTime = 5, ItemReward = ItemData.SoftLeatherGloves },
        new SkillActivity { Name = "String Sapphire Amulet", Icon = "📿", RequiredLevel = 50, XP = 135, ActionTime = 7, ItemReward = ItemData.SapphireAmulet },
        new SkillActivity { Name = "Craft Dragonhide Body", Icon = "🐉", RequiredLevel = 80, XP = 290, ActionTime = 10, ItemReward = ItemData.DragonhideBody }
    };


    // ============================================================
    // FLETCHING
    // ============================================================

    public static List<SkillActivity> Fletching = new()
    {
        new SkillActivity { Name = "Cut Arrow Shafts", Icon = "🪵", RequiredLevel = 1, XP = 12, ActionTime = 3, ItemReward = ItemData.ArrowShafts },
        new SkillActivity { Name = "Make Headless Arrows", Icon = "🏹", RequiredLevel = 20, XP = 55, ActionTime = 5, ItemReward = ItemData.HeadlessArrows },
        new SkillActivity { Name = "Fletch Willow Shortbow", Icon = "🏹", RequiredLevel = 40, XP = 115, ActionTime = 7, ItemReward = ItemData.WillowShortbow },
        new SkillActivity { Name = "Fletch Magic Longbow", Icon = "🏹", RequiredLevel = 75, XP = 250, ActionTime = 10, ItemReward = ItemData.MagicLongbow }
    };


    // ============================================================
    // FARMING
    // ============================================================

    public static List<SkillActivity> Farming = new()
    {
        new SkillActivity { Name = "Harvest Potatoes", Icon = "🥔", RequiredLevel = 1, XP = 14, ActionTime = 4, ItemReward = ItemData.Potato },
        new SkillActivity { Name = "Harvest Herb Patch", Icon = "🌿", RequiredLevel = 25, XP = 65, ActionTime = 6, ItemReward = ItemData.GrimyHerb },
        new SkillActivity { Name = "Harvest Watermelons", Icon = "🍉", RequiredLevel = 50, XP = 145, ActionTime = 8, ItemReward = ItemData.Watermelon },
        new SkillActivity { Name = "Tend Magic Sapling", Icon = "🌱", RequiredLevel = 80, XP = 310, ActionTime = 12, ItemReward = ItemData.MagicSapling }
    };
}
