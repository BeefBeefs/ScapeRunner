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
    public static IReadOnlyList<EnemyTrait> For(Enemy enemy)
    {
        List<EnemyTrait> traits = new();
        if (enemy.Defense >= 10 && enemy.Defense >= enemy.Attack * 1.25 && enemy.Defense >= enemy.Strength * 1.1)
            traits.Add(EnemyTrait.Armored);
        if (enemy.Attack >= 10 && enemy.Attack >= enemy.Defense * 1.25)
            traits.Add(EnemyTrait.Accurate);
        if (enemy.HP >= 100 && enemy.Tier >= EnemyTier.Tier4)
            traits.Add(EnemyTrait.Regenerative);
        if (enemy.AttackSpeedTicks <= 4 && enemy.Strength >= 30)
            traits.Add(EnemyTrait.Frenzied);
        return traits;
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
        EnemyTrait.Regenerative => "Regains 1% max HP every 10 ticks",
        EnemyTrait.Frenzied => "Attacks 10% faster",
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
