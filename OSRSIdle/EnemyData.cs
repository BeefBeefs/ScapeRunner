namespace OSRSIdle;

public static partial class EnemyData
{
    // ============================================================
    // TIER 1 - BEGINNER
    // ============================================================

    public static Enemy Chicken = new Enemy
    {
        Name = "Chicken",
        Icon = "🐔",
        Tier = EnemyTier.Tier1,

        HP = 1,
        Attack = 1,
        Strength = 1,
        Defense = 1,

        AttackSpeedTicks = 8,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                // Common
                new Drop
                {
                    Item = ItemData.Feathers,
                    Chance = 1.0,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.RawChicken,
                    Chance = 0.50,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.Bones,
                    Chance = 0.75,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Common
                },

                // Rare - 1/100
                new Drop
                {
                    Item = ItemData.AnimalHide,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                }
            }
        }
    };


    // ============================================================
    // 2. BIG CHICKEN
    // ============================================================

    public static Enemy BigChicken = new Enemy
    {
        Name = "Big Chicken",
        Icon = "🐓",
        Tier = EnemyTier.Tier1,

        HP = 5,
        Attack = 2,
        Strength = 2,
        Defense = 2,

        AttackSpeedTicks = 8,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.Feathers,
                    Chance = 1.0,
                    MinQuantity = 2,
                    MaxQuantity = 6,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.RawChicken,
                    Chance = 0.75,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.Bones,
                    Chance = 1.0,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.AnimalHide,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Rare
                },

                // 1/1000
                new Drop
                {
                    Item = ItemData.BronzeSword,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // 3. MOSS-COVERED GOBLIN
    // ============================================================

    public static Enemy MossCoveredGoblin = new Enemy
    {
        Name = "Moss-Covered Goblin",
        Icon = "👺",
        Tier = EnemyTier.Tier1,

        HP = 10,
        Attack = 3,
        Strength = 3,
        Defense = 2,

        AttackSpeedTicks = 7,

        RequiredSkillName = "Woodcutting",
        RequiredSkillIcon = "🪓",
        RequiredSkillLevel = 5,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.GoblinEar,
                    Chance = 0.75,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.GoblinTooth,
                    Chance = 0.50,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.Coins,
                    Chance = 0.80,
                    MinQuantity = 2,
                    MaxQuantity = 10,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.GoblinBlade,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.GoblinCrown,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // 4. CAVE RAT
    // ============================================================

    public static Enemy CaveRat = new Enemy
    {
        Name = "Cave Rat",
        Icon = "🐀",
        Tier = EnemyTier.Tier1,

        HP = 15,
        Attack = 4,
        Strength = 4,
        Defense = 3,

        AttackSpeedTicks = 6,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.RatTail,
                    Chance = 0.80,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.RatFur,
                    Chance = 0.60,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.Bones,
                    Chance = 0.75,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.AnimalHide,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.IronSword,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // TIER 2 - EARLY/MID GAME
    // ============================================================

    public static Enemy GloombloodBat = new Enemy
    {
        Name = "Gloomblood Bat",
        Icon = "🦇",
        Tier = EnemyTier.Tier2,

        HP = 22,
        Attack = 5,
        Strength = 5,
        Defense = 4,

        AttackSpeedTicks = 6,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.BatWing,
                    Chance = 0.85,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.BatFang,
                    Chance = 0.45,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.Eyeball,
                    Chance = 0.20,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.WolfPelt,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.BoneSword,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // 6. SWAMP WITCH'S FAMILIAR
    // ============================================================

    public static Enemy SwampWitchFamiliar = new Enemy
    {
        Name = "Swamp Witch's Familiar",
        Icon = "🐸",
        Tier = EnemyTier.Tier2,

        HP = 30,
        Attack = 6,
        Strength = 6,
        Defense = 5,

        AttackSpeedTicks = 7,

        RequiredSkillName = "Fishing",
        RequiredSkillIcon = "🎣",
        RequiredSkillLevel = 8,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.MossyMushroom,
                    Chance = 0.80,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.WitchEye,
                    Chance = 0.35,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.WitchBroomBristle,
                    Chance = 0.25,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.SpiderSilk,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.Graveblade,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // 7. FERAL DRYAD
    // ============================================================

    public static Enemy FeralDryad = new Enemy
    {
        Name = "Feral Dryad",
        Icon = "🌿",
        Tier = EnemyTier.Tier2,

        HP = 40,
        Attack = 7,
        Strength = 8,
        Defense = 7,

        AttackSpeedTicks = 8,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.EnchantedLeaf,
                    Chance = 0.75,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.DryadBark,
                    Chance = 0.45,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.ThornedVine,
                    Chance = 0.30,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.EnchantedLeaf,
                    Chance = 0.01,
                    MinQuantity = 3,
                    MaxQuantity = 8,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.WolfHelm,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // 8. GRAVE KNIGHT
    // ============================================================

    public static Enemy GraveKnight = new Enemy
    {
        Name = "Grave Knight",
        Icon = "💀",
        Tier = EnemyTier.Tier2,

        HP = 55,
        Attack = 9,
        Strength = 10,
        Defense = 10,

        AttackSpeedTicks = 8,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.GraveDust,
                    Chance = 0.70,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.RustedSwordFragment,
                    Chance = 0.40,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.CursedBone,
                    Chance = 0.20,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.Graveblade,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.DeathsScythe,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                },

                new Drop
                {
                    Item = ItemData.DeathsScythe,
                    Chance = 0.0005,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.MegaRare
                }
            }
        }
    };


    // ============================================================
    // TIER 3 - MID GAME
    // ============================================================

    public static Enemy AbyssalToad = new Enemy
    {
        Name = "Abyssal Toad",
        Icon = "🐸",
        Tier = EnemyTier.Tier3,

        HP = 70,
        Attack = 11,
        Strength = 12,
        Defense = 9,

        AttackSpeedTicks = 7,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.AbyssalSlime,
                    Chance = 0.75,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.WitchEye,
                    Chance = 0.30,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.Eyeball,
                    Chance = 0.50,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.VoidEssence,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.VoidReaper,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // 10. BLOODMOON CULTIST
    // ============================================================

    public static Enemy BloodmoonCultist = new Enemy
    {
        Name = "Bloodmoon Cultist",
        Icon = "🌙",
        Tier = EnemyTier.Tier3,

        HP = 85,
        Attack = 13,
        Strength = 13,
        Defense = 11,

        AttackSpeedTicks = 7,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.BloodmoonShard,
                    Chance = 0.45,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.CultistRobeScrap,
                    Chance = 0.60,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.VoidEssence,
                    Chance = 0.10,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.BloodAmulet,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.BloodmoonCrown,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                },

                new Drop
                {
                    Item = ItemData.BloodmoonExecutioner,
                    Chance = 0.0005,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.MegaRare
                }
            }
        }
    };


    // ============================================================
    // 11. IRONFANG WEREWOLF
    // ============================================================

    public static Enemy IronfangWerewolf = new Enemy
    {
        Name = "Ironfang Werewolf",
        Icon = "🐺",
        Tier = EnemyTier.Tier3,

        HP = 105,
        Attack = 15,
        Strength = 16,
        Defense = 13,

        AttackSpeedTicks = 6,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.WerewolfFang,
                    Chance = 0.65,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.WerewolfPelt,
                    Chance = 0.50,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.BloodmoonShard,
                    Chance = 0.12,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.WerewolfClaws,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.Bloodfang,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // 12. CRYPT MANTICORE
    // ============================================================

    public static Enemy CryptManticore = new Enemy
    {
        Name = "Crypt Manticore",
        Icon = "🦂",
        Tier = EnemyTier.Tier4,

        HP = 130,
        Attack = 18,
        Strength = 20,
        Defense = 15,

        AttackSpeedTicks = 7,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.ManticoreSpike,
                    Chance = 0.55,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.ManticoreHide,
                    Chance = 0.45,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.AncientBone,
                    Chance = 0.25,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.MonsterClaw,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.Dragonbane,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // 13. BONE COLLECTOR
    // ============================================================

    public static Enemy BoneCollector = new Enemy
    {
        Name = "Bone Collector",
        Icon = "☠️",
        Tier = EnemyTier.Tier4,

        HP = 155,
        Attack = 21,
        Strength = 22,
        Defense = 20,

        AttackSpeedTicks = 8,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.AncientBone,
                    Chance = 0.70,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.CursedBone,
                    Chance = 0.55,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.GraveDust,
                    Chance = 0.75,
                    MinQuantity = 2,
                    MaxQuantity = 5,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.BoneSword,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.BoneShield,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // TIER 4 - LATE GAME
    // ============================================================

    public static Enemy ThornCrownedTreant = new Enemy
    {
        Name = "Thorn-Crowned Treant",
        Icon = "🌳",
        Tier = EnemyTier.Tier5,

        HP = 185,
        Attack = 24,
        Strength = 27,
        Defense = 25,

        AttackSpeedTicks = 9,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.TreantHeartwood,
                    Chance = 0.65,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.ThornedVine,
                    Chance = 0.75,
                    MinQuantity = 2,
                    MaxQuantity = 5,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.EnchantedLeaf,
                    Chance = 0.50,
                    MinQuantity = 2,
                    MaxQuantity = 4,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.DragonScale,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.Worldbreaker,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // 15. DREADWING HARPY
    // ============================================================

    public static Enemy DreadwingHarpy = new Enemy
    {
        Name = "Dreadwing Harpy",
        Icon = "🦅",
        Tier = EnemyTier.Tier5,

        HP = 210,
        Attack = 29,
        Strength = 25,
        Defense = 21,

        AttackSpeedTicks = 5,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.HarpyTalon,
                    Chance = 0.65,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.HarpyFeather,
                    Chance = 0.85,
                    MinQuantity = 2,
                    MaxQuantity = 5,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.MoonstoneFragment,
                    Chance = 0.15,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.MoonstoneAmulet,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.Moonfang,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // 16. VOID-TONGUED WARLOCK
    // ============================================================

    public static Enemy VoidTonguedWarlock = new Enemy
    {
        Name = "Void-Tongued Warlock",
        Icon = "🧙",
        Tier = EnemyTier.Tier5,

        HP = 240,
        Attack = 32,
        Strength = 30,
        Defense = 28,

        AttackSpeedTicks = 7,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.VoidEssence,
                    Chance = 0.65,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.ShadowSilk,
                    Chance = 0.55,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.MoonstoneFragment,
                    Chance = 0.25,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.VoidAmulet,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.VoidGodslayer,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                },

                new Drop
                {
                    Item = ItemData.EyeOfEternity,
                    Chance = 0.0005,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.MegaRare
                }
            }
        }
    };


    // ============================================================
    // 17. FROSTFANG WYRM
    // ============================================================

    public static Enemy FrostfangWyrm = new Enemy
    {
        Name = "Frostfang Wyrm",
        Icon = "🐉",
        Tier = EnemyTier.Tier6,

        HP = 300,
        Attack = 38,
        Strength = 40,
        Defense = 35,

        AttackSpeedTicks = 8,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.FrostfangScale,
                    Chance = 0.65,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.WyrmFang,
                    Chance = 0.45,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.WyrmHeart,
                    Chance = 0.12,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.AncientDragonScale,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.Dragonbane,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                }
            }
        }
    };


    // ============================================================
    // TIER 5 - ENDGAME
    // ============================================================

    public static Enemy EmperorOfTheHollow = new Enemy
    {
        Name = "Emperor of the Hollow",
        Icon = "👑",
        Tier = EnemyTier.Tier7,

        HP = 400,
        Attack = 45,
        Strength = 48,
        Defense = 45,

        AttackSpeedTicks = 8,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.HollowSoul,
                    Chance = 0.75,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.HollowCrown,
                    Chance = 0.25,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.Godbone,
                    Chance = 0.08,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.GodslayerArmor,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.DeathsScythe,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                },

                new Drop
                {
                    Item = ItemData.FragmentOfCreation,
                    Chance = 0.0005,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.MegaRare
                }
            }
        }
    };


    // ============================================================
    // 19. THE STARVED GOD
    // ============================================================

    public static Enemy StarvedGod = new Enemy
    {
        Name = "The Starved God",
        Icon = "👁️",
        Tier = EnemyTier.Tier7,

        HP = 550,
        Attack = 60,
        Strength = 65,
        Defense = 55,

        AttackSpeedTicks = 9,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.StarvedGodsEye,
                    Chance = 0.35,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.Godbone,
                    Chance = 0.55,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.PhoenixAsh,
                    Chance = 0.10,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.DivineAmulet,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.DeathsScythe,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                },

                new Drop
                {
                    Item = ItemData.EyeOfEternity,
                    Chance = 0.0005,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.MegaRare
                }
            }
        }
    };


    // ============================================================
    // 20. MALAKAR, THE WORLD-EATER
    // ============================================================

    public static Enemy Malakar = new Enemy
    {
        Name = "Malakar, the World-Eater",
        Icon = "🐲",
        Tier = EnemyTier.Tier7,

        HP = 750,
        Attack = 80,
        Strength = 90,
        Defense = 75,

        AttackSpeedTicks = 10,

        DropTable = new DropTable
        {
            Drops = new List<Drop>
            {
                new Drop
                {
                    Item = ItemData.WorldEaterScale,
                    Chance = 0.60,
                    MinQuantity = 1,
                    MaxQuantity = 3,
                    Rarity = DropRarity.Common
                },

                new Drop
                {
                    Item = ItemData.WorldEaterFang,
                    Chance = 0.30,
                    MinQuantity = 1,
                    MaxQuantity = 2,
                    Rarity = DropRarity.Uncommon
                },

                new Drop
                {
                    Item = ItemData.MalakarsHeart,
                    Chance = 0.05,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.Worldbreaker,
                    Chance = 0.01,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.Rare
                },

                new Drop
                {
                    Item = ItemData.WorldEaterCrown,
                    Chance = 0.001,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.SuperRare
                },

                new Drop
                {
                    Item = ItemData.EyeOfEternity,
                    Chance = 0.0005,
                    MinQuantity = 1,
                    MaxQuantity = 1,
                    Rarity = DropRarity.MegaRare
                }
            }
        }
    };


    // ============================================================
    // EXPANDED ROSTER
    // ============================================================

    private static Enemy CreateExpandedEnemy(
        string name,
        string icon,
        EnemyTier tier,
        int hp,
        int attack,
        int strength,
        int defense,
        int attackSpeedTicks,
        Item commonItem,
        Item rareItem,
        Item? superRareItem = null,
        Item? megaRareItem = null,
        string? requiredSkillName = null,
        string? requiredSkillIcon = null,
        int requiredSkillLevel = 0)
    {
        List<Drop> drops =
            new List<Drop>
            {
                new Drop { Item = ItemData.Coins, Chance = 0.80, MinQuantity = Math.Max(2, hp / 2), MaxQuantity = Math.Max(5, hp), Rarity = DropRarity.Common },
                new Drop { Item = commonItem, Chance = 0.65, MinQuantity = 1, MaxQuantity = 3, Rarity = DropRarity.Common },
                new Drop { Item = rareItem, Chance = 0.01, MinQuantity = 1, MaxQuantity = 1, Rarity = DropRarity.Rare }
            };

        if (superRareItem != null)
        {
            drops.Add(new Drop { Item = superRareItem, Chance = 0.001, MinQuantity = 1, MaxQuantity = 1, Rarity = DropRarity.SuperRare });
        }

        if (megaRareItem != null)
        {
            drops.Add(new Drop { Item = megaRareItem, Chance = 0.0001, MinQuantity = 1, MaxQuantity = 1, Rarity = DropRarity.MegaRare });
        }

        return new Enemy
        {
            Name = name,
            Icon = icon,
            Tier = tier,
            HP = hp,
            Attack = attack,
            Strength = strength,
            Defense = defense,
            AttackSpeedTicks = attackSpeedTicks,
            RequiredSkillName = requiredSkillName,
            RequiredSkillIcon = requiredSkillIcon,
            RequiredSkillLevel = requiredSkillLevel,
            DropTable = new DropTable { Drops = drops }
        };
    }


    // ============================================================
    // ALL ENEMIES
    // ============================================================

    public static List<Enemy> AllEnemies = new()
    {
        // Tier 1
        Chicken,
        BigChicken,
        MossCoveredGoblin,
        CaveRat,
        CreateExpandedEnemy("Meadow Imp", "👿", EnemyTier.Tier1, 5, 2, 2, 1, 6, ItemData.Feathers, ItemData.AnimalHide),
        CreateExpandedEnemy("Marsh Crab", "🦀", EnemyTier.Tier1, 9, 3, 3, 3, 7, ItemData.Bones, ItemData.BogPearl),
        CreateExpandedEnemy("Sewer Spider", "🕷️", EnemyTier.Tier1, 14, 4, 4, 3, 6, ItemData.SpiderSilk, ItemData.MonsterFang, requiredSkillName: "Fletching", requiredSkillIcon: "🏹", requiredSkillLevel: 6),
        CreateExpandedEnemy("Bandit Scout", "🥷", EnemyTier.Tier1, 19, 5, 5, 4, 5, ItemData.Coins, ItemData.BronzeSword),
        CreateExpandedEnemy("Cinder Hound", "🐕", EnemyTier.Tier2, 24, 6, 7, 4, 6, ItemData.EmberCore, ItemData.IronSword),
        CreateExpandedEnemy("Hill Troll", "🧌", EnemyTier.Tier2, 30, 7, 8, 6, 8, ItemData.Bones, ItemData.SteelSword, ItemData.SandstalkerDagger),

        // Tier 2
        GloombloodBat,
        SwampWitchFamiliar,
        FeralDryad,
        GraveKnight,
        CreateExpandedEnemy("Coral Serpent", "🐍", EnemyTier.Tier2, 38, 8, 9, 7, 6, ItemData.BogPearl, ItemData.SandstalkerDagger, requiredSkillName: "Fishing", requiredSkillIcon: "🎣", requiredSkillLevel: 20),
        CreateExpandedEnemy("Ashen Mage", "🧙", EnemyTier.Tier2, 45, 10, 10, 8, 7, ItemData.EmberCore, ItemData.AmuletOfAccuracy, requiredSkillName: "Mining", requiredSkillIcon: "⛏️", requiredSkillLevel: 20),
        CreateExpandedEnemy("Ironback Boar", "🐗", EnemyTier.Tier2, 52, 11, 13, 11, 7, ItemData.IronOre, ItemData.IronShield, requiredSkillName: "Farming", requiredSkillIcon: "🌱", requiredSkillLevel: 10),
        CreateExpandedEnemy("Moonlit Revenant", "👻", EnemyTier.Tier2, 60, 13, 13, 12, 6, ItemData.MoonstoneAmulet, ItemData.BoneShield, ItemData.RuneSentinelAegis),
        CreateExpandedEnemy("Sand Warden", "🏜️", EnemyTier.Tier3, 68, 14, 15, 13, 7, ItemData.AnimalHide, ItemData.SandstalkerDagger, ItemData.TidecallerTrident),
        CreateExpandedEnemy("Grove Guardian", "🌳", EnemyTier.Tier3, 76, 15, 17, 16, 8, ItemData.EnchantedLeaf, ItemData.WolfHelm, ItemData.RuneSentinelAegis),

        // Tier 3
        AbyssalToad,
        BloodmoonCultist,
        IronfangWerewolf,
        CryptManticore,
        BoneCollector,
        CreateExpandedEnemy("Storm Drake", "🐲", EnemyTier.Tier3, 85, 18, 20, 17, 7, ItemData.StormScale, ItemData.MithrilSword, ItemData.TidecallerTrident, requiredSkillName: "Woodcutting", requiredSkillIcon: "🪓", requiredSkillLevel: 35),
        CreateExpandedEnemy("Obsidian Golem", "🗿", EnemyTier.Tier3, 96, 20, 22, 24, 9, ItemData.ObsidianHeart, ItemData.MithrilBar, ItemData.RuneSentinelAegis, requiredSkillName: "Agility", requiredSkillIcon: "🏃", requiredSkillLevel: 40),
        CreateExpandedEnemy("Dune Assassin", "🦂", EnemyTier.Tier4, 108, 23, 25, 20, 5, ItemData.MonsterClaw, ItemData.SandstalkerDagger, ItemData.TidecallerTrident, requiredSkillName: "Fletching", requiredSkillIcon: "🏹", requiredSkillLevel: 55),
        CreateExpandedEnemy("Plague Bringer", "☠️", EnemyTier.Tier4, 120, 25, 27, 23, 7, ItemData.CursedBone, ItemData.BloodAmulet, ItemData.InfernalGreatsword, requiredSkillName: "Thieving", requiredSkillIcon: "🕵️", requiredSkillLevel: 60),
        CreateExpandedEnemy("Tidecaller", "🧜", EnemyTier.Tier4, 133, 28, 29, 26, 6, ItemData.BogPearl, ItemData.TidecallerTrident, ItemData.RuneSentinelAegis),
        CreateExpandedEnemy("Runic Sentinel", "🤖", EnemyTier.Tier4, 147, 30, 32, 34, 8, ItemData.RunicBar, ItemData.RuneShield, ItemData.InfernalGreatsword),

        // Tier 4
        ThornCrownedTreant,
        DreadwingHarpy,
        VoidTonguedWarlock,
        FrostfangWyrm,
        CreateExpandedEnemy("Crystal Basilisk", "🦎", EnemyTier.Tier5, 162, 33, 35, 32, 7, ItemData.CrystalFang, ItemData.RuneHelm, ItemData.AstralCrown),
        CreateExpandedEnemy("Infernal Knight", "🔥", EnemyTier.Tier5, 178, 36, 40, 38, 7, ItemData.EmberCore, ItemData.InfernalGreatsword, ItemData.AstralCrown, requiredSkillName: "Crafting", requiredSkillIcon: "🧶", requiredSkillLevel: 72),
        CreateExpandedEnemy("Eclipse Beast", "🌘", EnemyTier.Tier5, 195, 40, 43, 36, 6, ItemData.CelestialShard, ItemData.Bloodplate, ItemData.MonarchsMantle),
        CreateExpandedEnemy("Ancient Colossus", "🗿", EnemyTier.Tier5, 213, 43, 47, 48, 9, ItemData.ObsidianHeart, ItemData.RunePlatebody, ItemData.MonarchsMantle),
        CreateExpandedEnemy("Vortex Wyrm", "🌀", EnemyTier.Tier6, 232, 47, 51, 43, 7, ItemData.VoidEssence, ItemData.VoidAmulet, ItemData.InfernalGreatsword, requiredSkillName: "Farming", requiredSkillIcon: "🌱", requiredSkillLevel: 70),
        CreateExpandedEnemy("Fallen Champion", "🛡️", EnemyTier.Tier6, 250, 50, 55, 52, 6, ItemData.DeathsEssence, ItemData.RuneGauntlets, ItemData.AstralCrown, requiredSkillName: "Fishing", requiredSkillIcon: "🎣", requiredSkillLevel: 75),

        // Tier 5
        EmperorOfTheHollow,
        StarvedGod,
        Malakar,
        CreateExpandedEnemy("Rift Devourer", "👾", EnemyTier.Tier6, 260, 55, 60, 54, 7, ItemData.VoidEssence, ItemData.VoidAegis, ItemData.MonarchsMantle, requiredSkillName: "Mining", requiredSkillIcon: "⛏️", requiredSkillLevel: 80),
        CreateExpandedEnemy("Sunken Leviathan", "🐋", EnemyTier.Tier6, 270, 58, 64, 59, 8, ItemData.StormScale, ItemData.TidecallerTrident, ItemData.AstralCrown),
        CreateExpandedEnemy("Titan Warlord", "⚔️", EnemyTier.Tier6, 280, 62, 68, 64, 6, ItemData.ObsidianHeart, ItemData.GodslayerArmor, ItemData.InfernalGreatsword, requiredSkillName: "Woodcutting", requiredSkillIcon: "🪓", requiredSkillLevel: 85),
        CreateExpandedEnemy("Astral Chimera", "🐉", EnemyTier.Tier7, 288, 66, 71, 62, 6, ItemData.AstralDust, ItemData.AstralCrown, ItemData.MonarchsMantle, requiredSkillName: "Agility", requiredSkillIcon: "🏃", requiredSkillLevel: 75),
        CreateExpandedEnemy("Abyss Monarch", "👑", EnemyTier.Tier7, 295, 70, 76, 68, 7, ItemData.AbyssalCore, ItemData.VoidGodslayer, ItemData.CrownlessKingsBlade, requiredSkillName: "Crafting", requiredSkillIcon: "🧶", requiredSkillLevel: 85),
        CreateExpandedEnemy("The Crownless King", "👑", EnemyTier.Tier7, 300, 75, 82, 72, 6, ItemData.AstralDust, ItemData.MonarchsMantle, ItemData.AstralCrown, ItemData.CrownlessKingsBlade, requiredSkillName: "Farming", requiredSkillIcon: "🌱", requiredSkillLevel: 90)
    };

    static EnemyData()
    {
        AddAreaBosses();
        AddEquipmentExpansionDrops();
        AddEquipmentExpansionTwoDrops();
        AddSharedDrops();
        BalanceEquipmentDropRates();
        EnemyDescriptions.Apply(AllEnemies);
    }
}
