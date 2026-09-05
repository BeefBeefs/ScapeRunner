namespace OSRSIdle;

public class CombatStats
{
    // ============================================================
    // COMBAT STATS
    // ============================================================

    public int HP { get; set; }

    public int Attack { get; set; }

    public int Strength { get; set; }

    public int Defense { get; set; }


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public CombatStats(
        int hp,
        int attack,
        int strength,
        int defense)
    {
        HP = hp;
        Attack = attack;
        Strength = strength;
        Defense = defense;
    }
}