namespace OSRSIdle;

public static class ExperienceTable
{
    // XP required for each level.
    // Index 1 = level 1
    // Index 2 = level 2
    // etc.

    private static readonly int[] XPTable = GenerateXPTable();

    private static int[] GenerateXPTable()
    {
        int[] table = new int[100];

        table[1] = 0;

        int accumulatedXP = 0;

        for (int level = 2; level <= 99; level++)
        {
            accumulatedXP += (int)Math.Floor((level - 1) + 300 * Math.Pow(2, (level - 1) / 7.0));
            table[level] = (int)Math.Floor(accumulatedXP / 4.0);
        }

        return table;
    }

    public static int GetLevel(double xp)
    {
        int level = 1;

        for (int i = 2; i <= 99; i++)
        {
            if (xp >= XPTable[i])
            {
                level = i;
            }
            else
            {
                break;
            }
        }

        return level;
    }

    public static int GetXPForLevel(int level)
    {
        if (level < 1)
            return 0;

        if (level > 99)
            return XPTable[99];

        return XPTable[level];
    }

    public static int GetXPToNextLevel(double xp)
    {
        int currentLevel = GetLevel(xp);

        if (currentLevel >= 99)
            return 0;

        return XPTable[currentLevel + 1] - (int)xp;
    }
}