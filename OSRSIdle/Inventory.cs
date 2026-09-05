namespace OSRSIdle;

public class Inventory
{
    public const int StartingSlotCapacity = 28;

    // ============================================================
    // ITEMS
    // ============================================================

    public List<InventoryItem> Items { get; private set; }

    public int SlotCapacity { get; private set; } = StartingSlotCapacity;

    public int UsedSlots => Items.Count(entry =>
        entry.Quantity > 0 &&
        entry.Item.Type != ItemType.Pet);

    public bool IsFull => UsedSlots >= SlotCapacity;

    public int NextSlotCost
    {
        get
        {
            int purchasedSlots = SlotCapacity - StartingSlotCapacity;
            double rawCost = 100d * Math.Pow(1.5d, purchasedSlots);
            return (int)Math.Min(Math.Ceiling(rawCost), int.MaxValue);
        }
    }


    // ============================================================
    // CONSTRUCTOR
    // ============================================================
    public event Action? InventoryChanged;

    public Inventory()
    {
        Items = new List<InventoryItem>();
    }


    // ============================================================
    // ADD ITEM
    // ============================================================

    public bool AddItem(
        Item item,
        int quantity = 1)
    {
        if (quantity <= 0)
            return false;

        InventoryItem? existingItem =
            Items.FirstOrDefault(
                inventoryItem =>
                    AreSameStack(item, inventoryItem.Item));

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;

            InventoryChanged?.Invoke();

            return true;
        }

        if (!CanAddItem(item))
            return false;

        Items.Add(
            new InventoryItem(
                item,
                quantity));

        InventoryChanged?.Invoke();
        return true;
    }


    // ============================================================
    // REMOVE ITEM
    // ============================================================

    public bool RemoveItem(
        Item item,
        int quantity = 1)
    {
        if (quantity <= 0)
            return false;


        InventoryItem? existingItem =
            Items.FirstOrDefault(
                inventoryItem =>
                    AreSameStack(item, inventoryItem.Item));


        // --------------------------------------------------------
        // Item doesn't exist.
        // --------------------------------------------------------

        if (existingItem == null)
            return false;


        // --------------------------------------------------------
        // Not enough items.
        // --------------------------------------------------------

        if (existingItem.Quantity < quantity)
            return false;


        // --------------------------------------------------------
        // Remove quantity.
        // --------------------------------------------------------

        existingItem.Quantity -= quantity;


        // --------------------------------------------------------
        // Remove the inventory entry entirely if
        // there are none left.
        // --------------------------------------------------------

        if (existingItem.Quantity <= 0)
        {
            Items.Remove(existingItem);
        }

        InventoryChanged?.Invoke();

        return true;
    }


    // ============================================================
    // GET QUANTITY
    // ============================================================

    public int GetQuantity(
        Item item)
    {
        InventoryItem? existingItem =
            Items.FirstOrDefault(
                inventoryItem =>
                    AreSameStack(item, inventoryItem.Item));


        if (existingItem == null)
            return 0;


        return existingItem.Quantity;
    }


    // ============================================================
    // HAS ITEM
    // ============================================================

    public bool HasItem(
        Item item,
        int quantity = 1)
    {
        return GetQuantity(item) >= quantity;
    }

    public bool TryCombineEquipment(Item item, out Item upgradedItem)
    {
        upgradedItem = item;
        if (item.Type != ItemType.Equipment || item.UpgradeLevel >= 10)
            return false;

        InventoryItem? sourceStack = Items.FirstOrDefault(entry =>
            AreSameStack(item, entry.Item));

        if (sourceStack == null || sourceStack.Quantity < 2)
            return false;

        upgradedItem = item.CreateUpgrade();
        List<InventoryItem> finalStacks = new();
        int retainedCopies = sourceStack.Quantity - 2;

        if (retainedCopies > 0)
            finalStacks.Add(new InventoryItem(item, retainedCopies));

        finalStacks.Add(new InventoryItem(upgradedItem));

        if (!CanStoreCombination(sourceStack, finalStacks))
        {
            upgradedItem = item;
            return false;
        }

        ApplyCombination(sourceStack, finalStacks);
        InventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Combines every possible pair from one equipment stack. Generated
    /// upgrades are combined again until no further pair remains, while an
    /// odd copy at any level is retained.
    /// </summary>
    public bool TryCombineAllEquipment(
        Item item,
        out EquipmentCombineAllResult result)
    {
        result = EquipmentCombineAllResult.None;

        if (item.Type != ItemType.Equipment || item.UpgradeLevel >= 10)
            return false;

        InventoryItem? sourceStack = Items.FirstOrDefault(entry =>
            AreSameStack(item, entry.Item));

        if (sourceStack == null || sourceStack.Quantity < 4)
            return false;

        int sourceQuantity = sourceStack.Quantity;
        int sourceRemainder = sourceQuantity;
        Item currentLevelItem = item;
        List<InventoryItem> finalStacks = new();
        List<InventoryItem> createdStacks = new();

        while (true)
        {
            if (currentLevelItem.UpgradeLevel >= 10)
            {
                AddResultStack(finalStacks, currentLevelItem, sourceRemainder);
                AddResultStack(createdStacks, currentLevelItem, sourceRemainder);
                break;
            }

            int remainder = sourceRemainder % 2;
            if (remainder > 0)
            {
                AddResultStack(finalStacks, currentLevelItem, remainder);

                if (currentLevelItem.UpgradeLevel > item.UpgradeLevel)
                    AddResultStack(createdStacks, currentLevelItem, remainder);
            }

            int upgradedCount = sourceRemainder / 2;
            if (upgradedCount == 0)
                break;

            currentLevelItem = currentLevelItem.CreateUpgrade();
            sourceRemainder = upgradedCount;
        }

        int retainedBaseCopies = finalStacks
            .FirstOrDefault(entry => AreSameStack(entry.Item, item))
            ?.Quantity ?? 0;
        int baseCopiesConsumed = sourceQuantity - retainedBaseCopies;

        if (baseCopiesConsumed <= 0 || !CanStoreCombination(sourceStack, finalStacks))
            return false;

        ApplyCombination(sourceStack, finalStacks);
        result = new EquipmentCombineAllResult(
            baseCopiesConsumed,
            createdStacks);
        InventoryChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        Items.Clear();
        InventoryChanged?.Invoke();
    }

    public bool CanAddItem(Item item)
    {
        return item.Type == ItemType.Pet ||
               !IsFull;
    }

    // Upgraded equipment is reconstructed as a new Item instance each time
    // it is created or loaded. Inventory identity must therefore be based on
    // stable item data rather than reference equality.
    private static bool AreSameStack(Item left, Item right)
    {
        return left.Type == right.Type &&
               left.UpgradeLevel == right.UpgradeLevel &&
               string.Equals(left.Name, right.Name, StringComparison.Ordinal);
    }

    private static void AddResultStack(
        List<InventoryItem> stacks,
        Item item,
        int quantity)
    {
        if (quantity <= 0)
            return;

        InventoryItem? existing = stacks.FirstOrDefault(entry =>
            AreSameStack(entry.Item, item));

        if (existing == null)
            stacks.Add(new InventoryItem(item, quantity));
        else
            existing.Quantity += quantity;
    }

    private bool CanStoreCombination(
        InventoryItem sourceStack,
        IReadOnlyList<InventoryItem> finalStacks)
    {
        bool sourceIsRemoved = !finalStacks.Any(entry =>
            AreSameStack(entry.Item, sourceStack.Item));
        int usedSlotsAfterRemovingSource = UsedSlots -
            (sourceIsRemoved ? 1 : 0);

        int additionalSlots = finalStacks.Count(stack =>
            !AreSameStack(stack.Item, sourceStack.Item) &&
            !Items.Any(existing => AreSameStack(existing.Item, stack.Item)));

        return usedSlotsAfterRemovingSource + additionalSlots <= SlotCapacity;
    }

    private void ApplyCombination(
        InventoryItem sourceStack,
        IReadOnlyList<InventoryItem> finalStacks)
    {
        InventoryItem? retainedSource = finalStacks.FirstOrDefault(entry =>
            AreSameStack(entry.Item, sourceStack.Item));

        if (retainedSource == null)
            Items.Remove(sourceStack);
        else
            sourceStack.Quantity = retainedSource.Quantity;

        foreach (InventoryItem resultStack in finalStacks)
        {
            if (AreSameStack(resultStack.Item, sourceStack.Item))
                continue;

            InventoryItem? existing = Items.FirstOrDefault(entry =>
                AreSameStack(entry.Item, resultStack.Item));

            if (existing == null)
                Items.Add(new InventoryItem(resultStack.Item, resultStack.Quantity));
            else
                existing.Quantity += resultStack.Quantity;
        }
    }

    public bool TryPurchaseNextSlot()
    {
        int cost = NextSlotCost;

        if (!RemoveItem(ItemData.Coins, cost))
            return false;

        SlotCapacity++;
        InventoryChanged?.Invoke();
        return true;
    }

    public void RestoreSlotCapacity(int slotCapacity)
    {
        SlotCapacity = Math.Max(StartingSlotCapacity, slotCapacity);
    }
}

public readonly record struct EquipmentCombineAllResult(
    int ConsumedCopies,
    IReadOnlyList<InventoryItem> CreatedStacks)
{
    public static EquipmentCombineAllResult None =>
        new(0, Array.Empty<InventoryItem>());
}
