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
        ["Meadow Imp"] = "A mischievous spirit of the tall grass that delights in leading travelers astray. Its tiny horns belie a knack for troublesome ambushes.",
        ["Marsh Crab"] = "A mud-caked crustacean that burrows beneath the marsh boardwalks. Its pearl-bright shell is tougher than it looks.",
        ["Sewer Spider"] = "A bloated spider nesting in the oldest drains beneath the frontier towns. Webs and stale water make its lair easy to miss until it is too late.",
        ["Bandit Scout"] = "A sharp-eyed outrider who maps every road before the bandit crews move in. It strikes quickly, then vanishes behind the next hedgerow.",
        ["Cinder Hound"] = "A coal-black hunting dog with embers glowing beneath its ribs. Its hot breath leaves singed pawprints across the trail.",
        ["Hill Troll"] = "A broad-shouldered troll that claims the high passes as its personal toll road. It keeps battered weapons and better-kept grudges.",
        ["Coral Serpent"] = "A reef serpent that coils around sunken ruins and shipwrecks. Its colorful scales hide a venomous bite and a patient hunter.",
        ["Ashen Mage"] = "A spellcaster cloaked in soot from a hundred burned battlefields. Ash swirls around its hands whenever it prepares a spell.",
        ["Ironback Boar"] = "A stubborn boar whose hide has hardened like hammered iron. It charges through fences, roots, and anything foolish enough to stand in its way.",
        ["Moonlit Revenant"] = "A dead wanderer that rises whenever moonlight touches its forgotten grave. It remembers no name, only the road back to the living.",
        ["Sand Warden"] = "A sentinel shaped from desert stone and windblown grit. It stands watch over buried roads long after the kingdoms that built them have vanished.",
        ["Grove Guardian"] = "An ancient protector woven from bark, moss, and stubborn roots. It wakes when careless axes threaten the heart of the grove.",
        ["Storm Drake"] = "A young dragon that nests where thunderheads gather over the borderlands. Lightning flickers between its teeth before every dive.",
        ["Obsidian Golem"] = "A walking statue carved from volcanic glass. Its polished body turns aside weak blows while its heavy fists grind stone to dust.",
        ["Dune Assassin"] = "A silent killer that disappears beneath shifting dunes between strikes. Only a ripple in the sand reveals where it is waiting.",
        ["Plague Bringer"] = "A masked herald carrying sickness from settlement to settlement. The bells on its belt ring long before its shadow reaches the road.",
        ["Tidecaller"] = "A sea-born mystic who commands the pull of the deep with a raised hand. Pools gather around its feet even under a cloudless sky.",
        ["Runic Sentinel"] = "A rune-inscribed construct built to guard a vault no map can find. Its glowing sigils flare whenever an intruder crosses the threshold.",
        ["Crystal Basilisk"] = "A basilisk with a hide of faceted crystal and a gaze like a shard of winter. Its lair glitters with the remains of failed hunters.",
        ["Infernal Knight"] = "A fallen knight armored in metal heated from within. Each swing leaves a brief orange scar across the ground.",
        ["Eclipse Beast"] = "A predator born in the instant daylight failed. Its silhouette swallows nearby color as it stalks beneath the dimmed sky.",
        ["Ancient Colossus"] = "A towering relic from an empire that measured roads in centuries. Dust falls from its shoulders whenever it takes a single step.",
        ["Vortex Wyrm"] = "A serpentine beast that coils inside violent spirals of wind. Its passage twists arrows, loose stones, and unlucky adventurers off course.",
        ["Fallen Champion"] = "A once-honored warrior who refused to accept defeat. Its dented shield still bears the crest of a kingdom that no longer exists.",
        ["Rift Devourer"] = "A hungry aberration that crawls out of cracks between worlds. It consumes magic first, leaving only cold silence behind.",
        ["Sunken Leviathan"] = "A sea giant sleeping beneath drowned temples and broken piers. When it surfaces, the tide rises with it.",
        ["Titan Warlord"] = "A battlefield titan who treats every valley as a campaign map. It collects the weapons of defeated armies and remembers every victory.",
        ["Astral Chimera"] = "A many-formed hunter stitched together from creatures of the starry void. Its shifting heads never agree on which direction the prey fled.",
        ["Abyss Monarch"] = "A crowned sovereign ruling the lightless space beneath the world. Its court is made of echoes, shadows, and creatures that bow only to fear.",
        ["The Crownless King"] = "A dethroned ruler who built an empire from the ruins of his own throne. He wears no crown because every foe who approaches becomes part of his legend."
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
