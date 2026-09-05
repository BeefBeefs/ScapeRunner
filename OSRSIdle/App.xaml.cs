namespace OSRSIdle;

public partial class App : Application
{
    public Game Game { get; private set; } = null!;

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

        window.Destroying += (sender, e) => Game?.Save();

        return window;
    }

    public async Task InitializeStartupAsync(
        IProgress<StartupProgress> progress)
    {
        await StartupDataCache.InitializeAsync(progress);

        progress.Report(new StartupProgress(0.98, "Preparing character profile"));
        Game ??= new Game();

        progress.Report(new StartupProgress(1, "Ready"));

        // Allow the final progress update to render before changing pages.
        await Task.Delay(80);

        Window? window = Windows.FirstOrDefault();
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
        Game?.Save();

        base.OnSleep();
    }
}
