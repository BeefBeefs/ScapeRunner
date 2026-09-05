namespace OSRSIdle;

public class Enemy
{
    private string? _spriteSlug;
    // ============================================================
    // BASIC INFORMATION
    // ============================================================

    public string Name { get; set; } = "";

    public string Icon { get; set; } = "";

    /// <summary>
    /// Short flavor text shown beneath the enemy portrait in combat and the
    /// Collection Log. Static enemy data populates this during startup.
    /// </summary>
    public string Description { get; set; } = "";

    public string IconImage
        => $"enemy_{SpriteSlug}.png";

    // Larger battle/detail variant. The compact icon remains available for
    // selection lists and other dense UI.
    public string LargeIconImage
        => $"enemy_{SpriteSlug}_64.png";

    private string SpriteSlug
    {
        get
        {
            if (_spriteSlug != null)
                return _spriteSlug;

            string slug = string.Concat(
                Name.ToLowerInvariant().Select(character =>
                    char.IsLetterOrDigit(character)
                        ? character
                        : '_'));

            while (slug.Contains("__", StringComparison.Ordinal))
                slug = slug.Replace("__", "_", StringComparison.Ordinal);

            _spriteSlug = slug.Trim('_');
            return _spriteSlug;
        }
    }

    public EnemyTier Tier { get; set; } = EnemyTier.Tier1;


    // ============================================================
    // SKILL REQUIREMENT
    // ============================================================

    public string? RequiredSkillName { get; set; }

    public string? RequiredSkillIcon { get; set; }

    public int RequiredSkillLevel { get; set; }


    // ============================================================
    // COMBAT STATS
    // ============================================================

    public CombatStats CombatStats { get; set; } =
        new CombatStats(
            hp: 1,
            attack: 1,
            strength: 1,
            defense: 1);


    // ============================================================
    // ATTACK SPEED
    // ============================================================

    // Measured in game ticks.
    public int AttackSpeedTicks { get; set; }

    public int CurrentHP { get; set; }


    // ============================================================
    // DROP TABLE
    // ============================================================

    public DropTable DropTable { get; set; } =
        new DropTable();


    // ============================================================
    // CONVENIENCE PROPERTIES
    // ============================================================

    public int HP
    {
        get => CombatStats.HP;
        set => CombatStats.HP = value;
    }

    public int Attack
    {
        get => CombatStats.Attack;
        set => CombatStats.Attack = value;
    }

    public int Strength
    {
        get => CombatStats.Strength;
        set => CombatStats.Strength = value;
    }

    public int Defense
    {
        get => CombatStats.Defense;
        set => CombatStats.Defense = value;
    }

    public int CombatLevel => Math.Max(
        1,
        (Attack + Defense + Strength + HP) / 4);
}
