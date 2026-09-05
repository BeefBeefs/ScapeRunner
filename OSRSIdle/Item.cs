namespace OSRSIdle;

// ============================================================
// ITEM TYPE
// ============================================================

public enum ItemType
{
    Material,
    Food,
    Currency,
    Equipment,
    Pet
}


// ============================================================
// EQUIPMENT SLOT
// ============================================================

public enum EquipmentSlot
{
    None,
    Head,
    Body,
    Legs,
    Weapon,
    Shield,
    Gloves,
    Boots,
    Amulet,
    Ring,
    Food
}


// ============================================================
// ITEM
// ============================================================

public class Item
{
    private string? _iconImage;
    // ============================================================
    // BASIC INFORMATION
    // ============================================================

    public string Name { get; set; } = "";

    public string Icon { get; set; } = "";

    // Short player-facing flavour text shown in the inventory details card.
    public string Description { get; set; } = "";

    public int UpgradeLevel { get; set; }
    public string? IconImageOverride { get; set; }

    public string IconImage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(IconImageOverride))
                return IconImageOverride;

            if (_iconImage != null)
                return _iconImage;

            string slug = string.Concat(
                Name.ToLowerInvariant().Select(character =>
                    char.IsLetterOrDigit(character)
                        ? character
                        : '_'));

            while (slug.Contains("__", StringComparison.Ordinal))
                slug = slug.Replace("__", "_", StringComparison.Ordinal);

            _iconImage = $"item_{slug.Trim('_')}.png";
            return _iconImage;
        }
    }


    // ============================================================
    // ITEM CLASSIFICATION
    // ============================================================

    public ItemType Type { get; set; } = ItemType.Material;

    // Miscellaneous resources are sellable through the inventory's
    // Sell Junk action.
    public bool IsJunk { get; set; }

    // EquipmentSlot.None is used for anything that isn't equipment.
    public EquipmentSlot EquipmentSlot { get; set; } = EquipmentSlot.None;


    // ============================================================
    // VALUE
    // ============================================================

    // Base value of the item in coins.
    public int Value { get; set; }


    // ============================================================
    // COMBAT BONUSES
    // ============================================================

    // Offensive accuracy / attack bonus.
    public int AttackBonus { get; set; }

    // Melee damage / strength bonus.
    public int StrengthBonus { get; set; }

    // Defensive bonus.
    public int DefenseBonus { get; set; }

    // Maximum HP bonus.
    public int HPBonus { get; set; }

    // Equipment prerequisites. Weapons use Attack; all other gear uses Defense.
    public int RequiredAttackLevel { get; set; }

    public int RequiredDefenseLevel { get; set; }

    // HP restored when this food is automatically eaten in combat.
    public int HealingAmount { get; set; }

    public int AttackSpeedTicks { get; set; } = 4;

    public Item CreateUpgrade()
    {
        if (UpgradeLevel >= 10)
            return this;

        int Bonus(int value) => value == 0 ? 0 : (int)Math.Ceiling(value * 1.02d);
        string baseName = Name;
        int marker = Name.LastIndexOf(" +", StringComparison.Ordinal);
        if (marker >= 0 && int.TryParse(Name[(marker + 2)..], out _))
            baseName = Name[..marker];

        return new Item
        {
            Name = $"{baseName} +{UpgradeLevel + 1}", Icon = Icon, Description = Description,
            IconImageOverride = IconImage, UpgradeLevel = UpgradeLevel + 1,
            Type = Type, IsJunk = IsJunk, EquipmentSlot = EquipmentSlot,
            Value = Value, AttackBonus = Bonus(AttackBonus),
            StrengthBonus = Bonus(StrengthBonus), DefenseBonus = Bonus(DefenseBonus),
            HPBonus = Bonus(HPBonus), RequiredAttackLevel = RequiredAttackLevel,
            RequiredDefenseLevel = RequiredDefenseLevel, HealingAmount = HealingAmount,
            AttackSpeedTicks = AttackSpeedTicks
        };
    }
}
