namespace OSRSIdle;

public partial class SkillPage : ContentView
{
    private readonly Skill _skill;
    private readonly ActivityManager _activityManager;
    private readonly Dictionary<SkillActivity, GoldSliceButton> _activityButtons = new();
    private readonly Dictionary<SkillActivity, ActivityCardUI> _activityCards = new();
    private IDispatcherTimer? _trainingProgressTimer;

    public SkillPage(
        Skill skill,
        ActivityManager activityManager)
    {
        InitializeComponent();

        _skill = skill;
        _activityManager = activityManager;

        _activityManager.XPChanged += OnXPChanged;

        UpdateSkillHeader();
        BuildActivityList();
    }

    public void RefreshDisplay()
    {
        _activityManager.XPChanged -= OnXPChanged;
        _activityManager.XPChanged += OnXPChanged;

        EnsureTrainingProgressTimer();
        UpdateSkillHeader();
        UpdateActivityButtons();
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateSkillHeader();

            UpdateActivityButtons();
        });
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
                    $"{activity.ActionTicks} tick{(activity.ActionTicks == 1 ? "" : "s")}",

                FontSize = 14
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
                TrainingFill = trainingFill
            };
            _activityCards[activity] = card;
        }

        UpdateActivityButtons();
    }

    private void UpdateActivityButtons()
    {
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
                bool isTraining = _activityManager.CurrentActivity == activity;
                card.Panel.BackgroundColor = isTraining
                    ? Color.FromArgb("#254F33")
                    : Color.FromArgb("#E64A4A4A");
                card.Panel.Stroke = isTraining
                    ? Color.FromArgb("#42A85A")
                    : Color.FromArgb("#D99032");
                card.TrainingProgress.IsVisible = isTraining;
                UpdateTrainingProgress(card);
            }
        }
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
        if (_activityManager.CurrentActivity is not SkillActivity activity ||
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
        _trainingProgressTimer.Interval = TimeSpan.FromMilliseconds(100);
        _trainingProgressTimer.Tick += OnTrainingProgressTick;
        _trainingProgressTimer.Start();
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
        card.TrainingFill.WidthRequest = card.TrainingTrack.Width * progress;
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
        {
            _activityManager.XPChanged -=
                OnXPChanged;

            if (_trainingProgressTimer != null)
            {
                _trainingProgressTimer.Stop();
                _trainingProgressTimer.Tick -= OnTrainingProgressTick;
                _trainingProgressTimer = null;
            }
        }
    }

    private sealed class ActivityCardUI
    {
        public required Border Panel { get; init; }
        public required Border TrainingProgress { get; init; }
        public required Grid TrainingTrack { get; init; }
        public required BoxView TrainingFill { get; init; }
    }
}
