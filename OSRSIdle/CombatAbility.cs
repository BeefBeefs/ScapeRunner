namespace OSRSIdle;

public enum CombatAbility
{
    PreciseStrike,
    PowerStrike,
    Guard
}

public static class CombatAbilityRules
{
    public const int CooldownTicks = 30;

    public static CombatStyle RequiredStyle(this CombatAbility ability) => ability switch
    {
        CombatAbility.PreciseStrike => CombatStyle.Attack,
        CombatAbility.PowerStrike => CombatStyle.Strength,
        CombatAbility.Guard => CombatStyle.Defense,
        _ => CombatStyle.Attack
    };

    public static string DisplayName(this CombatAbility ability) => ability switch
    {
        CombatAbility.PreciseStrike => "Precise",
        CombatAbility.PowerStrike => "Power",
        CombatAbility.Guard => "Guard",
        _ => ability.ToString()
    };

    public static string Description(this CombatAbility ability) => ability switch
    {
        CombatAbility.PreciseStrike => "Next attack gets +25% accuracy",
        CombatAbility.PowerStrike => "Next hit deals 50% more damage",
        CombatAbility.Guard => "Next hit taken deals 50% less damage",
        _ => string.Empty
    };
}
