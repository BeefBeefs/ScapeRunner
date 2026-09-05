namespace OSRSIdle;

public class Skill
{
    public string Name { get; set; }

    public string Icon { get; set; }

    public string IconImage => Name.ToLowerInvariant() switch
    {
        "fishing" => "skill_fishing.png",
        "mining" => "skill_mining.png",
        "woodcutting" => "skill_woodcutting.png",
        "agility" => "skill_agility.png",
        "thieving" => "skill_thieving.png",
        "crafting" => "skill_crafting.png",
        "fletching" => "skill_fletching.png",
        "farming" => "skill_farming.png",
        "hp" => "skill_hp.png",
        "attack" => "skill_attack.png",
        "strength" => "skill_strength.png",
        "defense" => "skill_defense.png",
        _ => string.Empty
    };

    public double XP { get; private set; }

    /// <summary>Total completed training actions, used for skilling-pet luck tracking.</summary>
    public long ActionsCompleted { get; private set; }

    public IReadOnlyList<SkillActivity> Activities { get; }

    public int Level
    {
        get
        {
            return ExperienceTable.GetLevel(XP);
        }
    }

    public Skill(
        string name,
        string icon,
        IReadOnlyList<SkillActivity> activities)
    {
        Name = name;
        Icon = icon;
        Activities = activities;
        XP = 0;
        ActionsCompleted = 0;
    }

    public void AddXP(double amount)
    {
        XP += amount;
    }

    public void RestoreXP(double xp)
    {
        XP = Math.Max(0, xp);
    }

    public void RecordAction(long count = 1)
    {
        ActionsCompleted = Math.Max(0, ActionsCompleted + count);
    }

    public void RestoreActions(long count)
    {
        ActionsCompleted = Math.Max(0, count);
    }
}
