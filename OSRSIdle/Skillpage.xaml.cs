namespace OSRSIdle;

public partial class SkillPage : ContentView
{
    private readonly Skill _skill;
    private readonly ActivityManager _activityManager;
    private readonly Dictionary<SkillActivity, GoldSliceButton> _activityButtons = new();
    private readonly Dictionary<SkillActivity, ActivityCardUI> _activityCards = new();
    private IDispatcherTimer? _trainingProgressTimer;
    private bool _isActive;
    private readonly UiUpdateCoalescer _uiUpdates;
    private SkillActivity? _pendingFeedbackActivity;

    public SkillPage(
        Skill skill,
        ActivityManager activityManager)
    {
        InitializeComponent();

        _skill = skill;
        _activityManager = activityManager;
        _uiUpdates = new UiUpdateCoalescer(UpdateSkillDisplay);

        UpdateSkillHeader();
        BuildActivityList();
    }

    public void SetActive(bool active)
    {
        if (_isActive == active)
            return;

        _isActive = active;

        if (active)
        {
            _activityManager.ActionCompleted += OnXPChanged;
            EnsureTrainingProgressTimer();
        }
        else
        {
            _activityManager.ActionCompleted -= OnXPChanged;
            StopTrainingProgressTimer();
        }
    }

    public void RefreshDisplay()
    {
        UpdateSkillDisplay();
    }

    private void UpdateSkillDisplay()
    {
        UpdateSkillHeader();
        UpdateActivityButtons();

        SkillActivity? activity = _pendingFeedbackActivity;
        _pendingFeedbackActivity = null;
        if (activity != null && _activityCards.TryGetValue(activity, out ActivityCardUI? card))
        {
            _ = PlayActionCompleteFeedbackAsync(card);
            _ = VisualEffects.ShowFloatingTextAsync(
                SkillEffectLayer,
                $"+{activity.XP * CombatRules.GetSkillXpMultiplier(_skill):N0} XP",
                Color.FromArgb("#FFE26A"),
                0.5,
                0.18);
        }
    }

    // ============================================================
    // SKILL HEADER
    // ============================================================

    private void UpdateSkillHeader()
    {
        SkillTitle.Text =
            _skill.Name;

        SkillIcon.Source = _skill.IconImage;

        SkillLevel.Text =
            $"Level {_skill.Level}";

        SkillXP.Text =
            $"{_skill.XP:N0} XP";
    }

    // ============================================================
    // XP CHANGED
    // ============================================================

    private void OnXPChanged(
        object? sender,
        EventArgs e)
    {
        if (_isActive)
        {
            _pendingFeedbackActivity = _activityManager.CurrentActivity;
            _uiUpdates.Request();
        }
    }

    public void Dispose()
    {
        _uiUpdates.Dispose();
        SetActive(false);
        StopTrainingProgressTimer();
    }

    // ============================================================
    // ACTIVITY LIST
    // ============================================================

    private void BuildActivityList()
    {
        ActivityList.Children.Clear();
        _activityButtons.Clear();
        _activityCards.Clear();

        foreach (SkillActivity activity in _skill.Activities)
        {
            VerticalStackLayout activityCard =
                new VerticalStackLayout
                {
                    Spacing = 3
                };

            // -------------------------
            // ACTIVITY NAME
            // -------------------------

            Label nameLabel = new Label
            {
                Text =
                    $"{activity.Icon}  {activity.Name}",

                FontSize = 17,

                FontAttributes =
                    FontAttributes.Bold
            };

            // -------------------------
            // REQUIRED LEVEL
            // -------------------------

            Label requirementLabel = new Label
            {
                Text =
                    $"Required Level: {activity.RequiredLevel}",

                FontSize = 14
            };

            // -------------------------
            // XP
            // -------------------------

            Label xpLabel = new Label
            {
                Text =
                    $"{activity.XP:N0} XP per action",

                FontSize = 14
            };

            // -------------------------
            // ACTION TIME
            // -------------------------

            Label timeLabel = new Label
            {
                Text =
                    $"{ActivityMetrics.EffectiveActionTicks(activity)} ticks",

                FontSize = 14
            };

            Label efficiencyLabel = new()
            {
                FontSize = 13,
                TextColor = Color.FromArgb("#FFE26A")
            };

            Label badgeLabel = new()
            {
                FontSize = 12,
                TextColor = Color.FromArgb("#62C7FF"),
                FontAttributes = FontAttributes.Bold
            };

            Label unlockLabel = new()
            {
                FontSize = 12,
                TextColor = Color.FromArgb("#B0B0B0")
            };

            Label? rewardLabel =
                activity.ItemReward == null
                    ? null
                    : new Label
                    {
                        Text =
                            $"Reward: {activity.ItemReward.Name} ×1",
                        FontSize = 14
                    };

            // -------------------------
            // TRAIN BUTTON
            // -------------------------

            bool meetsRequiredLevel =
                _skill.Level >= activity.RequiredLevel;

            GoldSliceButton trainButton = new GoldSliceButton
            {
                Text =
                    GetButtonText(activity),

                FontSize = 14,

                IsEnabled =
                    meetsRequiredLevel,

                Variant =
                    _activityManager.CurrentActivity == activity
                        ? GoldSliceButtonVariant.Green
                        : meetsRequiredLevel
                            ? GoldSliceButtonVariant.Neutral
                            : GoldSliceButtonVariant.Red,

                TextColor = Colors.White
            };

            trainButton.Clicked +=
                (sender, e) =>
                {
                    StartActivity(activity);
                };

            _activityButtons[activity] = trainButton;

            // -------------------------
            // BUILD CARD
            // -------------------------

            activityCard.Children.Add(
                nameLabel);

            activityCard.Children.Add(
                requirementLabel);

            activityCard.Children.Add(
                xpLabel);

            activityCard.Children.Add(
                timeLabel);

            activityCard.Children.Add(efficiencyLabel);
            activityCard.Children.Add(badgeLabel);
            activityCard.Children.Add(unlockLabel);

            if (rewardLabel != null)
            {
                activityCard.Children.Add(
                    rewardLabel);
            }

            activityCard.Children.Add(
                trainButton);

            Border trainingProgress = CreateTrainingProgress(
                out Grid trainingTrack,
                out BoxView trainingFill);

            activityCard.Children.Add(trainingProgress);

            Border cardPanel = GamePanel.Create(activityCard, 8);
            ActivityList.Children.Add(cardPanel);

            ActivityCardUI card = new()
            {
                Panel = cardPanel,
                TrainingProgress = trainingProgress,
                TrainingTrack = trainingTrack,
                TrainingFill = trainingFill,
                RewardLabel = rewardLabel,
                EfficiencyLabel = efficiencyLabel,
                BadgeLabel = badgeLabel,
                UnlockLabel = unlockLabel,
                Activity = activity,
                WasUnlocked = meetsRequiredLevel
            };
            _activityCards[activity] = card;
        }

        UpdateActivityButtons();
    }

    private void UpdateActivityButtons()
    {
        SkillActivity[] unlockedActivities = _skill.Activities
            .Where(candidate => _skill.Level >= candidate.RequiredLevel).ToArray();
        SkillActivity? bestXp = unlockedActivities
            .OrderByDescending(ActivityMetrics.XpPerHour).FirstOrDefault();
        SkillActivity? fastest = unlockedActivities
            .OrderBy(ActivityMetrics.EffectiveActionTicks)
            .ThenByDescending(ActivityMetrics.XpPerHour).FirstOrDefault();

        foreach ((SkillActivity activity, GoldSliceButton button) in _activityButtons)
        {
            bool meetsRequiredLevel = _skill.Level >= activity.RequiredLevel;
            button.Text = GetButtonText(activity);
            button.IsEnabled = meetsRequiredLevel;
            button.Variant = _activityManager.CurrentActivity == activity
                ? GoldSliceButtonVariant.Green
                : meetsRequiredLevel
                    ? GoldSliceButtonVariant.Neutral
                    : GoldSliceButtonVariant.Red;

            if (_activityCards.TryGetValue(activity, out ActivityCardUI? card))
            {
                bool unlocked = _skill.Level >= activity.RequiredLevel;
                bool newlyUnlocked = unlocked && !card.WasUnlocked;
                UpdateActivityMetrics(card, unlocked, bestXp, fastest);

                bool isTraining = _activityManager.CurrentActivity == activity;
                card.Panel.BackgroundColor = isTraining
                    ? Color.FromArgb("#254F33")
                    : unlocked
                        ? Color.FromArgb("#E64A4A4A")
                        : Color.FromArgb("#292929");
                card.Panel.Opacity = isTraining || unlocked ? 1 : 0.62;
                card.Panel.Stroke = isTraining
                    ? Color.FromArgb("#42A85A")
                    : unlocked
                        ? Color.FromArgb("#D99032")
                        : Color.FromArgb("#555555");
                card.TrainingProgress.IsVisible = isTraining;
                UpdateTrainingProgress(card);

                if (newlyUnlocked)
                    _ = PlayUnlockAnimationAsync(card);

                card.WasUnlocked = unlocked;
            }
        }
    }

    private void UpdateActivityMetrics(ActivityCardUI card, bool unlocked,
        SkillActivity? bestXp, SkillActivity? fastest)
    {
        SkillActivity activity = card.Activity;
        card.EfficiencyLabel.Text =
            $"{ActivityMetrics.FormatRate(ActivityMetrics.XpPerHour(activity))} XP/hr" +
            (activity.ItemReward == null
                ? ""
                : $"  •  {ActivityMetrics.FormatRate(ActivityMetrics.ItemsPerHour(activity))}/hr {activity.ItemReward.Name}");
        string bonus = ProgressionBonuses.Format(ProgressionBonuses.SkillXpPercent(_skill));
        if (!string.IsNullOrEmpty(bonus))
            card.EfficiencyLabel.Text += $"  •  {bonus}";

        List<string> badges = new();
        if (unlocked && bestXp == activity)
        {
            badges.Add("★ BEST XP/HR");
        }

        if (unlocked && fastest == activity)
        {
            badges.Add("⚡ FASTEST");
        }

        card.BadgeLabel.Text = string.Join("  •  ", badges);
        card.BadgeLabel.IsVisible = badges.Count > 0;

        if (unlocked)
        {
            card.UnlockLabel.Text = "";
            card.UnlockLabel.IsVisible = false;
            return;
        }

        double xpPerHour = bestXp == null
            ? 0
            : ActivityMetrics.XpPerHour(bestXp);
        double xpNeeded = Math.Max(
            0,
            ExperienceTable.GetXPForLevel(activity.RequiredLevel) - _skill.XP);
        double secondsToUnlock = xpPerHour <= 0
            ? 0
            : xpNeeded / xpPerHour * 3600d;
        card.UnlockLabel.Text =
            $"Unlocks at level {activity.RequiredLevel}" +
            (secondsToUnlock > 0
                ? $"  •  ~{ActivityMetrics.FormatDuration(secondsToUnlock)} at current pace"
                : "");
        card.UnlockLabel.IsVisible = true;
    }

    // ============================================================
    // BUTTON TEXT
    // ============================================================

    private string GetButtonText(
        SkillActivity activity)
    {
        if (_skill.Level <
            activity.RequiredLevel)
        {
            return
                $"Locked - Level {activity.RequiredLevel}";
        }

        if (_activityManager.CurrentActivity ==
            activity)
        {
            return "TRAINING";
        }

        return "TRAIN";
    }

    private static Border CreateTrainingProgress(
        out Grid track,
        out BoxView fill)
    {
        track = new Grid
        {
            HeightRequest = 7,
            BackgroundColor = Color.FromArgb("#8B1F1F")
        };

        fill = new BoxView
        {
            BackgroundColor = Color.FromArgb("#42A85A"),
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Fill
        };
        track.Children.Add(fill);

        Border progress = new()
        {
            Stroke = Color.FromArgb("#D99032"),
            StrokeThickness = 1,
            Padding = new Thickness(1),
            Margin = new Thickness(0, 2, 0, 0),
            IsVisible = false,
            Content = track
        };

        return progress;
    }

    private void OnTrainingProgressTick(object? sender, EventArgs e)
    {
        if (!_isActive ||
            _activityManager.CurrentActivity is not SkillActivity activity ||
            !_activityCards.TryGetValue(activity, out ActivityCardUI? card))
        {
            return;
        }

        UpdateTrainingProgress(card);
    }

    private void EnsureTrainingProgressTimer()
    {
        if (_trainingProgressTimer != null)
            return;

        _trainingProgressTimer = Dispatcher.CreateTimer();
        _trainingProgressTimer.Interval = TimeSpan.FromMilliseconds(200);
        _trainingProgressTimer.Tick += OnTrainingProgressTick;
        _trainingProgressTimer.Start();
    }

    private void StopTrainingProgressTimer()
    {
        if (_trainingProgressTimer == null)
            return;

        _trainingProgressTimer.Stop();
        _trainingProgressTimer.Tick -= OnTrainingProgressTick;
        _trainingProgressTimer = null;
    }

    private void UpdateTrainingProgress(ActivityCardUI card)
    {
        TimeSpan duration = _activityManager.ActionEnds - _activityManager.ActionStarted;
        if (duration <= TimeSpan.Zero || card.TrainingTrack.Width <= 0)
        {
            card.TrainingFill.WidthRequest = 0;
            return;
        }

        double progress = Math.Clamp(
            (DateTime.UtcNow - _activityManager.ActionStarted).TotalMilliseconds /
            duration.TotalMilliseconds,
            0,
            1);
        double fillWidth = card.TrainingTrack.Width * progress;
        if (Math.Abs(card.TrainingFill.WidthRequest - fillWidth) > 0.5)
            card.TrainingFill.WidthRequest = fillWidth;
    }

    private static async Task PlayActionCompleteFeedbackAsync(ActivityCardUI card)
    {
        if (card.FeedbackPlaying)
            return;
        card.FeedbackPlaying = true;
        try
        {
            Brush? originalStroke = card.Panel.Stroke;
            card.Panel.Stroke = Color.FromArgb("#FF9D2E");

            if (card.RewardLabel != null)
            {
                Color originalRewardColor = card.RewardLabel.TextColor;
                card.RewardLabel.TextColor = Color.FromArgb("#FF9D2E");
                await card.RewardLabel.FadeToAsync(0.55, 70, Easing.CubicOut);
                await card.RewardLabel.FadeToAsync(1, 180, Easing.CubicIn);
                card.RewardLabel.TextColor = originalRewardColor;
            }

            await Task.WhenAll(
                card.Panel.ScaleToAsync(1.018, 75, Easing.CubicOut),
                card.TrainingProgress.FadeToAsync(0.65, 75, Easing.CubicOut));
            await Task.WhenAll(
                card.Panel.ScaleToAsync(1, 150, Easing.CubicIn),
                card.TrainingProgress.FadeToAsync(1, 150, Easing.CubicIn));

            card.Panel.Stroke = originalStroke;
        }
        catch
        {
            card.Panel.Scale = 1;
        }
        finally
        {
            card.FeedbackPlaying = false;
        }
    }

    private static async Task PlayUnlockAnimationAsync(ActivityCardUI card)
    {
        try
        {
            Brush? originalStroke = card.Panel.Stroke;
            card.Panel.Stroke = Color.FromArgb("#FFE26A");
            card.UnlockLabel.Text = "✦ UNLOCKED!";
            card.UnlockLabel.TextColor = Color.FromArgb("#FFE26A");
            card.UnlockLabel.Opacity = 0;
            card.UnlockLabel.IsVisible = true;

            await Task.WhenAll(
                card.Panel.ScaleToAsync(1.06, 180, Easing.CubicOut),
                card.UnlockLabel.FadeToAsync(1, 180, Easing.CubicOut));
            await Task.WhenAll(
                card.Panel.ScaleToAsync(1, 220, Easing.CubicInOut),
                card.UnlockLabel.FadeToAsync(0, 420, Easing.CubicIn));

            card.Panel.Stroke = originalStroke;
            card.UnlockLabel.IsVisible = false;
            card.UnlockLabel.Text = string.Empty;
        }
        catch
        {
            card.Panel.Scale = 1;
            card.UnlockLabel.IsVisible = false;
            card.UnlockLabel.Text = string.Empty;
        }
    }

    // ============================================================
    // START ACTIVITY
    // ============================================================

    private void StartActivity(
        SkillActivity activity)
    {
        _activityManager.StartActivity(
            _skill,
            activity);

        UpdateActivityButtons();
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null)
            SetActive(false);
    }

    private sealed class ActivityCardUI
    {
        public required Border Panel { get; init; }
        public required Border TrainingProgress { get; init; }
        public bool FeedbackPlaying { get; set; }
        public required Grid TrainingTrack { get; init; }
        public required BoxView TrainingFill { get; init; }
        public Label? RewardLabel { get; init; }
        public required SkillActivity Activity { get; init; }
        public required Label EfficiencyLabel { get; init; }
        public required Label BadgeLabel { get; init; }
        public required Label UnlockLabel { get; init; }
        public bool WasUnlocked { get; set; }
    }
}
