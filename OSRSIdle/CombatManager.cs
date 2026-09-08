namespace OSRSIdle;

// ================================================================
// COMBAT MANAGER
// ================================================================

public class CombatManager : IDisposable
{
    // ============================================================
    // COMBAT STATE
    // ============================================================

    public Player Player { get; }

    // Publish loot as one immutable snapshot. Combat events are raised from
    // the combat loop while their UI consumers run on the main thread; a
    // mutable List could otherwise be cleared for the next encounter while a
    // defeat callback was still reading it.
    public IReadOnlyList<LootResult> LastLoot { get; private set; } =
        Array.Empty<LootResult>();

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

    public CombatAbility? PrimedAbility { get; private set; }

    public int AbilityCooldownTicks { get; private set; }


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

    public event Action? InventoryFull;

    public event Action? XPChanged;

    public event EventHandler<LevelUpEventArgs>? LevelUp;

    public Enemy? LastDefeatedEnemy { get; private set; }


    // ============================================================
    // TIMER
    // ============================================================

    private CancellationTokenSource? _combatCancellation;
    private CancellationTokenSource? _autoFightCancellation;
    private const int AutoFightDelayTicks = 12;

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

        if (PrimedAbility is CombatAbility ability &&
            ability.RequiredStyle() != style)
        {
            PrimedAbility = null;
        }

        CombatUpdated?.Invoke();
    }

    public bool ActivateAbility(CombatAbility ability)
    {
        if (!IsInCombat ||
            ability.RequiredStyle() != CurrentCombatStyle ||
            PrimedAbility != null ||
            AbilityCooldownTicks > 0)
        {
            return false;
        }

        PrimedAbility = ability;
        AbilityCooldownTicks = CombatAbilityRules.CooldownTicks;
        CombatUpdated?.Invoke();
        return true;
    }


    // ============================================================
    // AUTO-FIGHT RESPAWN STATUS
    // ============================================================

    public void BeginAutoFightRespawn(
        Enemy enemy,
        int ticksRemaining)
    {
        ClearAutoFightRespawn();
        if (!IsAutoFightEnabled || Player.CurrentHP <= 0 || IsInCombat)
            return;

        _autoFightCancellation = new CancellationTokenSource();
        CancellationToken token = _autoFightCancellation.Token;
        IsAutoFightRespawning =
            true;

        AutoFightEnemy =
            enemy;

        AutoFightTicksRemaining =
            Math.Max(
                0,
                ticksRemaining);

        CombatUpdated?.Invoke();
        _ = AutoFightRespawnAsync(enemy, token);
    }

    public void SetAutoFightEnabled(bool enabled)
    {
        IsAutoFightEnabled = enabled;
        if (!enabled)
            ClearAutoFightRespawn();
        CombatUpdated?.Invoke();
    }

    private async Task AutoFightRespawnAsync(Enemy enemy, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && AutoFightTicksRemaining > 0)
            {
                await Task.Delay(GameClock.TickInterval, token).ConfigureAwait(false);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (!token.IsCancellationRequested)
                        UpdateAutoFightRespawn(AutoFightTicksRemaining - 1);
                });
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Run and activity changes can cancel a queued restart.
                if (token.IsCancellationRequested || !IsAutoFightEnabled ||
                    !ReferenceEquals(AutoFightEnemy, enemy) || IsInCombat)
                    return;

                if (Player.CurrentHP <= 0)
                {
                    SetAutoFightEnabled(false);
                    return;
                }

                StartCombat(enemy, restorePlayerHealth: false);
            });
        }
        catch (OperationCanceledException)
        {
            // Auto was disabled, the encounter ended, or another fight began.
        }
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
        _autoFightCancellation?.Cancel();
        _autoFightCancellation?.Dispose();
        _autoFightCancellation = null;

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

        // --------------------------------------------------------
        // Clear previous loot.
        // --------------------------------------------------------

        LastLoot = Array.Empty<LootResult>();


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
        AbilityCooldownTicks = 0;
        PrimedAbility = null;
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
        ClearAutoFightRespawn();
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

        PrimedAbility = null;
        AbilityCooldownTicks = 0;


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
        LastLoot = Array.Empty<LootResult>();
    }

    public void Dispose()
    {
        AbortCombatEncounter();
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
        // Combat simulation remains time-based at a fine interval, while UI
        // snapshots are published less often to reduce main-thread work.

        const int NormalVisualUpdateMilliseconds = 100;
        const int CombatUiUpdateMilliseconds = 200;


        // ========================================================
        // ELAPSED TIME
        // ========================================================

        double playerElapsedMilliseconds = 0;

        double enemyElapsedMilliseconds = 0;

        double combatUiElapsedMilliseconds = 0;


        // ========================================================
        // COMBAT LOOP
        // ========================================================

        while (
            !cancellationToken.IsCancellationRequested &&
            CurrentEnemy != null)
        {
            int speedMultiplier = GameClock.SpeedMultiplier;
            int visualUpdateMilliseconds =
                GameClock.IsSpeedUpEnabled
                    ? GameClock.SpeedUpTickMilliseconds
                    : NormalVisualUpdateMilliseconds;

            try
            {
                await Task.Delay(
                    visualUpdateMilliseconds,
                    cancellationToken).ConfigureAwait(false);
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

            combatUiElapsedMilliseconds += visualUpdateMilliseconds;

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
                CombatRules.GetEnemyAttackSpeedTicks(enemy);


            double playerAttackMilliseconds =
                playerAttackTicks *
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

            int playerCatchUpAttacks = 0;
            while (playerElapsedMilliseconds >=
                   playerAttackMilliseconds &&
                   playerCatchUpAttacks++ < 8)
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

            int enemyCatchUpAttacks = 0;
            while (enemyElapsedMilliseconds >=
                   enemyAttackMilliseconds &&
                   enemyCatchUpAttacks++ < 8)
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

            if (combatUiElapsedMilliseconds >= CombatUiUpdateMilliseconds)
            {
                combatUiElapsedMilliseconds %= CombatUiUpdateMilliseconds;
                CombatUpdated?.Invoke();
            }
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

        int attackLevel = CombatRules.GetWeaknessAccuracyLevel(
            enemy,
            CurrentCombatStyle,
            CombatRules.GetPlayerAttackLevel(
                Player,
                CurrentCombatStyle));

        if (PrimedAbility == CombatAbility.PreciseStrike)
        {
            attackLevel = (int)Math.Ceiling(attackLevel * 1.25d);
            PrimedAbility = null;
        }

        bool hit =
            DebugSettings.IsInstakillEnabled ||
            RollAccuracy(
                attackLevel,
                CombatRules.GetEnemyDefenseLevel(enemy));


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
                    CombatRules.GetPlayerStrengthLevel(
                        Player,
                        CurrentCombatStyle));

        if (PrimedAbility == CombatAbility.PowerStrike)
        {
            damage = (int)Math.Ceiling(damage * 1.5d);
            PrimedAbility = null;
        }

        damage = CombatRules.GetWeaknessDamage(
            enemy,
            CurrentCombatStyle,
            damage);

        damage = CombatRules.ReducePlayerDamage(enemy, damage);


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
                CombatRules.GetEnemyAccuracyLevel(enemy),
                CombatRules.GetPlayerDefenseLevel(
                    Player,
                    CurrentCombatStyle));


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

        damage = CombatRules.ReduceIncomingDamage(CurrentCombatStyle, damage);

        if (PrimedAbility == CombatAbility.Guard)
        {
            damage = Math.Max(0, (int)Math.Floor(damage * 0.5d));
            PrimedAbility = null;
        }


        // --------------------------------------------------------
        // Apply damage.
        // --------------------------------------------------------

        Player.CurrentHP =
            Math.Max(
                0,
                Player.CurrentHP - damage);

        CombatRules.ApplyRegeneration(enemy);

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

        if (AbilityCooldownTicks > 0)
            AbilityCooldownTicks--;

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
        skill.AddXP(enemy == null
            ? xp
            : xp * CombatRules.GetCombatXpMultiplier(Player, enemy));

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

        LastLoot = Array.Empty<LootResult>();

        List<LootResult> awardedLoot = new();
        bool inventoryFull = false;


        foreach (Drop drop in enemy.DropTable.Drops)
        {
            // ----------------------------------------------------
            // Roll drop chance.
            // ----------------------------------------------------

            double effectiveChance = CombatRules.GetDropChance(
                Player,
                drop.Chance);

            if (_random.NextDouble() > effectiveChance)
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
                    effectiveChance,
                    $"{enemy.Name} kills");

                awardedLoot.Add(
                    new LootResult(
                        drop.Item,
                        quantity,
                        effectiveChance,
                        drop.Rarity));
            }
            else
            {
                inventoryFull = true;
            }
        }


        // --------------------------------------------------------
        // Remember defeated enemy.
        // --------------------------------------------------------

        LastDefeatedEnemy =
            enemy;

        LastLoot = Array.AsReadOnly(awardedLoot.ToArray());

        Player.CollectionLog.RecordKill(
            enemy);

        Player.CollectionLog.RecordDrops(
            enemy,
            LastLoot);


        // --------------------------------------------------------
        // Stop this encounter before notifying subscribers. This guarantees
        // cleanup even when a subscriber immediately starts another action,
        // and matches the player-defeat ordering below.
        // --------------------------------------------------------

        StopCombat();

        if (inventoryFull)
        {
            SetAutoFightEnabled(false);
            InventoryFull?.Invoke();
        }


        // --------------------------------------------------------
        // Tell UI.
        // --------------------------------------------------------

        if (IsAutoFightEnabled)
            BeginAutoFightRespawn(enemy,
                DebugSettings.GetAutoFightRespawnTicks(AutoFightDelayTicks));

        EnemyDefeated?.Invoke(
            enemy);
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
        SetAutoFightEnabled(false);


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
