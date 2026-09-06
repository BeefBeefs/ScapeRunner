namespace OSRSIdle;

public static class ProgressionBonuses
{
    public static int SkillXpPercent(Skill skill) => Math.Min(10, skill.Level / 10);
    public static int CombatXpPercent(Player player, Enemy enemy) =>
        Math.Min(10, Math.Max(0, (player.GetCombatLevel() - enemy.CombatLevel) / 20));
    public static string Format(int percent) => percent <= 0 ? "" : $"+{percent}% XP bonus";
}
