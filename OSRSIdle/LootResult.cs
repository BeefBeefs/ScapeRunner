namespace OSRSIdle;

public class LootResult
{
    // ============================================================
    // ITEM
    // ============================================================

    public Item Item { get; }


    // ============================================================
    // QUANTITY
    // ============================================================

    public int Quantity { get; }


    // ============================================================
    // DROP CHANCE
    // ============================================================

    public double Chance { get; }

    public DropRarity Rarity { get; }


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public LootResult(
        Item item,
        int quantity,
        double chance,
        DropRarity rarity)
    {
        Item = item;

        Quantity = quantity;

        Chance = chance;

        Rarity = rarity;
    }
}
