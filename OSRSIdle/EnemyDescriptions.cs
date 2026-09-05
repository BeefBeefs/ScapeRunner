namespace OSRSIdle;

/// <summary>
/// Supplies concise flavor text for the complete enemy roster. Keeping this
/// separate from combat data avoids making the drop/stat definitions harder
/// to maintain while ensuring newly added enemies still receive a description.
/// </summary>
public static class EnemyDescriptions
{
    private static readonly Dictionary<string, string> Specific = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Chicken"] = "A common farm bird with surprising courage. Its feathers and meat are useful to a prepared adventurer.",
        ["Big Chicken"] = "A barnyard terror swollen to monstrous size. It guards its flock with furious pecks and talons.",
        ["Moss-Covered Goblin"] = "A small goblin hidden beneath damp moss and stolen armor. It lurks along the woodland paths.",
        ["Cave Rat"] = "A pale scavenger from the deep tunnels. Its chittering warns of vermin gathering in the dark.",
        ["Gloomblood Bat"] = "A nocturnal bat whose veins glow with cursed crimson light. It hunts silently beneath the swamp canopy.",
        ["Swamp Witch's Familiar"] = "A twisted companion bound to an old marsh witch. It carries hexes through the mist on crooked wings.",
        ["Feral Dryad"] = "A woodland spirit driven wild by ancient blight. Thorn and root answer its every angry gesture.",
        ["Grave Knight"] = "An armored sentinel who refuses to leave its forgotten tomb. Rust cannot dull the oath in its dead heart.",
        ["Abyssal Toad"] = "A bloated toad touched by the abyss. Its croak bends the shadows around the fetid pools.",
        ["Bloodmoon Cultist"] = "A devoted servant of the bloodmoon rite. Strange prayers have sharpened both its blade and its fanaticism.",
        ["Ironfang Werewolf"] = "A werewolf with metal-hard fangs and a hunter's patience. It prowls beneath the moon for warm prey.",
        ["Crypt Manticore"] = "A tomb-dwelling beast with a lion's fury and a scorpion's tail. Its ancient hoard lies behind poisoned spines.",
        ["Bone Collector"] = "A gaunt keeper of ossuaries who gathers every bone it can find. Its rattling armor hides a tireless will.",
        ["Thorn-Crowned Treant"] = "An elder tree spirit crowned in living briars. Every step shakes loose roots that grasp at intruders.",
        ["Dreadwing Harpy"] = "A shrieking predator that nests on windswept cliffs. Its shadow arrives before its razor talons.",
        ["Void-Tongued Warlock"] = "A sorcerer who traded speech for whispers from the void. Its spells leave holes in the air where sound once lived.",
        ["Frostfang Wyrm"] = "An icebound wyrm that coils beneath the frozen peaks. Its breath turns steel, stone, and courage brittle.",
        ["Emperor of the Hollow"] = "The deathless ruler of a kingdom buried beneath the earth. Empty eyes still command legions of the lost.",
        ["The Starved God"] = "A forgotten god awakened by centuries of hunger. Its presence drains warmth from every living thing nearby.",
        ["Malakar, the World-Eater"] = "A colossal devourer whose appetite reaches beyond kingdoms. Legends say even mountains vanish beneath its jaws.",
        ["Goblin Warlord"] = "The iron-fisted champion of the goblin clans. It dreams of marching its ragged army across every green field.",
        ["Swamp King"] = "An ancient monarch of mud and drowned roots. The waters rise whenever it lifts its massive crown.",
        ["Frostmaw Leviathan"] = "A glacier-born leviathan armored in blue ice. Its roar echoes through the frozen mines like an avalanche.",
        ["The Molten Colossus"] = "A walking mountain of black stone and lava. Each blow leaves the battlefield glowing with molten cracks.",
        ["Sandsoul Pharaoh"] = "A desert sovereign animated by restless sands. Its buried tombs awaken at the sound of its golden staff.",
        ["The Voidcaller"] = "A robed herald who calls creatures from beyond the stars. Purple fire swirls wherever its ritual is completed.",
        ["Aethereal Sovereign"] = "A dragonlike ruler of the astral dark. It bends floating ruins and broken moons to its will.",
    };

    public static void Apply(IEnumerable<Enemy> enemies)
    {
        foreach (Enemy enemy in enemies)
        {
            if (Specific.TryGetValue(enemy.Name, out string? description))
            {
                enemy.Description = description;
                continue;
            }

            string region = enemy.Tier switch
            {
                EnemyTier.Tier1 => "the frontier",
                EnemyTier.Tier2 => "the haunted wilds",
                EnemyTier.Tier3 => "the perilous borderlands",
                EnemyTier.Tier4 => "the deep realms",
                EnemyTier.Tier5 => "the ancient kingdoms",
                EnemyTier.Tier6 => "the outer reaches",
                _ => "the umbral expanse"
            };

            enemy.Description = $"{enemy.Name} is a dangerous foe prowling {region}. Its name is spoken cautiously by adventurers who have crossed its path.";
        }
    }
}
