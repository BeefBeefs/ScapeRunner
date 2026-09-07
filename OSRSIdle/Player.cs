namespace OSRSIdle;

public enum AutoEquipPriority
{
    AttackBonus,
    DefenseBonus,
    StrengthBonus,
    OverallBonus
}

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

    private int _equipmentNotificationDeferralDepth;
    private bool _equipmentNotificationPending;

    private bool _equipmentBonusCacheDirty = true;
    private int _cachedEquipmentAttackBonus;
    private int _cachedEquipmentStrengthBonus;
    private int _cachedEquipmentDefenseBonus;
    private int _cachedEquipmentHPBonus;


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
        EnsureEquipmentBonusCache();
        return _cachedEquipmentAttackBonus;
    }

    public int GetEquipmentStrengthBonus()
    {
        EnsureEquipmentBonusCache();
        return _cachedEquipmentStrengthBonus;
    }

    public int GetEquipmentDefenseBonus()
    {
        EnsureEquipmentBonusCache();
        return _cachedEquipmentDefenseBonus;
    }

    public int GetEquipmentHPBonus()
    {
        EnsureEquipmentBonusCache();
        return _cachedEquipmentHPBonus;
    }

    public int GetMaxHit()
    {
        return Math.Max(
            1,
            (GetEffectiveStrengthLevel() / 3) + 1);
    }

    public int GetTotalEquipmentBonus()
    {
        EnsureEquipmentBonusCache();
        return _cachedEquipmentAttackBonus +
            _cachedEquipmentStrengthBonus +
            _cachedEquipmentDefenseBonus +
            _cachedEquipmentHPBonus;
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

        Item? previouslyEquipped =
            GetEquippedItem(item.EquipmentSlot);

        if (previouslyEquipped != null &&
            !Inventory.CanReplaceItem(item, previouslyEquipped))
        {
            return false;
        }

        if (!Inventory.RemoveItem(item))
            return false;

        SetEquippedItem(
            item.EquipmentSlot,
            item);

        if (previouslyEquipped != null)
        {
            // CanReplaceItem above guarantees this succeeds without losing
            // the previously equipped item.
            _ = Inventory.AddItem(previouslyEquipped);
        }

        CurrentHP =
            Math.Min(
                CurrentHP,
                GetMaxHP());

        NotifyEquipmentChanged();

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

        NotifyEquipmentChanged();

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
        NotifyEquipmentChanged();

        return true;
    }

    public void ClearEquippedFood()
    {
        if (EquippedFood == null)
            return;

        EquippedFood = null;
        NotifyEquipmentChanged();
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

        NotifyEquipmentChanged();

        return true;
    }

    internal IDisposable DeferEquipmentNotifications()
    {
        _equipmentNotificationDeferralDepth++;
        return new EquipmentNotificationDeferral(this);
    }

    private void EndEquipmentNotificationDeferral()
    {
        if (_equipmentNotificationDeferralDepth <= 0)
            return;

        _equipmentNotificationDeferralDepth--;
        if (_equipmentNotificationDeferralDepth == 0 &&
            _equipmentNotificationPending)
        {
            _equipmentNotificationPending = false;
            EquipmentChanged?.Invoke();
        }
    }

    private void NotifyEquipmentChanged()
    {
        if (_equipmentNotificationDeferralDepth > 0)
        {
            _equipmentNotificationPending = true;
            return;
        }

        EquipmentChanged?.Invoke();
    }

    private sealed class EquipmentNotificationDeferral : IDisposable
    {
        private Player? _player;

        public EquipmentNotificationDeferral(Player player)
        {
            _player = player;
        }

        public void Dispose()
        {
            Player? player = Interlocked.Exchange(ref _player, null);
            player?.EndEquipmentNotificationDeferral();
        }
    }


    public void AutoEquipBestGear()
    {
        AutoEquipBestGear(AutoEquipPriority.OverallBonus);
    }

    public void AutoEquipBestGear(AutoEquipPriority priority)
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
                    .OrderByDescending(item =>
                        GetAutoEquipScore(item, priority))
                    .ThenByDescending(GetCombinedEquipmentBonus)
                    .ThenByDescending(item => item.Value)
                    .ThenBy(item => item.AttackSpeedTicks)
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

    public void ClearEquippedItems()
    {
        EquippedHead = null;
        EquippedBody = null;
        EquippedLegs = null;
        EquippedWeapon = null;
        EquippedShield = null;
        EquippedGloves = null;
        EquippedBoots = null;
        EquippedAmulet = null;
        EquippedRing = null;
        EquippedFood = null;
        _equipmentBonusCacheDirty = true;
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

    private static int GetAutoEquipScore(
        Item item,
        AutoEquipPriority priority)
    {
        return priority switch
        {
            AutoEquipPriority.AttackBonus => item.AttackBonus,
            AutoEquipPriority.DefenseBonus => item.DefenseBonus,
            AutoEquipPriority.StrengthBonus => item.StrengthBonus,
            _ => GetCombinedEquipmentBonus(item)
        };
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

        _equipmentBonusCacheDirty = true;
    }

    private void EnsureEquipmentBonusCache()
    {
        if (!_equipmentBonusCacheDirty)
            return;

        _cachedEquipmentAttackBonus =
            (EquippedHead?.AttackBonus ?? 0) +
            (EquippedBody?.AttackBonus ?? 0) +
            (EquippedLegs?.AttackBonus ?? 0) +
            (EquippedWeapon?.AttackBonus ?? 0) +
            (EquippedShield?.AttackBonus ?? 0) +
            (EquippedGloves?.AttackBonus ?? 0) +
            (EquippedBoots?.AttackBonus ?? 0) +
            (EquippedAmulet?.AttackBonus ?? 0) +
            (EquippedRing?.AttackBonus ?? 0);

        _cachedEquipmentStrengthBonus =
            (EquippedHead?.StrengthBonus ?? 0) +
            (EquippedBody?.StrengthBonus ?? 0) +
            (EquippedLegs?.StrengthBonus ?? 0) +
            (EquippedWeapon?.StrengthBonus ?? 0) +
            (EquippedShield?.StrengthBonus ?? 0) +
            (EquippedGloves?.StrengthBonus ?? 0) +
            (EquippedBoots?.StrengthBonus ?? 0) +
            (EquippedAmulet?.StrengthBonus ?? 0) +
            (EquippedRing?.StrengthBonus ?? 0);

        _cachedEquipmentDefenseBonus =
            (EquippedHead?.DefenseBonus ?? 0) +
            (EquippedBody?.DefenseBonus ?? 0) +
            (EquippedLegs?.DefenseBonus ?? 0) +
            (EquippedWeapon?.DefenseBonus ?? 0) +
            (EquippedShield?.DefenseBonus ?? 0) +
            (EquippedGloves?.DefenseBonus ?? 0) +
            (EquippedBoots?.DefenseBonus ?? 0) +
            (EquippedAmulet?.DefenseBonus ?? 0) +
            (EquippedRing?.DefenseBonus ?? 0);

        _cachedEquipmentHPBonus =
            (EquippedHead?.HPBonus ?? 0) +
            (EquippedBody?.HPBonus ?? 0) +
            (EquippedLegs?.HPBonus ?? 0) +
            (EquippedWeapon?.HPBonus ?? 0) +
            (EquippedShield?.HPBonus ?? 0) +
            (EquippedGloves?.HPBonus ?? 0) +
            (EquippedBoots?.HPBonus ?? 0) +
            (EquippedAmulet?.HPBonus ?? 0) +
            (EquippedRing?.HPBonus ?? 0);

        _equipmentBonusCacheDirty = false;
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
