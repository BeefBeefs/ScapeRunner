namespace OSRSIdle;

public partial class SkillsView : ContentView
{
    private readonly Player _player;
    private readonly ActivityManager _activityManager;

    // Keep references to the UI elements for each skill.
    private readonly Dictionary<Skill, SkillUI> _skillUI =
        new();

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public SkillsView(
        Player player,
        ActivityManager activityManager)
    {
        InitializeComponent();

        _player = player;
        _activityManager = activityManager;

        _activityManager.XPChanged +=
            OnXPChanged;

        _activityManager.ActivityChanged +=
            OnActivityChanged;

        BuildSkillList();
    }

    // ============================================================
    // BUILD SKILL LIST
    // ============================================================

    private void BuildSkillList()
    {
        SkillList.Children.Clear();

        _skillUI.Clear();

        foreach (Skill skill in _player.Skills)
        {
            VerticalStackLayout skillCard =
                new VerticalStackLayout
                {
                    Spacing = 3
                };

            // -------------------------
            // SKILL NAME
            // -------------------------

            Image skillIcon =
                new Image
                {
                    Source = skill.IconImage,
                    WidthRequest = 24,
                    HeightRequest = 24,
                    Aspect = Aspect.AspectFit
                };

            Label nameLabel =
                new Label
                {
                    Text = skill.Name,

                    FontSize = 19,

                    FontAttributes =
                        FontAttributes.Bold,

                    TranslationY = 3
                };

            HorizontalStackLayout skillNameRow =
                new HorizontalStackLayout
                {
                    Spacing = 6,
                    Children = { skillIcon, nameLabel }
                };

            // -------------------------
            // LEVEL
            // -------------------------

            Label levelLabel =
                new Label
                {
                    Text =
                        $"Level {skill.Level}",

                    FontSize = 15
                };

            // -------------------------
            // XP
            // -------------------------

            Label xpLabel =
                new Label
                {
                    Text =
                        $"{skill.XP:N0} XP",

                    FontSize = 14
                };

            // -------------------------
            // PROGRESS BAR
            // -------------------------

            Grid progressTrack = new Grid
            {
                HeightRequest = 7,
                BackgroundColor = Color.FromArgb("#2B2B2B")
            };

            BoxView progressFill = new BoxView
            {
                HeightRequest = 7,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Fill
            };

            progressTrack.Children.Add(progressFill);

            Border progressBar = new Border
            {
                Stroke = Color.FromArgb("#D99032"),
                StrokeThickness = 1,
                Padding = new Thickness(1),
                Content = progressTrack
            };

            progressTrack.SizeChanged += (sender, e) =>
                UpdateProgressBar(
                    progressTrack,
                    progressFill,
                    GetLevelProgress(skill));

            UpdateProgressBar(
                progressTrack,
                progressFill,
                GetLevelProgress(skill));

            // -------------------------
            // OPEN BUTTON
            // -------------------------

            GoldSliceButton openButton =
                new GoldSliceButton
                {
                    Text =
                        GetOpenButtonText(skill),

                    FontSize = 14,

                    Variant = IsSkillBeingTrained(skill)
                        ? GoldSliceButtonVariant.Green
                        : GoldSliceButtonVariant.Neutral,

                    CenterText = true,

                    TextColor = Colors.White
                };

            openButton.Clicked +=
                (sender, e) =>
                {
                    OpenSkill(skill);
                };

            // -------------------------
            // BUILD CARD
            // -------------------------

            skillCard.Children.Add(
                skillNameRow);

            skillCard.Children.Add(
                levelLabel);

            skillCard.Children.Add(
                xpLabel);

            skillCard.Children.Add(
                progressBar);

            skillCard.Children.Add(
                openButton);

            SkillList.Children.Add(
                GamePanel.Create(skillCard, 8));

            // -------------------------
            // REMEMBER UI
            // -------------------------

            _skillUI[skill] =
                new SkillUI
                {
                    LevelLabel = levelLabel,
                    XPLabel = xpLabel,
                    ProgressTrack = progressTrack,
                    ProgressFill = progressFill,
                    OpenButton = openButton
                };
        }
    }

    public void RefreshDisplay()
    {
        _activityManager.XPChanged -= OnXPChanged;
        _activityManager.XPChanged += OnXPChanged;
        _activityManager.ActivityChanged -= OnActivityChanged;
        _activityManager.ActivityChanged += OnActivityChanged;
        UpdateSkillUI();
    }

    // ============================================================
    // UPDATE SKILL UI
    // ============================================================

    private void UpdateSkillUI()
    {
        foreach (Skill skill in _player.Skills)
        {
            if (!_skillUI.TryGetValue(
                    skill,
                    out SkillUI? ui))
            {
                continue;
            }

            // Update level.
            ui.LevelLabel.Text =
                $"Level {skill.Level}";

            // Update XP.
            ui.XPLabel.Text =
                $"{skill.XP:N0} XP";

            UpdateProgressBar(
                ui.ProgressTrack,
                ui.ProgressFill,
                GetLevelProgress(skill));

            ui.OpenButton.Variant = IsSkillBeingTrained(skill)
                ? GoldSliceButtonVariant.Green
                : GoldSliceButtonVariant.Neutral;

            ui.OpenButton.Text = GetOpenButtonText(skill);
        }
    }

    private string GetOpenButtonText(Skill skill)
    {
        return IsSkillBeingTrained(skill)
            ? $"Training {skill.Name}"
            : $"Train {skill.Name}";
    }

    private bool IsSkillBeingTrained(Skill skill)
    {
        return _activityManager.IsActive &&
            _activityManager.CurrentSkill == skill;
    }

    // ============================================================
    // LEVEL PROGRESS
    // ============================================================

    private double GetLevelProgress(
        Skill skill)
    {
        int currentLevel =
            skill.Level;

        if (currentLevel >= 99)
            return 1;

        double currentLevelXP =
            ExperienceTable.GetXPForLevel(
                currentLevel);

        double nextLevelXP =
            ExperienceTable.GetXPForLevel(
                currentLevel + 1);

        if (nextLevelXP <= currentLevelXP)
            return 1;

        double progress =
            (skill.XP - currentLevelXP) /
            (nextLevelXP - currentLevelXP);

        return Math.Clamp(
            progress,
            0,
            1);
    }

    private static void UpdateProgressBar(
        Grid progressTrack,
        BoxView progressFill,
        double progress)
    {
        progress = Math.Clamp(progress, 0, 1);

        progressFill.BackgroundColor = progress <= 0.30
            ? Colors.Red
            : progress <= 0.70
                ? Colors.Yellow
                : Colors.Green;

        progressFill.WidthRequest =
            Math.Max(0, progressTrack.Width * progress);
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
            UpdateSkillUI();
        });
    }

    private void OnActivityChanged(
        object? sender,
        EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateSkillUI();
        });
    }

    // ============================================================
    // OPEN SKILL
    // ============================================================

    private void OpenSkill(
        Skill skill)
    {
        GamePage? gamePage =
            GetGamePage();

        gamePage?.ShowSkillPage(
            skill);
    }

    // ============================================================
    // FIND GAME PAGE
    // ============================================================

    private GamePage? GetGamePage()
    {
        Element? current = this;

        while (current != null)
        {
            if (current is GamePage gamePage)
                return gamePage;

            current = current.Parent;
        }

        return null;
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

            _activityManager.ActivityChanged -=
                OnActivityChanged;
        }
    }

    // ============================================================
    // UI REFERENCES
    // ============================================================

    private class SkillUI
    {
        public required Label LevelLabel { get; set; }

        public required Label XPLabel { get; set; }

        public required Grid ProgressTrack { get; set; }

        public required BoxView ProgressFill { get; set; }

        public required GoldSliceButton OpenButton { get; set; }
    }
}
