namespace OSRSIdle;

public partial class SettingsView : ContentView
{
    private readonly Action _resetProgress;
    private readonly Action _returnToCharacterSelect;
    private bool _updatingDebugSwitches;
    private int _resetConfirmationStep;

    public SettingsView(Action resetProgress, Action returnToCharacterSelect)
    {
        InitializeComponent();
        _resetProgress = resetProgress;
        _returnToCharacterSelect = returnToCharacterSelect;

        GameClock.SpeedChanged += OnDebugSettingsChanged;
        DebugSettings.Changed += OnDebugSettingsChanged;
        UpdateDebugSwitches();
    }

    public void Dispose()
    {
        GameClock.SpeedChanged -= OnDebugSettingsChanged;
        DebugSettings.Changed -= OnDebugSettingsChanged;
    }

    private void OnDebugSpeedToggled(object? sender, ToggledEventArgs e)
    {
        if (_updatingDebugSwitches)
            return;
        if (e.Value)
            GameClock.SetDebugSpeedMultiplier(GameClock.DebugSpeedMultiplier);
        else if (!ExtremeDebugSpeedSwitch.IsToggled)
            GameClock.SetDebugSpeedEnabled(false);
    }

    private void OnExtremeDebugSpeedToggled(object? sender, ToggledEventArgs e)
    {
        if (_updatingDebugSwitches)
            return;
        if (e.Value)
            GameClock.SetDebugSpeedMultiplier(GameClock.ExtremeDebugSpeedMultiplier);
        else if (!DebugSpeedSwitch.IsToggled)
            GameClock.SetDebugSpeedEnabled(false);
    }

    private void OnInstakillToggled(object? sender, ToggledEventArgs e)
    {
        if (!_updatingDebugSwitches)
            DebugSettings.SetInstakillEnabled(e.Value);
    }

    private void OnDebugSettingsChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdateDebugSwitches);
    }

    private void UpdateDebugSwitches()
    {
        _updatingDebugSwitches = true;
        try
        {
            DebugSpeedSwitch.IsToggled =
                GameClock.IsDebugSpeedEnabled &&
                GameClock.SpeedMultiplier == GameClock.DebugSpeedMultiplier;
            ExtremeDebugSpeedSwitch.IsToggled =
                GameClock.IsDebugSpeedEnabled &&
                GameClock.SpeedMultiplier == GameClock.ExtremeDebugSpeedMultiplier;
            InstakillSwitch.IsToggled = DebugSettings.IsInstakillEnabled;
        }
        finally
        {
            _updatingDebugSwitches = false;
        }
    }

    private void OnReturnToCharacterSelectClicked(object? sender, EventArgs e)
    {
        _returnToCharacterSelect();
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

        ResetConfirmationOverlay.IsVisible = false;
        _resetConfirmationStep = 0;
        _resetProgress();
    }

    private void UpdateResetConfirmation()
    {
        bool finalStep = _resetConfirmationStep == 2;
        ResetConfirmationTitle.Text = finalStep ? "FINAL CONFIRMATION" : "RESET CHARACTER?";
        ResetConfirmationMessage.Text = finalStep
            ? "This cannot be undone. Permanently delete this character and every saved item, level, kill, and collection entry?"
            : "Resetting permanently deletes this character, inventory, equipment, levels, collection log, and kill counts. Continue to the final confirmation?";
        ResetConfirmationButton.Text = finalStep ? "Delete Forever" : "Continue";
    }
}
