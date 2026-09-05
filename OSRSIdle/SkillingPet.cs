namespace OSRSIdle;

public class SkillingPet
{
    public string SkillName { get; init; } = "";

    public string SkillIcon { get; init; } = "";

    public Item Item { get; init; } = null!;

    public double MinimumDropDenominator { get; init; } = 1_000;
}

public static class SkillingPetData
{
    public static List<SkillingPet> AllPets { get; } = new()
    {
        new SkillingPet { SkillName = "Fishing", SkillIcon = "🎣", Item = ItemData.Heron },
        new SkillingPet { SkillName = "Mining", SkillIcon = "⛏️", Item = ItemData.Rocky },
        new SkillingPet { SkillName = "Woodcutting", SkillIcon = "🪓", Item = ItemData.Beaver },
        new SkillingPet { SkillName = "Agility", SkillIcon = "🏃", Item = ItemData.Squirrel },
        new SkillingPet { SkillName = "Thieving", SkillIcon = "🕵️", Item = ItemData.Raccoon, MinimumDropDenominator = 10_000 },
        new SkillingPet { SkillName = "Crafting", SkillIcon = "🧶", Item = ItemData.Golem, MinimumDropDenominator = 10_000 },
        new SkillingPet { SkillName = "Fletching", SkillIcon = "🏹", Item = ItemData.ArrowEagle, MinimumDropDenominator = 10_000 },
        new SkillingPet { SkillName = "Farming", SkillIcon = "🌱", Item = ItemData.Tangleroot, MinimumDropDenominator = 10_000 }
    };

    public static SkillingPet? GetForSkill(
        Skill skill)
    {
        return AllPets.FirstOrDefault(
            pet => pet.SkillName == skill.Name);
    }

    public static double GetDropDenominator(
        SkillActivity activity)
    {
        return GetDropDenominator(
            null,
            activity);
    }

    public static double GetDropDenominator(
        SkillingPet? pet,
        SkillActivity activity)
    {
        double xpMultiplier =
            Math.Max(
                1,
                activity.XP / 25d);

        return Math.Max(
            pet?.MinimumDropDenominator ?? 1_000,
            200_000 /
            Math.Pow(
                xpMultiplier,
                2.3));
    }
}
