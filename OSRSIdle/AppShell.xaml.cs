namespace OSRSIdle;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    public void SetGamePageTitle(string title)
    {
        GameShellContent.Title = title;
    }
}
