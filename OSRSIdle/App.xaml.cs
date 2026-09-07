namespace OSRSIdle;

public partial class App : Application
{
    public Game Game { get; private set; } = null!;

    private CombatManager? _preloadedCombatManager;
    private CombatView? _preloadedCombatView;

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = new(new StartupLoadingPage());

#if WINDOWS
        // Start desktop builds in a comfortable portrait-phone footprint.
        window.Width = 450;
        window.Height = 800;
#endif

        window.Destroying += (sender, e) =>
        {
            GameClock.SetDebugSpeedEnabled(false);
            Game?.Save();
            _preloadedCombatView?.Dispose();
            _preloadedCombatManager?.Dispose();
        };

        return window;
    }

    public async Task InitializeStartupAsync(
        IProgress<StartupProgress> progress)
    {
        await StartupDataCache.InitializeAsync(progress);

        Window? window = Windows.FirstOrDefault();
        if (window?.Page is StartupLoadingPage startupLoadingPage)
        {
            await startupLoadingPage.WarmCombatImagesAsync(
                StartupDataCache.Enemies,
                progress);
        }

        progress.Report(new StartupProgress(0.985, "Preparing character profile"));
        Game ??= new Game();

        progress.Report(new StartupProgress(0.99, "Preparing combat enemy list"));
        _preloadedCombatManager ??= new CombatManager(Game.Player);
        _preloadedCombatView ??= new CombatView(
            Game.Player,
            _preloadedCombatManager);
        _preloadedCombatView.PreloadEnemyList();

        progress.Report(new StartupProgress(1, "Ready"));

        // Allow the final progress update to render before changing pages.
        await Task.Delay(80);

        if (window != null)
            window.Page = new LandingPage();
    }

    public void OpenGame()
    {
        Game.Load();

        Window? window = Windows.FirstOrDefault();

        if (window != null)
        {
            window.Page = new AppShell();
        }
    }

    public (CombatManager? Manager, CombatView? View)
        TakePreloadedCombatSession()
    {
        CombatManager? manager = _preloadedCombatManager;
        CombatView? view = _preloadedCombatView;
        _preloadedCombatManager = null;
        _preloadedCombatView = null;
        return (manager, view);
    }

    public void RenameCharacter(string name)
    {
        Game.Player.Name = name;

        if (Game.HasStarted)
        {
            Game.Save();
        }
        else
        {
            SaveManager.UpdateCharacterProfile(characterName: name);
        }
    }

    public void SelectCharacterPortrait(int portraitIndex)
    {
        Game.Player.PortraitIndex =
            PlayerPortraits.NormalizeIndex(portraitIndex);

        if (Game.HasStarted)
        {
            Game.Save();
        }
        else
        {
            SaveManager.UpdateCharacterProfile(
                portraitIndex: Game.Player.PortraitIndex);
        }
    }

    public void ResetProgress()
    {
        Game.Reset();
    }

    protected override void OnSleep()
    {
        GameClock.SetDebugSpeedEnabled(false);
        Game?.Save();

        base.OnSleep();
    }
}
