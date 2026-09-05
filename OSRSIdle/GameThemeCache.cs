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
        DropRarity rarity = (StartupDataCache.IsInitialized
                ? StartupDataCache.Enemies
                : EnemyData.AllEnemies)
            .SelectMany(enemy => enemy.DropTable.Drops)
            .Where(drop => string.Equals(
                GetBaseItemName(drop.Item.Name),
                GetBaseItemName(item.Name),
                StringComparison.Ordinal))
            .Select(drop => drop.Rarity)
            .DefaultIfEmpty(DropRarity.Common)
            .Max();

        return GetRarityColor(rarity);
    }

    private static string GetBaseItemName(string itemName)
    {
        int marker = itemName.LastIndexOf(" +", StringComparison.Ordinal);
        return marker >= 0 &&
               int.TryParse(itemName[(marker + 2)..], out _)
            ? itemName[..marker]
            : itemName;
    }

    public static void WarmUp()
    {
        _ = GetRarityColor(DropRarity.Common);
    }
}
