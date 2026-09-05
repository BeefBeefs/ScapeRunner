namespace OSRSIdle;

public sealed class OfflineCombatSimulation
{
    private const int AutoFightRespawnTicks = 12;

    private readonly Player _player;
    private readonly Random _random = new();
    private readonly CombatStyle _combatStyle;

    private double _playerElapsedTicks;
    private double _enemyElapsedTicks;
    private int _autoEatCooldownTicks;

    private const int AutoEatCooldownDurationTicks = 4;

    private readonly Dictionary<Skill, int> _startingLevels;

    public Enemy Enemy { get; }

    public bool AutoFight { get; }

    public bool IsRespawning { get; private set; }

    public int RespawnTicksRemaining { get; private set; }

    public bool IsComplete { get; private set; }

    public bool PlayerDied { get; private set; }

    public long TicksProcessed { get; private set; }

    public long Kills { get; private set; }

    public double HPXPGranted { get; private set; }

    public double StyleXPGranted { get; private set; }

    public CombatStyle CombatStyle => _combatStyle;

    public Dictionary<Item, long> Loot { get; } = new();

    public Dictionary<Item, DropRarity> LootRarities { get; } = new();

    public HashSet<Item> NewCollectionItems { get; } = new();

    public OfflineCombatSimulation(
        Player player,
        OfflineActivitySaveData savedActivity)
    {
        _player = player;

        _startingLevels = new Dictionary<Skill, int>
        {
            [player.HP] = player.HP.Level,
            [player.Attack] = player.Attack.Level,
            [player.Strength] = player.Strength.Level,
            [player.Defense] = player.Defense.Level
        };

        Enemy = StartupDataCache.FindEnemy(savedActivity.ActivityName)
            ?? throw new InvalidDataException(
                $"Unknown saved enemy '{savedActivity.ActivityName}'.");

        AutoFight = savedActivity.IsAutoFight;
        IsRespawning = savedActivity.IsRespawning;
        RespawnTicksRemaining = Math.Max(0, savedActivity.RespawnTicksRemaining);

        if (!Enum.TryParse(savedActivity.CombatStyle, out _combatStyle))
        {
            _combatStyle = CombatStyle.Attack;
        }

        Enemy.CurrentHP = savedActivity.EnemyCurrentHP > 0
            ? Math.Min(savedActivity.EnemyCurrentHP, Enemy.HP)
            : Enemy.HP;

        _playerElapsedTicks = Math.Clamp(
            savedActivity.PlayerAttackProgress,
            0,
            1) * Math.Max(1, _player.GetAttackSpeedTicks());

        _enemyElapsedTicks = Math.Clamp(
            savedActivity.EnemyAttackProgress,
            0,
            1) * Math.Max(1, Enemy.AttackSpeedTicks);

        _autoEatCooldownTicks = Math.Max(
            0,
            savedActivity.AutoEatCooldownTicks);
    }

    public void Advance(long ticks)
    {
        for (long tick = 0; tick < ticks && !IsComplete; tick++)
        {
            TicksProcessed++;

            if (IsRespawning)
            {
                AdvanceRespawn();
                continue;
            }

            _playerElapsedTicks++;
            _enemyElapsedTicks++;

            AdvanceAutoEatTick();

            if (_playerElapsedTicks >= _player.GetAttackSpeedTicks())
            {
                _playerElapsedTicks -= _player.GetAttackSpeedTicks();
                PerformPlayerAttack();

                if (IsComplete || IsRespawning)
                    continue;
            }

            if (_enemyElapsedTicks >= Enemy.AttackSpeedTicks)
            {
                _enemyElapsedTicks -= Enemy.AttackSpeedTicks;
                PerformEnemyAttack();
            }
        }
    }

    public IReadOnlyList<OfflineLevelUp> GetLevelUps()
    {
        return _startingLevels
            .Where(entry => entry.Key.Level > entry.Value)
            .Select(entry => new OfflineLevelUp(
                entry.Key.Name,
                entry.Value,
                entry.Key.Level))
            .ToList();
    }

    private void AdvanceRespawn()
    {
        RespawnTicksRemaining--;

        if (RespawnTicksRemaining > 0)
            return;

        IsRespawning = false;
        Enemy.CurrentHP = Enemy.HP;
        _playerElapsedTicks = 0;
        _enemyElapsedTicks = 0;
    }

    private void PerformPlayerAttack()
    {
        if (!RollAccuracy(_player.GetEffectiveAttackLevel(), Enemy.Defense))
            return;

        int damage = RollDamage(_player.GetEffectiveStrengthLevel());

        Enemy.CurrentHP = Math.Max(0, Enemy.CurrentHP - damage);
        _player.HP.AddXP(damage);
        HPXPGranted += damage;

        switch (_combatStyle)
        {
            case CombatStyle.Attack:
                _player.Attack.AddXP(damage * 4);
                StyleXPGranted += damage * 4;
                break;
            case CombatStyle.Strength:
                _player.Strength.AddXP(damage * 4);
                StyleXPGranted += damage * 4;
                break;
            case CombatStyle.Defense:
                _player.Defense.AddXP(damage * 4);
                StyleXPGranted += damage * 4;
                break;
        }

        if (Enemy.CurrentHP <= 0)
            HandleEnemyDefeated();
    }

    private void PerformEnemyAttack()
    {
        if (!RollAccuracy(Enemy.Attack, _player.GetEffectiveDefenseLevel()))
            return;

        int damage = RollDamage(Enemy.Strength);

        _player.CurrentHP = Math.Max(0, _player.CurrentHP - damage);

        TryAutoEat();

        if (_player.CurrentHP <= 0)
        {
            PlayerDied = true;
            IsComplete = true;
        }
    }

    private void HandleEnemyDefeated()
    {
        List<LootResult> killLoot = new();

        foreach (Drop drop in Enemy.DropTable.Drops)
        {
            if (_random.NextDouble() > drop.Chance)
                continue;

            int quantity = _random.Next(drop.MinQuantity, drop.MaxQuantity + 1);

            if (!_player.Inventory.AddItem(drop.Item, quantity))
                continue;

            // Keep offline combat's persistent luck tracking consistent with
            // live combat. Without this, rare drops earned while away appear
            // in loot but can never replace the Home-screen record.
            _player.RecordLuckiestDrop(
                drop.Item,
                _player.CollectionLog.GetKillCount(Enemy) + 1,
                drop.Chance,
                $"{Enemy.Name} kills");

            if (!_player.CollectionLog.HasReceivedDrop(Enemy, drop.Item))
            {
                NewCollectionItems.Add(drop.Item);
            }

            killLoot.Add(new LootResult(
                drop.Item,
                quantity,
                drop.Chance,
                drop.Rarity));

            Loot.TryGetValue(drop.Item, out long currentQuantity);
            Loot[drop.Item] = currentQuantity + quantity;
            LootRarities[drop.Item] = drop.Rarity;
        }

        Kills++;
        _player.CollectionLog.RecordKill(Enemy);

        _player.CollectionLog.RecordDrops(
            Enemy,
            killLoot);

        if (!AutoFight)
        {
            IsComplete = true;
            return;
        }

        IsRespawning = true;
        RespawnTicksRemaining = AutoFightRespawnTicks;
        _playerElapsedTicks = 0;
        _enemyElapsedTicks = 0;
    }

    private void AdvanceAutoEatTick()
    {
        if (_autoEatCooldownTicks > 0)
        {
            _autoEatCooldownTicks--;
        }

        TryAutoEat();
    }

    private bool TryAutoEat()
    {
        if (_autoEatCooldownTicks > 0 || !_player.ShouldAutoEat())
            return false;

        if (!_player.TryConsumeEquippedFood())
            return false;

        _autoEatCooldownTicks = AutoEatCooldownDurationTicks;
        return true;
    }

    private bool RollAccuracy(int attackLevel, int defenseLevel)
    {
        if (attackLevel <= 0 && defenseLevel <= 0)
            return true;

        double chance = (double)attackLevel / (attackLevel + defenseLevel);
        return _random.NextDouble() < Math.Clamp(chance, 0.05, 0.95);
    }

    private int RollDamage(int strengthLevel)
    {
        int maximumHit = Math.Max(1, strengthLevel / 3 + 1);
        return _random.Next(1, maximumHit + 1);
    }
}
