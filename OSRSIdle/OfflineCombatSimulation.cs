namespace OSRSIdle;

public sealed class OfflineCombatSimulation
{
    private const int AutoFightRespawnTicks = 12;

    private readonly Player _player;
    private readonly Random _random = new();
    private readonly CombatStyle _combatStyle;
    private readonly int _playerAttackTicks;
    private readonly int _enemyAttackTicks;
    private readonly int _enemyAccuracyLevel;
    private readonly int _enemyDefenseLevel;
    private readonly bool _enemyIsArmored;
    private readonly bool _enemyIsRegenerative;
    private readonly int _startingKillCount;
    private readonly List<LootResult> _killLoot;
    private readonly HashSet<Item> _undiscoveredDrops = new();
    private readonly HashSet<Item> _unrecordedEnemyDrops = new();

    private double _playerElapsedTicks;
    private double _enemyElapsedTicks;
    private int _autoEatCooldownTicks;
    private int _hpLevel;
    private int _attackLevel;
    private int _strengthLevel;
    private int _defenseLevel;
    private int _effectiveAttackLevel;
    private int _effectiveStrengthLevel;
    private int _effectiveDefenseLevel;
    private int _maximumHit;
    private int _maximumHP;
    private double _combatXpMultiplier;
    private double _dropChanceMultiplier;
    private double _nextHPLevelXP;
    private double _nextStyleLevelXP;
    private long _committedKills;

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
        RespawnTicksRemaining = DebugSettings.IsInstakillEnabled
            ? 1
            : Math.Max(0, savedActivity.RespawnTicksRemaining);

        if (!Enum.TryParse(savedActivity.CombatStyle, out _combatStyle))
        {
            _combatStyle = CombatStyle.Attack;
        }

        _playerAttackTicks = Math.Max(1, _player.GetAttackSpeedTicks());
        _enemyAttackTicks = CombatRules.GetEnemyAttackSpeedTicks(Enemy);
        _enemyAccuracyLevel = CombatRules.GetEnemyAccuracyLevel(Enemy);
        _enemyDefenseLevel = CombatRules.GetEnemyDefenseLevel(Enemy);
        _enemyIsArmored = Enemy.Traits.Contains(EnemyTrait.Armored);
        _enemyIsRegenerative = Enemy.Traits.Contains(EnemyTrait.Regenerative);
        _startingKillCount = _player.CollectionLog.GetKillCount(Enemy);
        _killLoot = new List<LootResult>(Enemy.DropTable.Drops.Count);

        foreach (Drop drop in Enemy.DropTable.Drops)
        {
            if (!_player.CollectionLog.HasReceivedDrop(Enemy, drop.Item))
                _undiscoveredDrops.Add(drop.Item);

            if (!_player.CollectionLog.HasRecordedDrop(Enemy, drop.Item))
                _unrecordedEnemyDrops.Add(drop.Item);
        }

        _hpLevel = _player.HP.Level;
        _attackLevel = _player.Attack.Level;
        _strengthLevel = _player.Strength.Level;
        _defenseLevel = _player.Defense.Level;
        _nextHPLevelXP = GetNextLevelXP(_hpLevel);
        _nextStyleLevelXP = GetNextLevelXP(GetStyleSkill().Level);
        RecalculatePlayerCombatValues();
        RefreshDropChanceMultiplier();

        Enemy.CurrentHP = savedActivity.EnemyCurrentHP > 0
            ? Math.Min(savedActivity.EnemyCurrentHP, Enemy.HP)
            : Enemy.HP;

        _playerElapsedTicks = Math.Clamp(
            savedActivity.PlayerAttackProgress,
            0,
            1) * _playerAttackTicks;

        _enemyElapsedTicks = Math.Clamp(
            savedActivity.EnemyAttackProgress,
            0,
            1) * _enemyAttackTicks;

        _autoEatCooldownTicks = Math.Max(
            0,
            savedActivity.AutoEatCooldownTicks);
    }

    public void Advance(long ticks)
    {
        Advance(ticks, CancellationToken.None);
    }

    public void Advance(long ticks, CancellationToken cancellationToken)
    {
        long remainingTicks = Math.Max(0, ticks);

        try
        {
            // Loot and auto-eating can otherwise schedule thousands of UI and
            // save refreshes during a long catch-up. The underlying state stays
            // current while listeners receive consolidated notifications.
            using IDisposable equipmentNotifications =
                _player.DeferEquipmentNotifications();
            using IDisposable inventoryNotifications =
                _player.Inventory.DeferNotifications();

            while (remainingTicks > 0 &&
                   !IsComplete &&
                   !cancellationToken.IsCancellationRequested)
            {
                if (IsRespawning)
                {
                    long respawnTicks = Math.Min(
                        remainingTicks,
                        Math.Max(1, RespawnTicksRemaining));

                    TicksProcessed += respawnTicks;
                    remainingTicks -= respawnTicks;
                    RespawnTicksRemaining -= (int)respawnTicks;

                    if (RespawnTicksRemaining > 0)
                        continue;

                    IsRespawning = false;
                    Enemy.CurrentHP = Enemy.HP;
                    _playerElapsedTicks = 0;
                    _enemyElapsedTicks = 0;
                    continue;
                }

                long ticksToPlayerAttack = TicksUntilBoundary(
                    _playerAttackTicks,
                    _playerElapsedTicks);
                long ticksToEnemyAttack = TicksUntilBoundary(
                    _enemyAttackTicks,
                    _enemyElapsedTicks);

                long ticksToAutoEat = GetTicksUntilAutoEat();
                long ticksThisStep = Math.Min(
                    remainingTicks,
                    Math.Min(
                        ticksToPlayerAttack,
                        Math.Min(ticksToEnemyAttack, ticksToAutoEat)));

                // There is always at least one future event while combat is
                // active, but keep this guard defensive against future changes.
                if (ticksThisStep <= 0)
                    ticksThisStep = 1;

                _playerElapsedTicks += ticksThisStep;
                _enemyElapsedTicks += ticksThisStep;
                TicksProcessed += ticksThisStep;
                remainingTicks -= ticksThisStep;

                AdvanceAutoEatTicks(ticksThisStep);

                if (_playerElapsedTicks >= _playerAttackTicks)
                {
                    _playerElapsedTicks -= _playerAttackTicks;
                    PerformPlayerAttack();

                    if (IsComplete || IsRespawning)
                        continue;
                }

                if (_enemyElapsedTicks >= _enemyAttackTicks)
                {
                    _enemyElapsedTicks -= _enemyAttackTicks;
                    PerformEnemyAttack();
                }
            }
        }
        finally
        {
            CommitKillCount();
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

    private Skill GetStyleSkill()
    {
        return _combatStyle switch
        {
            CombatStyle.Strength => _player.Strength,
            CombatStyle.Defense => _player.Defense,
            _ => _player.Attack
        };
    }

    private static double GetNextLevelXP(int level)
    {
        return level >= 99
            ? double.PositiveInfinity
            : ExperienceTable.GetXPForLevel(level + 1);
    }

    private void RefreshLevelsAfterExperience()
    {
        bool levelsChanged = false;

        if (_player.HP.XP >= _nextHPLevelXP)
        {
            _hpLevel = _player.HP.Level;
            _nextHPLevelXP = GetNextLevelXP(_hpLevel);
            levelsChanged = true;
        }

        Skill styleSkill = GetStyleSkill();
        if (styleSkill.XP >= _nextStyleLevelXP)
        {
            int level = styleSkill.Level;
            switch (_combatStyle)
            {
                case CombatStyle.Strength:
                    _strengthLevel = level;
                    break;
                case CombatStyle.Defense:
                    _defenseLevel = level;
                    break;
                default:
                    _attackLevel = level;
                    break;
            }

            _nextStyleLevelXP = GetNextLevelXP(level);
            levelsChanged = true;
        }

        if (levelsChanged)
            RecalculatePlayerCombatValues();
    }

    private void RecalculatePlayerCombatValues()
    {
        _effectiveAttackLevel =
            _attackLevel + _player.GetEquipmentAttackBonus();
        _effectiveStrengthLevel =
            _strengthLevel + _player.GetEquipmentStrengthBonus();
        _effectiveDefenseLevel =
            _defenseLevel + _player.GetEquipmentDefenseBonus();
        _maximumHit = Math.Max(1, _effectiveStrengthLevel / 3 + 1);
        _maximumHP = Math.Max(
            1,
            _hpLevel + _player.GetEquipmentHPBonus());
        _combatXpMultiplier =
            CombatRules.GetCombatXpMultiplier(_player, Enemy);
    }

    private void RefreshDropChanceMultiplier()
    {
        _dropChanceMultiplier =
            1d + _player.GlobalDropBoostPercent / 100d;
    }

    private static long TicksUntilBoundary(
        int boundary,
        double elapsed)
    {
        return Math.Max(
            1,
            (long)Math.Ceiling(boundary - elapsed));
    }

    private long GetTicksUntilAutoEat()
    {
        if (_autoEatCooldownTicks > 0)
            return _autoEatCooldownTicks;

        return ShouldAutoEat() &&
               _player.EquippedFood != null
            ? 1
            : long.MaxValue;
    }

    private bool ShouldAutoEat()
    {
        if (_player.CurrentHP <= 0 || _player.CurrentHP >= _maximumHP)
            return false;

        int thresholdHP = (int)Math.Ceiling(
            _maximumHP * _player.AutoEatThresholdPercent / 100d);

        return _player.CurrentHP <= thresholdHP;
    }

    private void AdvanceAutoEatTicks(long ticks)
    {
        if (_autoEatCooldownTicks > 0)
        {
            _autoEatCooldownTicks = Math.Max(
                0,
                _autoEatCooldownTicks - (int)Math.Min(ticks, int.MaxValue));
        }

        if (_autoEatCooldownTicks == 0)
            TryAutoEat();
    }

    private void PerformPlayerAttack()
    {
        if (!DebugSettings.IsInstakillEnabled &&
            !RollAccuracy(
                _effectiveAttackLevel,
                _enemyDefenseLevel))
            return;

        int damage = DebugSettings.IsInstakillEnabled
            ? Enemy.CurrentHP
            : _random.Next(1, _maximumHit + 1);

        if (_enemyIsArmored)
        {
            damage = Math.Max(
                1,
                (int)Math.Floor(damage * 0.85d));
        }

        Enemy.CurrentHP = Math.Max(0, Enemy.CurrentHP - damage);
        double hpXP = damage * _combatXpMultiplier;
        _player.HP.AddXP(hpXP);
        HPXPGranted += hpXP;

        switch (_combatStyle)
        {
            case CombatStyle.Attack:
                _player.Attack.AddXP(damage * 4 * _combatXpMultiplier);
                StyleXPGranted += damage * 4 * _combatXpMultiplier;
                break;
            case CombatStyle.Strength:
                _player.Strength.AddXP(damage * 4 * _combatXpMultiplier);
                StyleXPGranted += damage * 4 * _combatXpMultiplier;
                break;
            case CombatStyle.Defense:
                _player.Defense.AddXP(damage * 4 * _combatXpMultiplier);
                StyleXPGranted += damage * 4 * _combatXpMultiplier;
                break;
        }

        RefreshLevelsAfterExperience();

        if (Enemy.CurrentHP <= 0)
            HandleEnemyDefeated();
    }

    private void PerformEnemyAttack()
    {
        if (!RollAccuracy(
                _enemyAccuracyLevel,
                _effectiveDefenseLevel))
            return;

        int damage = RollDamage(Enemy.Strength);

        _player.CurrentHP = Math.Max(0, _player.CurrentHP - damage);

        if (_enemyIsRegenerative)
        {
            Enemy.CurrentHP = Math.Min(
                Enemy.HP,
                Enemy.CurrentHP + Math.Max(1, Enemy.HP / 100));
        }

        TryAutoEat();

        if (_player.CurrentHP <= 0)
        {
            PlayerDied = true;
            IsComplete = true;
        }
    }

    private void HandleEnemyDefeated()
    {
        _killLoot.Clear();
        bool discoveredDrop = false;

        foreach (Drop drop in Enemy.DropTable.Drops)
        {
            double effectiveChance = Math.Clamp(
                drop.Chance * _dropChanceMultiplier,
                0d,
                1d);

            if (_random.NextDouble() > effectiveChance)
                continue;

            int quantity = _random.Next(drop.MinQuantity, drop.MaxQuantity + 1);

            if (!_player.Inventory.AddItem(drop.Item, quantity))
                continue;

            // Keep offline combat's persistent luck tracking consistent with
            // live combat. Without this, rare drops earned while away appear
            // in loot but can never replace the Home-screen record.
            LuckiestDrop luckiestDrop = _player.LuckiestDrop;
            if (effectiveChance > 0 &&
                (!luckiestDrop.IsValid ||
                 effectiveChance < luckiestDrop.Chance))
            {
                _player.RecordLuckiestDrop(
                    drop.Item,
                    _startingKillCount + Kills + 1,
                    effectiveChance,
                    $"{Enemy.Name} kills");
            }

            bool isNewCollectionItem =
                _undiscoveredDrops.Count > 0 &&
                _undiscoveredDrops.Remove(drop.Item);
            if (isNewCollectionItem)
            {
                NewCollectionItems.Add(drop.Item);
                discoveredDrop = true;
            }

            if (_unrecordedEnemyDrops.Count > 0 &&
                _unrecordedEnemyDrops.Remove(drop.Item))
            {
                _killLoot.Add(new LootResult(
                    drop.Item,
                    quantity,
                    effectiveChance,
                    drop.Rarity));
            }

            Loot.TryGetValue(drop.Item, out long currentQuantity);
            Loot[drop.Item] = currentQuantity + quantity;
            LootRarities[drop.Item] = drop.Rarity;
        }

        Kills++;

        if (_killLoot.Count > 0)
        {
            if (discoveredDrop)
            {
                // Preserve the live-combat event ordering: collection-completion
                // listeners see the kill that produced the final missing drop.
                CommitKillCount();
            }

            _player.CollectionLog.RecordDrops(Enemy, _killLoot);

            if (discoveredDrop)
                RefreshDropChanceMultiplier();
        }

        if (!AutoFight)
        {
            IsComplete = true;
            return;
        }

        IsRespawning = true;
        RespawnTicksRemaining = DebugSettings.GetAutoFightRespawnTicks(
            AutoFightRespawnTicks);
        _playerElapsedTicks = 0;
        _enemyElapsedTicks = 0;
    }

    private void CommitKillCount()
    {
        long pendingKills = Kills - _committedKills;
        if (pendingKills <= 0)
            return;

        _player.CollectionLog.RecordKills(Enemy, pendingKills);
        _committedKills = Kills;
    }

    private bool TryAutoEat()
    {
        if (_autoEatCooldownTicks > 0 || !ShouldAutoEat())
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
