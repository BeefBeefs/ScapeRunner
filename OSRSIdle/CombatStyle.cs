namespace OSRSIdle;

public enum CombatStyle
{
    Attack,
    Strength,
    Defense
}

public static class CombatStyleRules
{
    public static string DisplayName(this CombatStyle style) => style switch
    {
        CombatStyle.Attack => "Attack",
        CombatStyle.Strength => "Strength",
        CombatStyle.Defense => "Defense",
        _ => style.ToString()
    };

    public static string Description(this CombatStyle style) => style switch
    {
        CombatStyle.Attack => "+10% accuracy",
        CombatStyle.Strength => "+15% max hit",
        CombatStyle.Defense => "+15% defense, -10% damage taken",
        _ => string.Empty
    };
}
