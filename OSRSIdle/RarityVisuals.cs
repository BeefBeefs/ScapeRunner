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
        if (rarity < DropRarity.Rare)
            return visual;

        Color rarityColor = GameThemeCache.GetRarityColor(rarity);
        Border glow = new()
        {
            Stroke = rarityColor,
            StrokeThickness = Math.Max(1, size / 18),
            BackgroundColor = rarityColor.WithAlpha(0.08f),
            Opacity = GetGlowOpacity(rarity),
            InputTransparent = true,
            WidthRequest = size * 0.86,
            HeightRequest = size * 0.86,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        visual.Children.Add(glow);

        StartPulse(visual, glow, rarity);
        return visual;
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

        return (StartupDataCache.IsInitialized
                ? StartupDataCache.Enemies
                : EnemyData.AllEnemies)
            .SelectMany(enemy => enemy.DropTable.Drops)
            .Where(drop => string.Equals(
                GetBaseName(drop.Item.Name),
                GetBaseName(item.Name),
                StringComparison.Ordinal))
            .Select(drop => drop.Rarity)
            .DefaultIfEmpty(DropRarity.Common)
            .Max();
    }

    private static void StartPulse(
        Grid visual,
        Border glow,
        DropRarity rarity)
    {
        uint duration = rarity >= DropRarity.MegaRare ? 780u : 1200u;
        bool running = true;

        // Item visuals are frequently rebuilt as virtualized lists recycle
        // cells. Stop the animation as soon as the visual leaves the visual
        // tree so detached cells do not retain an endless animation callback.
        void OnHandlerChanged(object? sender, EventArgs args)
        {
            if (visual.Handler == null)
            {
                StopPulse(glow);
                running = false;
            }
            else if (!running)
            {
                CommitPulse(glow, rarity, duration);
                running = true;
            }
        }

        visual.HandlerChanged += OnHandlerChanged;
        CommitPulse(glow, rarity, duration);
    }

    private static void CommitPulse(
        Border glow,
        DropRarity rarity,
        uint duration)
    {
        new Animation
        {
            { 0.0, 0.5, new Animation(value => glow.Opacity = value, 0.18, GetGlowOpacity(rarity)) },
            { 0.5, 1.0, new Animation(value => glow.Opacity = value, GetGlowOpacity(rarity), 0.18) }
        }.Commit(glow, "rarityGlow", 16, duration, Easing.SinInOut, repeat: () => true);

    }

    private static void StopPulse(Border glow)
    {
        glow.AbortAnimation("rarityGlow");
    }

    private static double GetGlowOpacity(DropRarity rarity) => rarity switch
    {
        DropRarity.Uncommon => 0.38,
        DropRarity.Rare => 0.50,
        DropRarity.VeryRare => 0.60,
        DropRarity.SuperRare => 0.72,
        DropRarity.MegaRare => 0.86,
        _ => 0.18
    };

    private static string GetBaseName(string itemName)
    {
        int marker = itemName.LastIndexOf(" +", StringComparison.Ordinal);
        return marker >= 0 && int.TryParse(itemName[(marker + 2)..], out _)
            ? itemName[..marker]
            : itemName;
    }
}
