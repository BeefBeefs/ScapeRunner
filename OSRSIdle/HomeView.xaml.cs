namespace OSRSIdle;

public partial class HomeView : ContentView
{
    private readonly Player _player;

    private readonly CombatManager _combatManager;

    private readonly ActivityManager _activityManager;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public HomeView(
        Player player,
        CombatManager combatManager,
        ActivityManager activityManager)
    {
        InitializeComponent();

        _player =
            player;

        _combatManager =
            combatManager;

        _activityManager =
            activityManager;

        ConfigureEquipmentSlot(HeadSlot, EquipmentSlot.Head);
        ConfigureEquipmentSlot(AmuletSlot, EquipmentSlot.Amulet);
        ConfigureEquipmentSlot(BodySlot, EquipmentSlot.Body);
        ConfigureEquipmentSlot(RingSlot, EquipmentSlot.Ring);
        ConfigureEquipmentSlot(WeaponSlot, EquipmentSlot.Weapon);
        ConfigureEquipmentSlot(LegsSlot, EquipmentSlot.Legs);
        ConfigureEquipmentSlot(ShieldSlot, EquipmentSlot.Shield);
        ConfigureEquipmentSlot(GlovesSlot, EquipmentSlot.Gloves);
        ConfigureEquipmentSlot(BootsSlot, EquipmentSlot.Boots);
        ConfigureEquipmentSlot(FoodSlot, EquipmentSlot.Food);

        // --------------------------------------------------------
        // Listen for combat XP changes.
        // --------------------------------------------------------

        _combatManager.XPChanged +=
            OnXPChanged;

        _activityManager.ActivityChanged +=
            OnActivityChanged;

        _player.EquipmentChanged +=
            OnEquipmentChanged;

        _player.AutoEatSettingsChanged +=
            OnAutoEatSettingsChanged;

        _player.LuckiestDropChanged +=
            OnLuckiestDropChanged;

        _player.CollectionLog.CollectionChanged +=
            OnCollectionChanged;

        HPXPBar.SizeChanged +=
            OnXPBarSizeChanged;

        AttackXPBar.SizeChanged +=
            OnXPBarSizeChanged;

        StrengthXPBar.SizeChanged +=
            OnXPBarSizeChanged;

        DefenseXPBar.SizeChanged +=
            OnXPBarSizeChanged;

        UpdateDisplay();
    }


    // ============================================================
    // DISPOSE
    // ============================================================

    public void Dispose()
    {
        _combatManager.XPChanged -=
            OnXPChanged;

        _activityManager.ActivityChanged -=
            OnActivityChanged;

        _player.EquipmentChanged -=
            OnEquipmentChanged;

        _player.AutoEatSettingsChanged -=
            OnAutoEatSettingsChanged;

        _player.LuckiestDropChanged -=
            OnLuckiestDropChanged;

        _player.CollectionLog.CollectionChanged -=
            OnCollectionChanged;

    }


    // ============================================================
    // XP CHANGED
    // ============================================================

    private void OnXPChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateDisplay();
        });
    }

    private void OnEquipmentChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateDisplay();
        });
    }

    private void OnActivityChanged(
        object? sender,
        EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdateActivityEfficiency);
    }

    private void OnAutoEatSettingsChanged()
    {
        MainThread.BeginInvokeOnMainThread(UpdateDisplay);
    }

    private void OnLuckiestDropChanged()
    {
        MainThread.BeginInvokeOnMainThread(UpdateLuckiestDropDisplay);
    }

    private void OnCollectionChanged()
    {
        MainThread.BeginInvokeOnMainThread(UpdateDisplay);
    }

    public void RefreshDisplay()
    {
        UpdateDisplay();
    }

    private void OnXPBarSizeChanged(
        object? sender,
        EventArgs e)
    {
        UpdateDisplay();
    }

    // ============================================================
    // UPDATE DISPLAY
    // ============================================================

    private void UpdateDisplay()
    {
        CharacterNameLabel.Text = _player.Name;
        CharacterPortraitImage.Source = _player.PortraitImage;
        CharacterCombatLevelLabel.Text =
            $"Combat lvl {_player.GetCombatLevel()}";

        UpdateLuckiestDropDisplay();
        TotalEnemiesKilledLabel.Text =
            $"Total enemies killed: {_player.CollectionLog.GetTotalKillCount():N0}";
        GlobalDropBoostLabel.Text =
            $"Global drop boost: {_player.GlobalDropBoostPercent:0.0}%";

        UpdateActivityEfficiency();

        AutoEatThresholdSlider.Value = _player.AutoEatThresholdPercent;
        AutoEatThresholdLabel.Text = $"{_player.AutoEatThresholdPercent}%";

        HPLabel.Text =
            $"HP: {_player.HP.Level} + {_player.GetEquipmentHPBonus()}";

        AttackLabel.Text =
            $"Attack: {_player.Attack.Level} + {_player.GetEquipmentAttackBonus()}";

        StrengthLabel.Text =
            $"Strength: {_player.Strength.Level} + {_player.GetEquipmentStrengthBonus()}";

        DefenseLabel.Text =
            $"Defense: {_player.Defense.Level} + {_player.GetEquipmentDefenseBonus()}";

        Item? weapon =
            _player.EquippedWeapon;

        int attackSpeedTicks =
            _player.GetAttackSpeedTicks();

        WeaponStatsLabel.Text =
            $"Weapon: {weapon?.Name ?? "Unarmed"} — " +
            $"{attackSpeedTicks} tick{(attackSpeedTicks == 1 ? "" : "s")}";

        SetGoldStatValue(
            MaxHitLabel,
            "Max Hit: ",
            _player.GetMaxHit());

        SetGoldStatValue(
            TotalEquipmentStrengthLabel,
            "Total Equipment Strength: ",
            _player.GetEquipmentStrengthBonus());

        SetGoldStatValue(
            TotalEquipmentAttackLabel,
            "Total Equipment Attack: ",
            _player.GetEquipmentAttackBonus());

        SetGoldStatValue(
            TotalEquipmentDefenseLabel,
            "Total Equipment Defense: ",
            _player.GetEquipmentDefenseBonus());

        SetGoldStatValue(
            TotalEquipmentHPLabel,
            "Total Equipment HP: ",
            _player.GetEquipmentHPBonus());

        UpdateEquipment();


        // --------------------------------------------------------
        // UPDATE COMBAT XP BARS
        // --------------------------------------------------------

        UpdateSkillBar(
            HPXPBar,
            HPXPFill,
            HPXPLabel,
            _player.HP);

        UpdateSkillBar(
            AttackXPBar,
            AttackXPFill,
            AttackXPLabel,
            _player.Attack);

        UpdateSkillBar(
            StrengthXPBar,
            StrengthXPFill,
            StrengthXPLabel,
            _player.Strength);

        UpdateSkillBar(
            DefenseXPBar,
            DefenseXPFill,
            DefenseXPLabel,
            _player.Defense);
    }

    private void UpdateActivityEfficiency()
    {
        if (!_activityManager.IsActive ||
            _activityManager.CurrentSkill == null ||
            _activityManager.CurrentActivity == null)
        {
            ActivityEfficiencyLabel.Text = "No skilling activity";
            return;
        }

        SkillActivity activity = _activityManager.CurrentActivity;
        ActivityEfficiencyLabel.Text =
            $"{activity.Name}: " +
            $"{ActivityMetrics.FormatRate(ActivityMetrics.XpPerHour(activity))} XP/hr" +
            (activity.ItemReward == null
                ? ""
                : $"  •  {ActivityMetrics.FormatRate(ActivityMetrics.ItemsPerHour(activity))}/hr {activity.ItemReward.Name}");
    }

    private void UpdateLuckiestDropDisplay()
    {
        LuckiestDrop record = _player.LuckiestDrop;

        if (!record.IsValid)
        {
            LuckiestDropLabel.Text = "Luckiest drop: None yet";
            LuckiestDropChanceLabel.IsVisible = false;
            return;
        }

        string chanceText = record.Chance >= 1d
            ? $"{record.Chance:P0}"
            : $"1/{Math.Max(1, Math.Round(1d / record.Chance)):N0}";

        LuckiestDropLabel.Text =
            $"Luckiest drop: {record.ItemName}\n" +
            $"{record.Attempts:N0} {record.Source} • {chanceText}";

        long attempts = Math.Max(1, record.Attempts);
        double cumulativeChance = 1d - Math.Pow(
            1d - Math.Min(1d, record.Chance),
            attempts);
        LuckiestDropChanceLabel.Text =
            $"Chance: {cumulativeChance:P2}";
        LuckiestDropChanceLabel.IsVisible = true;
    }


    // ============================================================
    // EQUIPMENT
    // ============================================================

    private void UpdateEquipment()
    {
        UpdateEquipmentSlot(
            HeadIconLabel,
            HeadEmptySlotIcon,
            HeadNameLabel,
            EquipmentSlot.Head,
            "empty_head.png");

        UpdateEquipmentSlot(
            AmuletIconLabel,
            AmuletEmptySlotIcon,
            AmuletNameLabel,
            EquipmentSlot.Amulet,
            "empty_amulet.png");

        UpdateEquipmentSlot(
            BodyIconLabel,
            BodyEmptySlotIcon,
            BodyNameLabel,
            EquipmentSlot.Body,
            "empty_body.png");

        UpdateEquipmentSlot(
            RingIconLabel,
            RingEmptySlotIcon,
            RingNameLabel,
            EquipmentSlot.Ring,
            "empty_ring.png");

        UpdateEquipmentSlot(
            WeaponIconLabel,
            WeaponEmptySlotIcon,
            WeaponNameLabel,
            EquipmentSlot.Weapon,
            "empty_weapon.png");

        UpdateEquipmentSlot(
            LegsIconLabel,
            LegsEmptySlotIcon,
            LegsNameLabel,
            EquipmentSlot.Legs,
            "empty_legs.png");

        UpdateEquipmentSlot(
            ShieldIconLabel,
            ShieldEmptySlotIcon,
            ShieldNameLabel,
            EquipmentSlot.Shield,
            "empty_shield.png");

        UpdateEquipmentSlot(
            GlovesIconLabel,
            GlovesEmptySlotIcon,
            GlovesNameLabel,
            EquipmentSlot.Gloves,
            "empty_gloves.png");

        UpdateEquipmentSlot(
            BootsIconLabel,
            BootsEmptySlotIcon,
            BootsNameLabel,
            EquipmentSlot.Boots,
            "empty_boots.png");

        Item? food = _player.EquippedFood;

        FoodIconLabel.Text = "";
        FoodEmptySlotIcon.Source = food?.IconImage ?? "empty_food.png";
        FoodEmptySlotIcon.IsVisible = true;
        FoodNameLabel.Text = food == null
            ? "Empty"
            : $"{food.Name} x{_player.Inventory.GetQuantity(food)} (+{food.HealingAmount} HP)";
        FoodNameLabel.TextColor = food == null
            ? Colors.Red
            : Colors.White;

        if (FoodIconLabel.Parent?.Parent?.Parent is Border foodSlotBorder)
        {
            foodSlotBorder.BackgroundColor = food == null
                ? Color.FromArgb("#4A4A4A")
                : Color.FromArgb("#1B1B1B");
        }
    }

    private void UpdateEquipmentSlot(
        Label iconLabel,
        Image emptySlotIcon,
        Label nameLabel,
        EquipmentSlot slot,
        string emptyIconImage)
    {
        Item? item =
            _player.GetEquippedItem(slot);

        iconLabel.Text = "";
        emptySlotIcon.Source = item?.IconImage ?? emptyIconImage;
        emptySlotIcon.IsVisible = true;

        nameLabel.Text =
            item?.Name ?? "Empty";
        nameLabel.TextColor = item == null
            ? Colors.Red
            : GameThemeCache.GetItemRarityColor(item);

        // Keep empty slots light and make equipped gear use the darker slot
        // treatment so the populated equipment stands out by contrast.
        if (iconLabel.Parent?.Parent?.Parent is Border slotBorder)
        {
            slotBorder.BackgroundColor = item == null
                ? Color.FromArgb("#4A4A4A")
                : Color.FromArgb("#1B1B1B");
        }
    }

    private static void SetGoldStatValue(
        Label label,
        string statLabel,
        int value)
    {
        FormattedString formattedText = new();
        formattedText.Spans.Add(new Span { Text = statLabel });
        formattedText.Spans.Add(
            new Span
            {
                Text = value.ToString(),
                TextColor = Color.FromArgb("#D99032")
            });

        label.FormattedText = formattedText;
    }

    private void OnEquipmentSlotTapped(
        object? sender,
        TappedEventArgs e)
    {
        string? slotName =
            e.Parameter?.ToString() ??
            (sender as TapGestureRecognizer)?
                .CommandParameter?
                .ToString();

        if (string.IsNullOrWhiteSpace(slotName) ||
            !Enum.TryParse(
                slotName,
                out EquipmentSlot slot))
        {
            return;
        }

        UnequipSlot(slot);
    }

    private void ConfigureEquipmentSlot(
        Border slotBorder,
        EquipmentSlot slot)
    {
        slotBorder.GestureRecognizers.Clear();

        if (slotBorder.Content is VisualElement content)
        {
            content.InputTransparent = false;
        }

        TapGestureRecognizer tap = new();
        tap.Tapped += (sender, e) => UnequipSlot(slot);
        slotBorder.GestureRecognizers.Add(tap);
    }

    private void UnequipSlot(EquipmentSlot slot)
    {
        Item? item = _player.GetEquippedItem(slot);

        if (item == null)
        {
            return;
        }

        if (slot == EquipmentSlot.Food)
        {
            _player.ClearEquippedFood();
            UpdateDisplay();
            return;
        }

        if (_player.UnequipItem(slot))
        {
            UpdateDisplay();
        }
    }


    private void OnAutoEquipClicked(
        object sender,
        EventArgs e)
    {
        _player.AutoEquipBestGear();

        UpdateDisplay();
    }

    private void OnAutoEatThresholdChanged(
        object? sender,
        ValueChangedEventArgs e)
    {
        int percentage = (int)Math.Round(e.NewValue);
        _player.SetAutoEatThresholdPercent(percentage);
        AutoEatThresholdLabel.Text = $"{percentage}%";
    }


    // ============================================================
    // UPDATE SKILL BAR
    // ============================================================

    private void UpdateSkillBar(
        Grid xpBar,
        BoxView xpFill,
        Label xpLabel,
        Skill skill)
    {
        int currentLevel =
            skill.Level;


        if (currentLevel >= 99)
        {
            UpdateXPBar(
                xpBar,
                xpFill,
                1);

            xpLabel.Text =
                $"{skill.XP:0} XP — MAX";

            return;
        }


        int currentLevelXP =
            ExperienceTable.GetXPForLevel(
                currentLevel);

        int nextLevelXP =
            ExperienceTable.GetXPForLevel(
                currentLevel + 1);


        double progress =
            (skill.XP - currentLevelXP) /
            (nextLevelXP - currentLevelXP);


        progress =
            Math.Clamp(
                progress,
                0,
                1);


        UpdateXPBar(
            xpBar,
            xpFill,
            progress);


        xpLabel.Text =
            $"{skill.XP:0} / {nextLevelXP} XP";
    }


    // ============================================================
    // UPDATE XP BAR
    // ============================================================

    private static void UpdateXPBar(
        Grid xpBar,
        BoxView xpFill,
        double percentage)
    {
        percentage =
            Math.Clamp(
                percentage,
                0,
                1);

        xpFill.BackgroundColor =
            percentage <= 0.30
                ? Colors.Red
                : percentage <= 0.70
                    ? Colors.Yellow
                    : Colors.Green;

        xpFill.WidthRequest =
            xpBar.Width * percentage;
    }
}
