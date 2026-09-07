namespace OSRSIdle;

public static class GameThemeCache
{
    private static readonly IReadOnlyDictionary<DropRarity, Color> RarityColors =
        new Dictionary<DropRarity, Color>
        {
            [DropRarity.Common] = Colors.White,
            [DropRarity.Uncommon] = Color.FromArgb("#62C7FF"),
            [DropRarity.Rare] = Color.FromArgb("#C882FF"),
            [DropRarity.VeryRare] = Color.FromArgb("#FF87C2"),
            [DropRarity.SuperRare] = Color.FromArgb("#FFD24A"),
            [DropRarity.MegaRare] = Color.FromArgb("#FF6868")
        };

    public static Color GetRarityColor(DropRarity rarity)
    {
        return RarityColors.TryGetValue(rarity, out Color? color)
            ? color
            : Colors.White;
    }

    public static Color GetItemRarityColor(Item item)
    {
        return GetRarityColor(RarityVisuals.GetBaseRarity(item));
    }

    public static void WarmUp()
    {
        _ = GetRarityColor(DropRarity.Common);
    }
}
