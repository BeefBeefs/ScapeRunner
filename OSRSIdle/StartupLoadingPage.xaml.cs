namespace OSRSIdle;

public partial class StartupLoadingPage : ContentPage
{
    private bool _started;
    private double _progress;

    public StartupLoadingPage()
    {
        InitializeComponent();

        LoadingProgressTrack.SizeChanged += (_, _) => UpdateProgressWidth();
        SizeChanged += (_, _) => UiLayoutMetrics.Update(Width, Height);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_started)
            return;

        _started = true;
        await Task.Yield();

        UiLayoutMetrics.Update(
            Width > 0 ? Width : 450,
            Height > 0 ? Height : 800);

        Progress<StartupProgress> progress = new(UpdateProgress);

        try
        {
            await ((App)Application.Current!).InitializeStartupAsync(progress);
        }
        catch (Exception exception)
        {
            LoadingStageLabel.Text = "Startup failed";
            await CustomDialogService.ShowAsync(
                "Startup failed",
                exception.Message,
                "OK");
        }
    }

    private void UpdateProgress(StartupProgress update)
    {
        _progress = Math.Clamp(update.Value, 0, 1);
        LoadingStageLabel.Text = update.Stage;
        LoadingPercentLabel.Text = $"{_progress:P0}";
        UpdateProgressWidth();
    }

    private void UpdateProgressWidth()
    {
        LoadingProgressFill.WidthRequest =
            Math.Max(0, LoadingProgressTrack.Width * _progress);
    }
}
