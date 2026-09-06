namespace OSRSIdle;

// ================================================================
// COMBAT MANAGER
// ================================================================

public class CombatManager
{
    // ============================================================
    // COMBAT STATE
    // ============================================================

    public Player Player { get; }

    public List<LootResult> LastLoot { get; private set; } = new();

    public Enemy? CurrentEnemy { get; private set; }

    public bool IsInCombat =>
        CurrentEnemy != null;

    // Auto-fight keeps this state while waiting to start the next fight.
    // It lets persistent UI remain in combat mode between enemies.
    public bool IsAutoFightRespawning { get; private set; }

    public Enemy? AutoFightEnemy { get; private set; }

    public int AutoFightTicksRemaining { get; private set; }

    public bool IsAutoFightEnabled { get; private set; }


    // ============================================================
    // COMBAT STYLE
    // ============================================================

    public CombatStyle CurrentCombatStyle { get; private set; } =
        CombatStyle.Attack;


    // ============================================================
    // ATTACK PROGRESS
    // ============================================================

    // These are visual progress values from 0.0 to 1.0.
    //
    // The actual attacks happen on discrete 600 ms game ticks.
    // The UI updates more frequently so the bars appear smooth.

    public double PlayerAttackProgress { get; private set; }

    public double EnemyAttackProgress { get; private set; }


    // ============================================================
    // CURRENT ATTACK SPEED
    // ============================================================

    public int PlayerAttackSpeedTicks =>
        GetPlayerAttackSpeedTicks();

    public int AutoEatCooldownTicks { get; private set; }


    // ============================================================
    // RANDOM NUMBER GENERATOR
    // ============================================================

    private readonly Random _random = new();


    // ============================================================
    // EVENTS
    // ============================================================

    public event Action? CombatUpdated;

    public event Action? CombatStarted;

    public event Action? CombatStopped;

    public event Action<CombatHitEventArgs>? AttackPerformed;

    public event Action<AutoEatEventArgs>? AutoEatPerformed;

    public event Action<Enemy>? EnemyDefeated;

    public event Action? PlayerDefeated;

    public event Action? XPChanged;

    public event EventHandler<LevelUpEventArgs>? LevelUp;

    public Enemy? LastDefeatedEnemy { get; private set; }


    // ============================================================
    // TIMER
    // ============================================================

    private CancellationTokenSource? _combatCancellation;

    private double _autoEatElapsedMilliseconds;

    private const int AutoEatCooldownDurationTicks = 4;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public CombatManager(Player player)
    {
        Player = player;
    }


    // ============================================================
    // PLAYER ATTACK SPEED
    // ============================================================

    private int GetPlayerAttackSpeedTicks()
    {
        return Player.GetAttackSpeedTicks();
    }


    // ============================================================
    // SET COMBAT STYLE
    // ============================================================

    public void SetCombatStyle(
        CombatStyle style)
    {
        CurrentCombatStyle =
            style;

        CombatUpdated?.Invoke();
    }


    // ============================================================
    // AUTO-FIGHT RESPAWN STATUS
    // ============================================================

    public void BeginAutoFightRespawn(
        Enemy enemy,
        int ticksRemaining)
    {
        IsAutoFightRespawning =
            true;

        AutoFightEnemy =
            enemy;

        AutoFightTicksRemaining =
            Math.Max(
                0,
                ticksRemaining);

        CombatUpdated?.Invoke();
    }

    public void SetAutoFightEnabled(bool enabled)
    {
        IsAutoFightEnabled = enabled;
        CombatUpdated?.Invoke();
    }

    public void UpdateAutoFightRespawn(
        int ticksRemaining)
    {
        if (!IsAutoFightRespawning)
            return;

        AutoFightTicksRemaining =
            Math.Max(
                0,
                ticksRemaining);

        CombatUpdated?.Invoke();
    }

    public void ClearAutoFightRespawn()
    {
        if (!IsAutoFightRespawning &&
            AutoFightEnemy == null &&
            AutoFightTicksRemaining == 0)
        {
            return;
        }

        IsAutoFightRespawning =
            false;

        AutoFightEnemy =
            null;

        AutoFightTicksRemaining =
            0;

        CombatUpdated?.Invoke();
    }


    // ============================================================
    // START COMBAT
    // ============================================================

    public void StartCombat(
        Enemy enemy,
        bool restorePlayerHealth = true)
    {
        // --------------------------------------------------------
        // Stop any existing combat first.
        // --------------------------------------------------------

        StopCombat();

        ClearAutoFightRespawn();


        // --------------------------------------------------------
        // Clear previous loot.
        // --------------------------------------------------------

        LastLoot.Clear();


        // --------------------------------------------------------
        // Set the enemy.
        // --------------------------------------------------------

        CurrentEnemy =
            enemy;


        // --------------------------------------------------------
        // Always restore the new enemy. The player only restores when a
        // fight is manually started; auto-fight carries their remaining HP
        // into the next encounter.
        // --------------------------------------------------------

        if (restorePlayerHealth)
        {
            Player.CurrentHP =
                Player.GetMaxHP();
        }

        enemy.CurrentHP =
            enemy.HP;


        // --------------------------------------------------------
        // Reset attack progress.
        // --------------------------------------------------------

        PlayerAttackProgress = 0;

        EnemyAttackProgress = 0;

        AutoEatCooldownTicks = 0;
        _autoEatElapsedMilliseconds = 0;


        // --------------------------------------------------------
        // Combat is now active.
        // --------------------------------------------------------

        CombatStarted?.Invoke();


        // --------------------------------------------------------
        // Create cancellation token.
        // --------------------------------------------------------

        _combatCancellation =
            new CancellationTokenSource();


        // --------------------------------------------------------
        // Start combat loop.
        // --------------------------------------------------------

        _ = CombatLoopAsync(
            _combatCancellation.Token);
    }


    // ============================================================
    // STOP COMBAT
    // ============================================================

    public void StopCombat()
    {
        // --------------------------------------------------------
        // Cancel the combat loop.
        // --------------------------------------------------------

        _combatCancellation?.Cancel();


        // --------------------------------------------------------
        // Dispose the cancellation source.
        // --------------------------------------------------------

        _combatCancellation?.Dispose();

        _combatCancellation = null;


        // --------------------------------------------------------
        // Remove the active enemy.
        // --------------------------------------------------------

        CurrentEnemy = null;


        // --------------------------------------------------------
        // Reset attack bars.
        // --------------------------------------------------------

        PlayerAttackProgress = 0;

        EnemyAttackProgress = 0;


        // --------------------------------------------------------
        // Tell the UI that combat has stopped.
        // --------------------------------------------------------

        CombatStopped?.Invoke();
    }

    /// <summary>
    /// Ends an encounter without retaining any enemy/loot state that could
    /// be used to resume auto-fight later. This is used when another
    /// activity (such as skilling) takes over the player's activity slot.
    /// </summary>
    public void AbortCombatEncounter()
    {
        SetAutoFightEnabled(false);
        ClearAutoFightRespawn();
        StopCombat();

        LastDefeatedEnemy = null;
        LastLoot.Clear();
    }


    // ============================================================
    // COMBAT LOOP
    // ============================================================

    private async Task CombatLoopAsync(
        CancellationToken cancellationToken)
    {
        // ========================================================
        // GAME TICK
        // ========================================================

        const int GameTickMilliseconds =
            GameClock.StandardTickMilliseconds;


        // ========================================================
        // VISUAL UPDATE
        // ========================================================

        // The combat engine operates on 600 ms game ticks.
        //
        // The UI receives updates every 100 ms so the attack bars
        // can animate smoothly between actual game ticks.

        const int NormalVisualUpdateMilliseconds = 100;
        const int DebugVisualUpdateMilliseconds = 15;


        // ========================================================
        // ELAPSED TIME
        // ========================================================

        double playerElapsedMilliseconds = 0;

        double enemyElapsedMilliseconds = 0;


        // ========================================================
        // COMBAT LOOP
        // ========================================================

        while (
            !cancellationToken.IsCancellationRequested &&
            CurrentEnemy != null)
        {
            int speedMultiplier = GameClock.SpeedMultiplier;
            int visualUpdateMilliseconds =
                GameClock.IsDebugSpeedEnabled
                    ? DebugVisualUpdateMilliseconds
                    : NormalVisualUpdateMilliseconds;

            try
            {
                await Task.Delay(
                    visualUpdateMilliseconds,
                    cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }


            // ----------------------------------------------------
            // Get current enemy.
            // ----------------------------------------------------

            Enemy? enemy =
                CurrentEnemy;


            if (enemy == null)
                break;


            // ----------------------------------------------------
            // Advance elapsed combat time.
            // ----------------------------------------------------

            double simulatedElapsedMilliseconds =
                visualUpdateMilliseconds * speedMultiplier;

            playerElapsedMilliseconds +=
                simulatedElapsedMilliseconds;

            enemyElapsedMilliseconds +=
                simulatedElapsedMilliseconds;

            _autoEatElapsedMilliseconds +=
                simulatedElapsedMilliseconds;

            while (_autoEatElapsedMilliseconds >= GameTickMilliseconds)
            {
                _autoEatElapsedMilliseconds -= GameTickMilliseconds;
                AdvanceAutoEatTick();
            }


            // ----------------------------------------------------
            // Calculate attack cycle durations.
            // ----------------------------------------------------

            int playerAttackTicks =
                Math.Max(
                    1,
                    GetPlayerAttackSpeedTicks());

            int enemyAttackTicks =
                Math.Max(
                    1,
                    enemy.Traits.Contains(EnemyTrait.Frenzied)
                        ? (int)Math.Floor(enemy.AttackSpeedTicks * 0.9)
                        : enemy.AttackSpeedTicks);


            double playerAttackMilliseconds =
                Player.GetAttackSpeedTicks() *
                GameTickMilliseconds;

            double enemyAttackMilliseconds =
                enemyAttackTicks *
                GameTickMilliseconds;


            // ====================================================
            // UPDATE VISUAL ATTACK BARS
            // ====================================================

            PlayerAttackProgress =
                Math.Clamp(
                    playerElapsedMilliseconds /
                    playerAttackMilliseconds,
                    0,
                    1);

            EnemyAttackProgress =
                Math.Clamp(
                    enemyElapsedMilliseconds /
                    enemyAttackMilliseconds,
                    0,
                    1);


            // ====================================================
            // PLAYER ATTACK
            // ====================================================

            if (playerElapsedMilliseconds >=
                playerAttackMilliseconds)
            {
                // ------------------------------------------------
                // Preserve any extra elapsed time.
                //
                // Normally this will be very small, but preserving
                // it makes the timer more accurate if the device
                // experiences a delayed update.
                // ------------------------------------------------

                playerElapsedMilliseconds -=
                    playerAttackMilliseconds;

                PlayerAttackProgress = 0;


                // ------------------------------------------------
                // Perform attack.
                // ------------------------------------------------

                PerformPlayerAttack(enemy);


                // ------------------------------------------------
                // Player attack may have defeated the enemy.
                // ------------------------------------------------

                if (CurrentEnemy == null)
                    break;
            }


            // ====================================================
            // ENEMY ATTACK
            // ====================================================

            if (enemyElapsedMilliseconds >=
                enemyAttackMilliseconds)
            {
                enemyElapsedMilliseconds -=
                    enemyAttackMilliseconds;

                EnemyAttackProgress = 0;


                // ------------------------------------------------
                // Perform attack.
                // ------------------------------------------------

                PerformEnemyAttack(enemy);


                // ------------------------------------------------
                // Enemy attack may have killed the player.
                // ------------------------------------------------

                if (CurrentEnemy == null)
                    break;
            }


            // ----------------------------------------------------
            // Update UI.
            // ----------------------------------------------------

            CombatUpdated?.Invoke();
        }
    }


    // ============================================================
    // PLAYER ATTACK
    // ============================================================

    private void PerformPlayerAttack(
        Enemy enemy)
    {
        // --------------------------------------------------------
        // Roll accuracy.
        // --------------------------------------------------------

        bool hit =
            DebugSettings.IsInstakillEnabled ||
            RollAccuracy(
                Player.GetEffectiveAttackLevel(),
                enemy.Traits.Contains(EnemyTrait.Armored)
                    ? (int)Math.Ceiling(enemy.Defense * 1.15)
                    : enemy.Defense);


        // --------------------------------------------------------
        // Miss.
        // --------------------------------------------------------

        if (!hit)
        {
            AttackPerformed?.Invoke(
                new CombatHitEventArgs(
                    attackerIsPlayer: true,
                    hit: false,
                    damage: 0));

            return;
        }


        // --------------------------------------------------------
        // Roll damage.
        // --------------------------------------------------------

        int damage =
            DebugSettings.IsInstakillEnabled
                ? enemy.CurrentHP
                : RollDamage(
                    Player.GetEffectiveStrengthLevel());

        if (enemy.Traits.Contains(EnemyTrait.Armored))
            damage = Math.Max(1, (int)Math.Floor(damage * 0.85));


        // --------------------------------------------------------
        // Apply damage.
        // --------------------------------------------------------

        enemy.CurrentHP =
            Math.Max(
                0,
                enemy.CurrentHP - damage);


        // ========================================================
        // XP
        // ========================================================

        // HP receives 1 XP per damage dealt.
        AwardCombatXP(
            Player.HP,
            damage);


        // Selected combat style receives 4 XP per damage.
        switch (CurrentCombatStyle)
        {
            case CombatStyle.Attack:

                AwardCombatXP(
                    Player.Attack,
                    damage * 4);

                break;


            case CombatStyle.Strength:

                AwardCombatXP(
                    Player.Strength,
                    damage * 4);

                break;


            case CombatStyle.Defense:

                AwardCombatXP(
                    Player.Defense,
                    damage * 4);

                break;
        }


        // --------------------------------------------------------
        // Tell UI XP changed.
        // --------------------------------------------------------

        XPChanged?.Invoke();


        // --------------------------------------------------------
        // Tell UI about the attack.
        // --------------------------------------------------------

        AttackPerformed?.Invoke(
            new CombatHitEventArgs(
                attackerIsPlayer: true,
                hit: true,
                damage: damage));


        // --------------------------------------------------------
        // Check if enemy died.
        // --------------------------------------------------------

        if (enemy.CurrentHP <= 0)
        {
            HandleEnemyDefeated(
                enemy);
        }
    }


    // ============================================================
    // ENEMY ATTACK
    // ============================================================

    private void PerformEnemyAttack(
        Enemy enemy)
    {
        // --------------------------------------------------------
        // Roll accuracy.
        // --------------------------------------------------------

        bool hit =
            RollAccuracy(
                enemy.Traits.Contains(EnemyTrait.Accurate)
                    ? (int)Math.Ceiling(enemy.Attack * 1.1)
                    : enemy.Attack,
                Player.GetEffectiveDefenseLevel());


        // --------------------------------------------------------
        // Miss.
        // --------------------------------------------------------

        if (!hit)
        {
            AttackPerformed?.Invoke(
                new CombatHitEventArgs(
                    attackerIsPlayer: false,
                    hit: false,
                    damage: 0));

            return;
        }


        // --------------------------------------------------------
        // Roll damage.
        // --------------------------------------------------------

        int damage =
            RollDamage(
                enemy.Strength);


        // --------------------------------------------------------
        // Apply damage.
        // --------------------------------------------------------

        Player.CurrentHP =
            Math.Max(
                0,
                Player.CurrentHP - damage);

        if (enemy.Traits.Contains(EnemyTrait.Regenerative))
            enemy.CurrentHP = Math.Min(enemy.HP, enemy.CurrentHP + Math.Max(1, enemy.HP / 100));

        TryAutoEat();


        // --------------------------------------------------------
        // Tell UI about the attack.
        // --------------------------------------------------------

        AttackPerformed?.Invoke(
            new CombatHitEventArgs(
                attackerIsPlayer: false,
                hit: true,
                damage: damage));


        // --------------------------------------------------------
        // Check if player died.
        // --------------------------------------------------------

        if (Player.CurrentHP <= 0)
        {
            HandlePlayerDefeated();
        }
    }


    // ============================================================
    // ACCURACY ROLL
    // ============================================================

    private void AdvanceAutoEatTick()
    {
        if (AutoEatCooldownTicks > 0)
        {
            AutoEatCooldownTicks--;
        }

        TryAutoEat();
    }

    private bool TryAutoEat()
    {
        if (AutoEatCooldownTicks > 0 || !Player.ShouldAutoEat())
            return false;

        Item? food = Player.EquippedFood;
        int previousHP = Player.CurrentHP;
        if (!Player.TryConsumeEquippedFood())
            return false;

        AutoEatCooldownTicks = AutoEatCooldownDurationTicks;
        if (food != null)
            AutoEatPerformed?.Invoke(
                new AutoEatEventArgs(
                    food,
                    previousHP,
                    Player.CurrentHP));
        return true;
    }

    private bool RollAccuracy(
        int attackLevel,
        int defenseLevel)
    {
        // --------------------------------------------------------
        // Prevent division by zero.
        // --------------------------------------------------------

        if (attackLevel <= 0 &&
            defenseLevel <= 0)
        {
            return true;
        }


        double hitChance =
            (double)attackLevel /
            (attackLevel + defenseLevel);


        // --------------------------------------------------------
        // Clamp hit chance.
        // --------------------------------------------------------

        hitChance =
            Math.Clamp(
                hitChance,
                0.05,
                0.95);


        return _random.NextDouble() <
               hitChance;
    }


    // ============================================================
    // DAMAGE ROLL
    // ============================================================

    private int RollDamage(
        int strengthLevel)
    {
        int maximumHit =
            Math.Max(
                1,
                (strengthLevel / 3) + 1);


        return _random.Next(
            1,
            maximumHit + 1);
    }


    // ============================================================
    // COMBAT XP
    // ============================================================

    private void AwardCombatXP(
        Skill skill,
        double xp)
    {
        int oldLevel = skill.Level;

        Enemy? enemy = CurrentEnemy;
        double bonus = enemy == null
            ? 0
            : ProgressionBonuses.CombatXpPercent(Player, enemy) / 100d;
        skill.AddXP(xp * (1 + bonus));

        int newLevel = skill.Level;

        if (newLevel > oldLevel)
        {
            LevelUp?.Invoke(
                this,
                new LevelUpEventArgs(
                    skill,
                    newLevel));
        }
    }


    // ============================================================
    // ENEMY DEFEATED
    // ============================================================

    private void HandleEnemyDefeated(
        Enemy enemy)
    {
        // --------------------------------------------------------
        // Roll loot.
        // --------------------------------------------------------

        LastLoot.Clear();


        foreach (Drop drop in enemy.DropTable.Drops)
        {
            // ----------------------------------------------------
            // Roll drop chance.
            // ----------------------------------------------------

            if (_random.NextDouble() > drop.Chance)
                continue;


            // ----------------------------------------------------
            // Determine quantity.
            // ----------------------------------------------------

            int quantity =
                _random.Next(
                    drop.MinQuantity,
                    drop.MaxQuantity + 1);


            // ----------------------------------------------------
            // Add loot to inventory.
            // ----------------------------------------------------

            if (Player.Inventory.AddItem(drop.Item, quantity))
            {
                Player.RecordLuckiestDrop(
                    drop.Item,
                    Player.CollectionLog.GetKillCount(enemy) + 1,
                    drop.Chance,
                    $"{enemy.Name} kills");

                LastLoot.Add(
                    new LootResult(
                        drop.Item,
                        quantity,
                        drop.Chance,
                        drop.Rarity));
            }
        }


        // --------------------------------------------------------
        // Remember defeated enemy.
        // --------------------------------------------------------

        LastDefeatedEnemy =
            enemy;

        Player.CollectionLog.RecordKill(
            enemy);

        Player.CollectionLog.RecordDrops(
            enemy,
            LastLoot);


        // --------------------------------------------------------
        // Tell UI.
        // --------------------------------------------------------

        EnemyDefeated?.Invoke(
            enemy);


        // --------------------------------------------------------
        // Stop combat.
        // --------------------------------------------------------

        StopCombat();
    }


    // ============================================================
    // PLAYER DEFEATED
    // ============================================================

    private void HandlePlayerDefeated()
    {
        // --------------------------------------------------------
        // Make absolutely certain HP is zero.
        // --------------------------------------------------------

        Player.CurrentHP = 0;


        // --------------------------------------------------------
        // Stop combat BEFORE notifying the rest of the game.
        //
        // This is important because the death event may immediately
        // begin the respawn sequence and change pages.
        // --------------------------------------------------------

        StopCombat();


        // --------------------------------------------------------
        // Tell GamePage / the rest of the game that the player died.
        // --------------------------------------------------------

        PlayerDefeated?.Invoke();
    }
}


// ================================================================
// COMBAT HIT EVENT ARGS
// ================================================================

public class CombatHitEventArgs : EventArgs
{
    public bool AttackerIsPlayer { get; }

    public bool Hit { get; }

    public int Damage { get; }


    public CombatHitEventArgs(
        bool attackerIsPlayer,
        bool hit,
        int damage)
    {
        AttackerIsPlayer =
            attackerIsPlayer;

        Hit =
            hit;

        Damage =
            damage;
    }
}

public sealed class AutoEatEventArgs : EventArgs
{
    public Item Food { get; }
    public int PreviousHP { get; }
    public int CurrentHP { get; }

    public AutoEatEventArgs(Item food, int previousHP, int currentHP)
    {
        Food = food;
        PreviousHP = previousHP;
        CurrentHP = currentHP;
    }
}
