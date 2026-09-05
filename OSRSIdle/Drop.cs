namespace OSRSIdle;

public enum DropRarity
{
    Common,
    Uncommon,
    Rare,
    VeryRare,
    SuperRare,
    MegaRare
}
public class Drop
{
    public Item Item { get; set; } = null!;

    public double Chance { get; set; }

    public int MinQuantity { get; set; }

    public int MaxQuantity { get; set; }

    public DropRarity Rarity { get; set; }
}