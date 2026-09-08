namespace OSRSIdle;

/// <summary>
/// Shared combat calculations used by both the live and offline simulators.
/// Keeping these rules in one place prevents idle catch-up from drifting away
/// from the rules shown during an active fight.
/// </summary>
public static class CombatRules
{
    public static int GetEnemyAttackSpeedTicks(Enemy enemy)
    {
        return Math.Max(
            1,
            enemy.Traits.Contains(EnemyTrait.Frenzied)
                ? (int)Math.Floor(enemy.AttackSpeedTicks * 0.9d)
                : enemy.AttackSpeedTicks);
    }

    public static int GetEnemyAccuracyLevel(Enemy enemy)
    {
        return enemy.Traits.Contains(EnemyTrait.Accurate)
            ? (int)Math.Ceiling(enemy.Attack * 1.1d)
            : enemy.Attack;
    }

    public static int GetEnemyDefenseLevel(Enemy enemy)
    {
        return enemy.Traits.Contains(EnemyTrait.Armored)
            ? (int)Math.Ceiling(enemy.Defense * 1.15d)
            : enemy.Defense;
    }

    public static int ReducePlayerDamage(Enemy enemy, int damage)
    {
        return enemy.Traits.Contains(EnemyTrait.Armored)
            ? Math.Max(1, (int)Math.Floor(damage * 0.85d))
            : damage;
    }

    public static void ApplyRegeneration(Enemy enemy)
    {
        if (enemy.Traits.Contains(EnemyTrait.Regenerative))
        {
            enemy.CurrentHP = Math.Min(
                enemy.HP,
                enemy.CurrentHP + Math.Max(1, enemy.HP / 100));
        }
    }

    public static double GetCombatXpMultiplier(Player player, Enemy enemy)
    {
        return 1d + ProgressionBonuses.CombatXpPercent(player, enemy) / 100d;
    }

    public static int GetPlayerAttackLevel(Player player, CombatStyle style)
    {
        int level = player.GetEffectiveAttackLevel();
        return style == CombatStyle.Attack ? (int)Math.Ceiling(level * 1.10d) : level;
    }

    public static int GetPlayerStrengthLevel(Player player, CombatStyle style)
    {
        int level = player.GetEffectiveStrengthLevel();
        return style == CombatStyle.Strength ? (int)Math.Ceiling(level * 1.15d) : level;
    }

    public static int GetPlayerDefenseLevel(Player player, CombatStyle style)
    {
        int level = player.GetEffectiveDefenseLevel();
        return style == CombatStyle.Defense ? (int)Math.Ceiling(level * 1.15d) : level;
    }

    public static int ReduceIncomingDamage(CombatStyle style, int damage)
    {
        return style == CombatStyle.Defense
            ? Math.Max(0, (int)Math.Floor(damage * 0.90d))
            : damage;
    }

    public static bool IsWeakTo(Enemy enemy, CombatStyle style) =>
        enemy.Weakness == style;

    public static int GetWeaknessAccuracyLevel(Enemy enemy, CombatStyle style, int attackLevel)
    {
        return IsWeakTo(enemy, style)
            ? (int)Math.Ceiling(attackLevel * 1.25d)
            : attackLevel;
    }

    public static int GetWeaknessDamage(Enemy enemy, CombatStyle style, int damage)
    {
        return IsWeakTo(enemy, style)
            ? Math.Max(1, (int)Math.Ceiling(damage * 1.25d))
            : damage;
    }

    public static double GetSkillXpMultiplier(Skill skill)
    {
        return GetSkillXpMultiplier(skill.Level);
    }

    public static double GetSkillXpMultiplier(int skillLevel)
    {
        return 1d + Math.Min(10, Math.Max(1, skillLevel) / 10) / 100d;
    }

    public static double GetDropChance(Player player, double baseChance)
    {
        return Math.Clamp(
            baseChance * (1d + player.GlobalDropBoostPercent / 100d),
            0d,
            1d);
    }
}
