namespace OSRSIdle;

public class Player
{
    public string Name { get; set; } = "Adventurer";

    public int PortraitIndex { get; set; }

    public string PortraitImage =>
        PlayerPortraits.GetImage(PortraitIndex);

    public int AutoEatThresholdPercent { get; private set; } = 50;

    public event Action? AutoEatSettingsChanged;

    public void SetAutoEatThresholdPercent(int percentage)
    {
        int normalizedPercentage = Math.Clamp(percentage, 10, 90);

        if (AutoEatThresholdPercent == normalizedPercentage)
            return;

        AutoEatThresholdPercent = normalizedPercentage;
        AutoEatSettingsChanged?.Invoke();
    }

    public void RestoreAutoEatThresholdPercent(int percentage)
    {
        AutoEatThresholdPercent = Math.Clamp(percentage, 10, 90);
    }

    public bool ShouldAutoEat()
    {
        if (CurrentHP <= 0 || CurrentHP >= GetMaxHP())
            return false;

        int thresholdHP = (int)Math.Ceiling(
            GetMaxHP() * AutoEatThresholdPercent / 100d);

        return CurrentHP <= thresholdHP;
    }

    // ============================================================
    // NON-COMBAT SKILLS
    // ============================================================

    public List<Skill> Skills { get; private set; }

    public Skill Fishing { get; private set; }
    public Skill Mining { get; private set; }
    public Skill Woodcutting { get; private set; }
    public Skill Agility { get; private set; }
    public Skill Thieving { get; private set; }
    public Skill Crafting { get; private set; }
    public Skill Fletching { get; private set; }
    public Skill Farming { get; private set; }


    // ============================================================
    // COMBAT SKILLS
    // ============================================================

    public Skill HP { get; private set; }
    public Skill Attack { get; private set; }
    public Skill Strength { get; private set; }
    public Skill Defense { get; private set; }


    // ============================================================
    // INVENTORY
    // ============================================================

    public Inventory Inventory { get; private set; }

    public CollectionLog CollectionLog { get; private set; }

    public LuckiestDrop LuckiestDrop { get; private set; } = new();

    public double GlobalDropBoostPercent =>
        CollectionLog.GetCompletedEnemyCount() * 0.1d;

    public event Action? LuckiestDropChanged;

    public void RecordLuckiestDrop(Item item, long attempts, double chance, string source)
    {
        if (chance <= 0 ||
            (LuckiestDrop.IsValid && chance >= LuckiestDrop.Chance))
        {
            return;
        }

        LuckiestDrop = new LuckiestDrop
        {
            ItemName = item.Name,
            Attempts = Math.Max(1, attempts),
            Chance = chance,
            Source = source
        };

        LuckiestDropChanged?.Invoke();
    }

    public void RestoreLuckiestDrop(LuckiestDrop? record)
    {
        LuckiestDrop = record ?? new LuckiestDrop();
    }


    // ============================================================
    // EQUIPMENT
    // ============================================================

    public Item? EquippedHead { get; private set; }
    public Item? EquippedBody { get; private set; }
    public Item? EquippedLegs { get; private set; }
    public Item? EquippedWeapon { get; private set; }
    public Item? EquippedShield { get; private set; }
    public Item? EquippedGloves { get; private set; }
    public Item? EquippedBoots { get; private set; }
    public Item? EquippedAmulet { get; private set; }
    public Item? EquippedRing { get; private set; }
    public Item? EquippedFood { get; private set; }

    public event Action? EquipmentChanged;


    // ============================================================
    // COMBAT INFORMATION
    // ============================================================

    // Current HP during combat.
    public int CurrentHP { get; set; }


    // ============================================================
    // COMBAT SPEED
    // ============================================================

    // Attack speed when fighting without a weapon.
    private const int UnarmedAttackSpeedTicks = 4;

    // Returns the player's current effective attack speed.
    public int GetAttackSpeedTicks()
    {
        return EquippedWeapon?.AttackSpeedTicks ??
            UnarmedAttackSpeedTicks;
    }

    public int GetEffectiveAttackLevel()
    {
        return Attack.Level +
            GetEquipmentAttackBonus();
    }

    public int GetEffectiveStrengthLevel()
    {
        return Strength.Level +
            GetEquipmentStrengthBonus();
    }

    public int GetEffectiveDefenseLevel()
    {
        return Defense.Level +
            GetEquipmentDefenseBonus();
    }

    public int GetMaxHP()
    {
        return Math.Max(
            1,
            HP.Level + GetEquipmentHPBonus());
    }

    public int GetCombatLevel()
    {
        return Math.Max(
            1,
            (GetEffectiveAttackLevel() +
             GetEffectiveDefenseLevel() +
             GetEffectiveStrengthLevel() +
             GetMaxHP()) / 4);
    }

    public int GetEquipmentAttackBonus()
    {
        return GetEquippedItems().Sum(item => item.AttackBonus);
    }

    public int GetEquipmentStrengthBonus()
    {
        return GetEquippedItems().Sum(item => item.StrengthBonus);
    }

    public int GetEquipmentDefenseBonus()
    {
        return GetEquippedItems().Sum(item => item.DefenseBonus);
    }

    public int GetEquipmentHPBonus()
    {
        return GetEquippedItems().Sum(item => item.HPBonus);
    }

    public int GetMaxHit()
    {
        return Math.Max(
            1,
            (GetEffectiveStrengthLevel() / 3) + 1);
    }

    public int GetTotalEquipmentBonus()
    {
        return GetEquippedItems().Sum(item =>
            item.AttackBonus +
            item.StrengthBonus +
            item.DefenseBonus +
            item.HPBonus);
    }

    public bool EquipItem(
        Item item)
    {
        if (item.Type != ItemType.Equipment ||
            item.EquipmentSlot == EquipmentSlot.None ||
            !MeetsEquipmentRequirement(item))
        {
            return false;
        }

        if (!Inventory.RemoveItem(item))
            return false;

        Item? previouslyEquipped =
            GetEquippedItem(item.EquipmentSlot);

        SetEquippedItem(
            item.EquipmentSlot,
            item);

        if (previouslyEquipped != null)
        {
            Inventory.AddItem(previouslyEquipped);
        }

        CurrentHP =
            Math.Min(
                CurrentHP,
                GetMaxHP());

        EquipmentChanged?.Invoke();

        return true;
    }

    public bool MeetsEquipmentRequirement(Item item)
    {
        return item.EquipmentSlot == EquipmentSlot.Weapon
            ? Attack.Level >= item.RequiredAttackLevel
            : Defense.Level >= item.RequiredDefenseLevel;
    }

    public string GetEquipmentRequirementText(Item item)
    {
        return item.EquipmentSlot == EquipmentSlot.Weapon
            ? $"⚔️ Attack level {item.RequiredAttackLevel}"
            : $"🛡️ Defense level {item.RequiredDefenseLevel}";
    }

    public bool UnequipItem(
        EquipmentSlot slot)
    {
        Item? item =
            GetEquippedItem(slot);

        if (item == null)
            return false;

        if (!Inventory.CanAddItem(item))
            return false;

        SetEquippedItem(
            slot,
            null);

        Inventory.AddItem(item);

        CurrentHP =
            Math.Min(
                CurrentHP,
                GetMaxHP());

        EquipmentChanged?.Invoke();

        return true;
    }

    public bool SelectFood(Item item)
    {
        if (item.Type != ItemType.Food ||
            item.HealingAmount <= 0 ||
            !Inventory.HasItem(item))
        {
            return false;
        }

        EquippedFood = item;
        EquipmentChanged?.Invoke();

        return true;
    }

    public void ClearEquippedFood()
    {
        if (EquippedFood == null)
            return;

        EquippedFood = null;
        EquipmentChanged?.Invoke();
    }

    public bool TryConsumeEquippedFood()
    {
        Item? food = EquippedFood;

        if (food == null ||
            CurrentHP >= GetMaxHP() ||
            !Inventory.RemoveItem(food))
        {
            return false;
        }

        CurrentHP = Math.Min(
            GetMaxHP(),
            CurrentHP + food.HealingAmount);

        if (!Inventory.HasItem(food))
        {
            EquippedFood = null;
        }

        EquipmentChanged?.Invoke();

        return true;
    }


    public void AutoEquipBestGear()
    {
        Item[] availableItems =
            Inventory.Items
                .Select(inventoryItem => inventoryItem.Item)
                .Concat(GetEquippedItems())
                .Where(item =>
                    item.Type == ItemType.Equipment &&
                    item.EquipmentSlot != EquipmentSlot.None &&
                    MeetsEquipmentRequirement(item))
                .Distinct()
                .ToArray();

        foreach (EquipmentSlot slot in Enum.GetValues<EquipmentSlot>())
        {
            if (slot is EquipmentSlot.None or EquipmentSlot.Food)
                continue;

            Item? bestItem =
                availableItems
                    .Where(item => item.EquipmentSlot == slot)
                    .OrderByDescending(item => slot == EquipmentSlot.Weapon
                        ? GetWeaponAutoEquipScore(item)
                        : (double)GetCombinedEquipmentBonus(item))
                    .ThenBy(item =>
                        slot == EquipmentSlot.Weapon
                            ? item.AttackSpeedTicks
                            : int.MaxValue)
                    .FirstOrDefault();

            Item? currentlyEquipped =
                GetEquippedItem(slot);

            if (bestItem == null ||
                bestItem == currentlyEquipped)
            {
                continue;
            }

            if (currentlyEquipped != null)
            {
                UnequipItem(slot);
            }

            EquipItem(bestItem);
        }

        Item? bestFood =
            Inventory.Items
                .Select(inventoryItem => inventoryItem.Item)
                .Where(item =>
                    item.Type == ItemType.Food &&
                    item.HealingAmount > 0)
                .OrderByDescending(item => item.HealingAmount)
                .ThenByDescending(item => item.Value)
                .FirstOrDefault();

        if (bestFood != null && bestFood != EquippedFood)
        {
            SelectFood(bestFood);
        }
    }

    public Item? GetEquippedItem(
        EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Head => EquippedHead,
            EquipmentSlot.Body => EquippedBody,
            EquipmentSlot.Legs => EquippedLegs,
            EquipmentSlot.Weapon => EquippedWeapon,
            EquipmentSlot.Shield => EquippedShield,
            EquipmentSlot.Gloves => EquippedGloves,
            EquipmentSlot.Boots => EquippedBoots,
            EquipmentSlot.Amulet => EquippedAmulet,
            EquipmentSlot.Ring => EquippedRing,
            EquipmentSlot.Food => EquippedFood,
            _ => null
        };
    }

    public IEnumerable<Skill> GetAllSkills()
    {
        return Skills.Concat(new[]
        {
            HP,
            Attack,
            Strength,
            Defense
        });
    }

    public void RestoreEquippedItem(
        EquipmentSlot slot,
        Item? item)
    {
        if (slot == EquipmentSlot.Food)
        {
            EquippedFood = item;
            return;
        }

        SetEquippedItem(slot, item);
    }

    private IEnumerable<Item> GetEquippedItems()
    {
        return new[]
            {
                EquippedHead,
                EquippedBody,
                EquippedLegs,
                EquippedWeapon,
                EquippedShield,
                EquippedGloves,
                EquippedBoots,
                EquippedAmulet,
                EquippedRing
            }
            .OfType<Item>();
    }

    private static int GetCombinedEquipmentBonus(
        Item item)
    {
        return item.AttackBonus +
            item.StrengthBonus +
            item.DefenseBonus +
            item.HPBonus;
    }

    private double GetWeaponAutoEquipScore(Item item)
    {
        int currentWeaponAttack = EquippedWeapon?.AttackBonus ?? 0;
        int currentWeaponStrength = EquippedWeapon?.StrengthBonus ?? 0;
        double attackLevel = Math.Max(1,
            Attack.Level + GetEquipmentAttackBonus() - currentWeaponAttack + item.AttackBonus);
        double strengthLevel = Math.Max(1,
            Strength.Level + GetEquipmentStrengthBonus() - currentWeaponStrength + item.StrengthBonus);
        double hitChance = Math.Clamp(attackLevel / (attackLevel + 50d), 0.05d, 0.95d);
        double maximumHit = Math.Max(1d, strengthLevel / 3d + 1d);
        double averageHit = (maximumHit + 1d) / 2d;
        return hitChance * averageHit / Math.Max(1, item.AttackSpeedTicks);
    }

    private void SetEquippedItem(
        EquipmentSlot slot,
        Item? item)
    {
        switch (slot)
        {
            case EquipmentSlot.Head:
                EquippedHead = item;
                break;
            case EquipmentSlot.Body:
                EquippedBody = item;
                break;
            case EquipmentSlot.Legs:
                EquippedLegs = item;
                break;
            case EquipmentSlot.Weapon:
                EquippedWeapon = item;
                break;
            case EquipmentSlot.Shield:
                EquippedShield = item;
                break;
            case EquipmentSlot.Gloves:
                EquippedGloves = item;
                break;
            case EquipmentSlot.Boots:
                EquippedBoots = item;
                break;
            case EquipmentSlot.Amulet:
                EquippedAmulet = item;
                break;
            case EquipmentSlot.Ring:
                EquippedRing = item;
                break;
        }
    }


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public Player()
    {
        // --------------------------------------------------------
        // NON-COMBAT SKILLS
        // --------------------------------------------------------

        Fishing = new Skill(
            "Fishing",
            "🎣",
            StartupDataCache.GetSkillActivities("Fishing"));

        Mining = new Skill(
            "Mining",
            "⛏️",
            StartupDataCache.GetSkillActivities("Mining"));

        Woodcutting = new Skill(
            "Woodcutting",
            "🪓",
            StartupDataCache.GetSkillActivities("Woodcutting"));

        Agility = new Skill(
            "Agility",
            "🏃",
            StartupDataCache.GetSkillActivities("Agility"));

        Thieving = new Skill(
            "Thieving",
            "🕵️",
            StartupDataCache.GetSkillActivities("Thieving"));

        Crafting = new Skill(
            "Crafting",
            "🧶",
            StartupDataCache.GetSkillActivities("Crafting"));

        Fletching = new Skill(
            "Fletching",
            "🏹",
            StartupDataCache.GetSkillActivities("Fletching"));

        Farming = new Skill(
            "Farming",
            "🌱",
            StartupDataCache.GetSkillActivities("Farming"));


        // --------------------------------------------------------
        // COMBAT SKILLS
        // --------------------------------------------------------

        HP = new Skill(
            "HP",
            "❤️",
            Array.Empty<SkillActivity>());

        HP.AddXP(
            ExperienceTable.GetXPForLevel(10));


        Attack = new Skill(
            "Attack",
            "⚔️",
            Array.Empty<SkillActivity>());


        Strength = new Skill(
            "Strength",
            "💪",
            Array.Empty<SkillActivity>());


        Defense = new Skill(
            "Defense",
            "🛡️",
            Array.Empty<SkillActivity>());


        // --------------------------------------------------------
        // GENERAL SKILLS PAGE
        // --------------------------------------------------------

        Skills = new List<Skill>
        {
            Fishing,
            Mining,
            Woodcutting,
            Agility,
            Thieving,
            Crafting,
            Fletching,
            Farming
        };


        // --------------------------------------------------------
        // INVENTORY
        // --------------------------------------------------------

        Inventory = new Inventory();

        CollectionLog = new CollectionLog();


        // --------------------------------------------------------
        // STARTING COMBAT STATE
        // --------------------------------------------------------

        CurrentHP = GetMaxHP();
    }
}
