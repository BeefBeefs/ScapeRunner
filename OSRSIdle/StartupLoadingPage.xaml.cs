namespace OSRSIdle;

public partial class StartupLoadingPage : ContentPage
{
    private static readonly string[] PageBackgroundImages =
    [
        "background_startup.png",
        "background_landing.png",
        "background_home.png",
        "background_skills.png",
        "background_skill.png",
        "background_combat.png",
        "background_inventory.png",
        "background_collection_log.png",
        "background_settings.png"
    ];

    private static readonly string[] CombatTierBannerImages =
    [
        "tier_greenvale.png",
        "tier_mirewood.png",
        "tier_frostpeak.png",
        "tier_emberfall.png",
        "tier_sandswept_ruins.png",
        "tier_astral_reach.png",
        "tier_umbral_expanse.png"
    ];

    private readonly List<Image> _warmupImages = new();
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

    public async Task WarmCombatImagesAsync(
        IReadOnlyList<Enemy> enemies,
        IProgress<StartupProgress> progress)
    {
        if (_warmupImages.Count > 0)
            return;

        List<string> imageSources = new();

        // Decode all page backdrops while the startup page is already
        // attached. The individual pages can then reuse the native image
        // loader/cache when they are first opened.
        imageSources.AddRange(PageBackgroundImages);

        // Warm the first combat areas in gameplay order. The compact and
        // large enemy images share the platform decoder cache with the later
        // combat cards; item and drop images intentionally stay lazy.
        foreach (EnemyTier tier in Enum.GetValues<EnemyTier>())
        {
            int tierIndex = (int)tier;
            if (tierIndex < CombatTierBannerImages.Length)
                imageSources.Add(CombatTierBannerImages[tierIndex]);

            foreach (Enemy enemy in enemies
                .Where(enemy => enemy.Tier == tier)
                .OrderBy(enemy => enemy.CombatLevel)
                .ThenBy(enemy => enemy.Name))
            {
                imageSources.Add(enemy.IconImage);
                imageSources.Add(enemy.LargeIconImage);
            }
        }

        // Avoid making the same native decode request twice if data ever
        // contains duplicate image paths.
        imageSources = imageSources
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (int index = 0; index < imageSources.Count; index++)
        {
            Image image = new()
            {
                Source = ImageSource.FromFile(imageSources[index]),
                WidthRequest = 1,
                HeightRequest = 1,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                InputTransparent = true,
                IsVisible = true
            };

            ImageWarmupLayer.Children.Add(image);
            _warmupImages.Add(image);

            if ((index + 1) % 8 == 0)
            {
                double warmupProgress = (index + 1) /
                    (double)Math.Max(1, imageSources.Count);
                progress.Report(new StartupProgress(
                    0.965 + warmupProgress * 0.015,
                    $"Warming combat images ({index + 1}/{imageSources.Count})"));

                // Give the native image handlers a UI turn to begin their
                // asynchronous decode before adding the next batch.
                await Task.Yield();
            }
        }

        progress.Report(new StartupProgress(0.98, "Combat images ready"));
        await Task.Yield();
    }

    private void UpdateProgressWidth()
    {
        LoadingProgressFill.WidthRequest =
            Math.Max(0, LoadingProgressTrack.Width * _progress);
    }
}
