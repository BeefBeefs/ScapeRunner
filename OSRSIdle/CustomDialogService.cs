namespace OSRSIdle;

public static class CustomDialogService
{
    private static GamePage? _host;

    public static void SetHost(GamePage host)
    {
        _host = host;
    }

    public static Task<string?> ShowAsync(
        string title,
        string message,
        params string[] buttons)
    {
        return _host?.ShowCustomDialogAsync(title, message, buttons)
            ?? Task.FromResult<string?>(null);
    }

    public static Task<string?> ShowItemAsync(
        string title,
        string imageSource,
        string message,
        Color titleColor,
        params string[] buttons)
    {
        return _host?.ShowCustomDialogAsync(
                   title,
                   message,
                   buttons,
                   imageSource,
                   titleColor)
               ?? Task.FromResult<string?>(null);
    }
}
