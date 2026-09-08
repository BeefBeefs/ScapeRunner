namespace OSRSIdle;

/// <summary>
/// Shared presentation for items whose rarity should remain visible wherever
/// the item is shown, including inventory and future bank-style screens.
/// </summary>
public static class RarityVisuals
{
    public static bool IsRainbowRare(double chance) => chance <= 0.0005d;

    public static FormattedString RainbowText(string text)
    {
        Color[] colors =
        {
            Color.FromArgb("#FF5C5C"), Color.FromArgb("#FFB347"),
            Color.FromArgb("#FFF45C"), Color.FromArgb("#68E06F"),
            Color.FromArgb("#5CB8FF"), Color.FromArgb("#B783FF"),
            Color.FromArgb("#FF7DC8")
        };
        FormattedString result = new();
        for (int index = 0; index < text.Length; index++)
            result.Spans.Add(new Span { Text = text[index].ToString(), TextColor = colors[index % colors.Length] });
        return result;
    }

    public static Grid CreateItemVisual(
        Item item,
        double size,
        bool revealed = true,
        DropRarity? rarityOverride = null)
    {
        Grid visual = new()
        {
            WidthRequest = size,
            HeightRequest = size,
            IsClippedToBounds = false
        };

        Image image = new()
        {
            Source = item.IconImage,
            WidthRequest = size,
            HeightRequest = size,
            Aspect = Aspect.AspectFit,
            IsVisible = revealed,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        visual.Children.Add(image);

        if (!revealed)
            return visual;

        DropRarity rarity = rarityOverride ?? GetRarity(item);
        Color rarityColor = GameThemeCache.GetRarityColor(rarity);
        Border rarityBox = new()
        {
            Stroke = rarityColor,
            StrokeThickness = 2,
            BackgroundColor = Colors.Transparent,
            InputTransparent = true,
            WidthRequest = size,
            HeightRequest = size,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        visual.Children.Add(rarityBox);
        return visual;
    }

    public static Border CreateUndiscoveredItemVisual(
        double size)
    {
        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            BackgroundColor = Colors.Black,
            Stroke = Color.FromArgb("#FF000000"),
            StrokeThickness = 1,
            InputTransparent = true
        };
    }

    public static void ReplaceImage(
        Grid host,
        Image image,
        Item item,
        int column = 0,
        bool revealed = true)
    {
        host.Children.Remove(image);
        Grid visual = CreateItemVisual(item, image.WidthRequest, revealed);
        host.Add(visual, column);
    }

    public static DropRarity GetRarity(Item item)
    {
        if (StartupDataCache.IsInitialized &&
            StartupDataCache.MaxDropRarityByItem.TryGetValue(
                item,
                out DropRarity cachedRarity))
        {
            return cachedRarity;
        }

        return GetBaseRarity(item);
    }

    public static DropRarity GetBaseRarity(Item item)
    {
        string baseName = GetBaseName(item.Name);
        if (StartupDataCache.IsInitialized)
            return StartupDataCache.MaxDropRarityByBaseName.TryGetValue(baseName, out DropRarity rarity)
                ? rarity
                : DropRarity.Common;

        return EnemyData.AllEnemies
            .SelectMany(enemy => enemy.DropTable.Drops)
            .Where(drop => string.Equals(
                GetBaseName(drop.Item.Name),
                baseName,
                StringComparison.Ordinal))
            .Select(drop => drop.Rarity)
            .DefaultIfEmpty(DropRarity.Common)
            .Max();
    }

    internal static string GetBaseName(string itemName)
    {
        int marker = itemName.LastIndexOf(" +", StringComparison.Ordinal);
        return marker >= 0 && int.TryParse(itemName[(marker + 2)..], out _)
            ? itemName[..marker]
            : itemName;
    }
}
