namespace OSRSIdle;

public partial class LandingPage : ContentPage
{
    private int _resetConfirmationStep;
    private string _currentCharacterName = "Adventurer";
    private DateTime _offlineSavedAtUtc;
    private IDispatcherTimer? _offlineTickTimer;

    public LandingPage()
    {
        InitializeComponent();

        RefreshPreview();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshPreview();
        FireflyLayer.Start();
        StartOfflineTickTimer();
    }

    protected override void OnDisappearing()
    {
        FireflyLayer.Stop();
        _offlineTickTimer?.Stop();
        base.OnDisappearing();
    }

    private void RefreshPreview()
    {
        App app = (App)Application.Current!;

        SavePreview preview = SaveManager.GetPreview(app.Game.Player);

        _currentCharacterName = preview.CharacterName;

        app.Game.Player.PortraitIndex =
            PlayerPortraits.NormalizeIndex(preview.PortraitIndex);

        CharacterNameLabel.Text = preview.CharacterName.ToUpperInvariant();
        CharacterNameEntry.Text = preview.CharacterName;
        CharacterPortraitImage.Source = app.Game.Player.PortraitImage;
        int maximumSkillTotal = app.Game.Player.GetAllSkills().Count() * 99;
        SkillTotalLabel.Text =
            $"{preview.SkillTotal:N0} / {maximumSkillTotal:N0}";
        TotalKillsLabel.Text = preview.TotalKills.ToString("N0");
        CollectionLogLabel.Text =
            $"{preview.ObtainedCollectionEntries:N0} / " +
            $"{preview.TotalCollectionEntries:N0}";
        LastActivityLabel.Text = preview.LastActivity;
        OfflineTicksLabel.Text = preview.OfflineTicks.ToString("N0");
        _offlineSavedAtUtc = preview.SavedAtUtc;
        SaveStatusLabel.Text = SaveManager.RecoveredFromBackup
            ? "Your previous save was recovered from a backup."
            : SaveManager.LastError;
        SaveStatusLabel.IsVisible = !string.IsNullOrWhiteSpace(SaveStatusLabel.Text);
    }

    private void StartOfflineTickTimer()
    {
        _offlineTickTimer ??= Dispatcher.CreateTimer();
        _offlineTickTimer.Interval = TimeSpan.FromMilliseconds(600);
        _offlineTickTimer.Tick -= OnOfflineTickTimerTick;
        _offlineTickTimer.Tick += OnOfflineTickTimerTick;
        _offlineTickTimer.Start();
    }

    private void OnOfflineTickTimerTick(object? sender, EventArgs e)
    {
        if (_offlineSavedAtUtc == default)
            return;

        OfflineTicksLabel.Text =
            SaveManager.GetElapsedTicks(_offlineSavedAtUtc).ToString("N0");
    }

    private void OnCharacterTapped(object? sender, TappedEventArgs e)
    {
        ((App)Application.Current!).OpenGame();
    }

    private void OnSaveNameClicked(object? sender, EventArgs e)
    {
        string name = CharacterNameEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name))
            return;

        ((App)Application.Current!).RenameCharacter(name);

        RefreshPreview();
        SetRenameExpanded(false);
    }

    private void OnToggleRenameClicked(object? sender, EventArgs e)
    {
        SetRenameExpanded(!RenamePanel.IsVisible);
    }

    private void OnCancelRenameClicked(object? sender, EventArgs e)
    {
        CharacterNameEntry.Text = _currentCharacterName;
        SetRenameExpanded(false);
    }

    private void SetRenameExpanded(bool isExpanded)
    {
        RenamePanel.IsVisible = isExpanded;
        ChangeNameButton.Text = isExpanded
            ? "Hide Name Editor"
            : "Change Character Name";

        if (isExpanded)
        {
            CharacterNameEntry.Focus();
            CharacterNameEntry.CursorPosition = CharacterNameEntry.Text?.Length ?? 0;
        }
    }

    private void OnPreviousPortraitClicked(object? sender, EventArgs e)
    {
        ChangePortrait(-1);
    }

    private void OnNextPortraitClicked(object? sender, EventArgs e)
    {
        ChangePortrait(1);
    }

    private void ChangePortrait(int offset)
    {
        App app = (App)Application.Current!;
        int portraitIndex = PlayerPortraits.NormalizeIndex(
            app.Game.Player.PortraitIndex + offset);

        app.SelectCharacterPortrait(portraitIndex);
        CharacterPortraitImage.Source = app.Game.Player.PortraitImage;
    }

    private void OnResetProgressClicked(object? sender, EventArgs e)
    {
        _resetConfirmationStep = 1;
        UpdateResetConfirmation();
        ResetConfirmationOverlay.IsVisible = true;
    }

    private void OnCancelResetClicked(object? sender, EventArgs e)
    {
        ResetConfirmationOverlay.IsVisible = false;
        _resetConfirmationStep = 0;
    }

    private void OnConfirmResetClicked(object? sender, EventArgs e)
    {
        if (_resetConfirmationStep == 1)
        {
            _resetConfirmationStep = 2;
            UpdateResetConfirmation();
            return;
        }

        if (_resetConfirmationStep != 2)
            return;

        ((App)Application.Current!).ResetProgress();

        ResetConfirmationOverlay.IsVisible = false;
        _resetConfirmationStep = 0;
        SetRenameExpanded(false);
        RefreshPreview();
    }

    private void UpdateResetConfirmation()
    {
        bool isFinalStep = _resetConfirmationStep == 2;

        ResetConfirmationTitle.Text = isFinalStep
            ? "FINAL CONFIRMATION"
            : "RESET PROGRESS?";
        ResetConfirmationMessage.Text = isFinalStep
            ? "This cannot be undone. Permanently delete this character and every saved item, level, kill, and collection entry?"
            : "Resetting permanently deletes your character, inventory, equipment, levels, collection log, and kill counts. Continue to the final confirmation?";
        ResetConfirmationButton.Text = isFinalStep
            ? "Delete Forever"
            : "Continue";
    }
}
