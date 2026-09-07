namespace OSRSIdle;

public enum EnemyTrait
{
    Armored,
    Accurate,
    Regenerative,
    Frenzied
}

public static class EnemyTraitRules
{
    private static readonly IReadOnlyList<IReadOnlyList<EnemyTrait>> RosterProfiles =
        new IReadOnlyList<EnemyTrait>[]
        {
            Array.Empty<EnemyTrait>(),
            new[] { EnemyTrait.Armored },
            new[] { EnemyTrait.Accurate },
            new[] { EnemyTrait.Regenerative },
            new[] { EnemyTrait.Frenzied },
            new[] { EnemyTrait.Armored, EnemyTrait.Accurate },
            new[] { EnemyTrait.Armored, EnemyTrait.Regenerative },
            new[] { EnemyTrait.Accurate, EnemyTrait.Frenzied },
            new[] { EnemyTrait.Regenerative, EnemyTrait.Frenzied },
            new[] { EnemyTrait.Armored, EnemyTrait.Accurate, EnemyTrait.Frenzied },
            new[] { EnemyTrait.Armored, EnemyTrait.Accurate, EnemyTrait.Regenerative }
        };

    /// <summary>
    /// Assigns a stable, area-spanning trait profile to the static enemy
    /// roster. Profiles are deliberately independent of raw stats so one
    /// trait, especially Regenerative, cannot dominate an entire late area.
    /// </summary>
    public static void AssignRosterTraits(IEnumerable<Enemy> enemies)
    {
        Enemy[] orderedEnemies = enemies
            .OrderBy(enemy => enemy.Tier)
            .ThenBy(enemy => enemy.CombatLevel)
            .ThenBy(enemy => enemy.Name)
            .ToArray();

        for (int index = 0; index < orderedEnemies.Length; index++)
        {
            IReadOnlyList<EnemyTrait> profile =
                RosterProfiles[index % RosterProfiles.Count];
            orderedEnemies[index].TraitOverride = profile;
        }
    }

    public static IReadOnlyList<EnemyTrait> For(Enemy enemy)
    {
        if (enemy.TraitOverride != null)
            return enemy.TraitOverride;

        List<EnemyTrait> traits = new();
        if (enemy.Defense >= 10 && enemy.Defense >= enemy.Attack * 1.25 && enemy.Defense >= enemy.Strength * 1.1)
            traits.Add(EnemyTrait.Armored);
        if (enemy.Attack >= 10 && enemy.Attack >= enemy.Defense * 1.25)
            traits.Add(EnemyTrait.Accurate);
        if (enemy.HP >= 100 && enemy.Tier >= EnemyTier.Tier4)
            traits.Add(EnemyTrait.Regenerative);
        if (enemy.AttackSpeedTicks <= 4 && enemy.Strength >= 30)
            traits.Add(EnemyTrait.Frenzied);
        return traits.Count == 0
            ? Array.Empty<EnemyTrait>()
            : Array.AsReadOnly(traits.ToArray());
    }

    public static string Name(EnemyTrait trait) => trait switch
    {
        EnemyTrait.Armored => "Armored",
        EnemyTrait.Accurate => "Accurate",
        EnemyTrait.Regenerative => "Regenerative",
        EnemyTrait.Frenzied => "Frenzied",
        _ => trait.ToString()
    };

    public static string Description(EnemyTrait trait) => trait switch
    {
        EnemyTrait.Armored => "Takes 15% less damage",
        EnemyTrait.Accurate => "Has +10% hit chance",
        EnemyTrait.Regenerative => "Regains 1% max HP when it lands an attack",
        EnemyTrait.Frenzied => "Attacks up to 10% faster",
        _ => string.Empty
    };

    public static Color Color(EnemyTrait trait) => trait switch
    {
        EnemyTrait.Armored => Microsoft.Maui.Graphics.Color.FromArgb("#6FA8DC"),
        EnemyTrait.Accurate => Microsoft.Maui.Graphics.Color.FromArgb("#FF7D7D"),
        EnemyTrait.Regenerative => Microsoft.Maui.Graphics.Color.FromArgb("#70D890"),
        EnemyTrait.Frenzied => Microsoft.Maui.Graphics.Color.FromArgb("#FFB347"),
        _ => Microsoft.Maui.Graphics.Colors.White
    };
}
