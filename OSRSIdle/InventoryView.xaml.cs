namespace OSRSIdle;

public partial class InventoryView : ContentView
{
    // ============================================================
    // INVENTORY
    // ============================================================

    private readonly Inventory _inventory;

    private readonly Player _player;


    // ============================================================
    // CONSTANTS
    // ============================================================

    private const int Columns = 4;


    // ============================================================
    // CATEGORIES
    // ============================================================

    private enum InventoryCategory
    {
        Equipment,
        Food,
        Junk
    }

    private InventoryCategory _selectedCategory =
        InventoryCategory.Equipment;


    // ============================================================
    // SORTING
    // ============================================================

    private enum InventorySortMode
    {
        Name,
        Quantity,
        TotalValue
    }

    private InventorySortMode _sortMode =
        InventorySortMode.Name;

    private bool _sortAscending = true;
    private bool _useDefaultFoodSort;
    private bool _isActive;
    private bool _refreshPending;
    private readonly Dictionary<InventoryItem, InventorySlot> _slots = new();
    private readonly Grid _emptyCategoryHost = new();
    private readonly Label _emptyCategoryLabel = new()
    {
        TextColor = Color.FromArgb("#C8C8C8"),
        FontSize = 15,
        HorizontalOptions = LayoutOptions.Center,
        Margin = new Thickness(0, 20)
    };

    private sealed record InventorySlot(Border Card, Label Quantity);


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public InventoryView(
        Player player)
    {
        InitializeComponent();

        _player = player;

        _inventory =
            player.Inventory;
        // Labels acquire a shadow wrapper when parented. Retain an explicit
        // host so removal/reinsertion operates on the actual grid child.
        _emptyCategoryHost.Children.Add(_emptyCategoryLabel);


        // --------------------------------------------------------
        // Listen for inventory changes.
        // --------------------------------------------------------

        // --------------------------------------------------------
        // Initial inventory.
        // --------------------------------------------------------

        UpdateSortButtonText();
        UpdateCategoryTabs();
        UpdateInventoryCapacity();

        UpdateInventory();
    }


    // ============================================================
    // INVENTORY CHANGED
    // ============================================================

    private void OnInventoryChanged()
    {
        if (!_isActive || _refreshPending)
            return;

        _refreshPending = true;
        // Bulk sales, combining and loot can emit many events in one action.
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () =>
        {
            _refreshPending = false;
            if (_isActive)
                UpdateInventory();
        });
    }


    // ============================================================
    // SORT: NAME
    // ============================================================

    private void OnSortNameClicked(
        object? sender,
        EventArgs e)
    {
        _useDefaultFoodSort = false;
        SetSortMode(
            InventorySortMode.Name);
    }


    // ============================================================
    // SORT: QUANTITY
    // ============================================================

    private void OnSortQuantityClicked(
        object? sender,
        EventArgs e)
    {
        _useDefaultFoodSort = false;
        SetSortMode(
            InventorySortMode.Quantity);
    }


    // ============================================================
    // SORT: VALUE
    // ============================================================

    private void OnSortValueClicked(
        object? sender,
        EventArgs e)
    {
        _useDefaultFoodSort = false;
        SetSortMode(
            InventorySortMode.TotalValue);
    }


    // ============================================================
    // SET SORT MODE
    // ============================================================

    private void SetSortMode(
        InventorySortMode mode)
    {
        // Clicking the currently selected sort
        // reverses the direction.
        if (_sortMode == mode)
        {
            _sortAscending =
                !_sortAscending;
        }
        else
        {
            _sortMode =
                mode;

            _sortAscending =
                true;
        }


        UpdateSortButtonText();
        UpdateInventory();
    }


    // ============================================================
    // SORT DIRECTION
    // ============================================================

    private void OnSortDirectionClicked(
        object? sender,
        EventArgs e)
    {
        _useDefaultFoodSort = false;
        _sortAscending =
            !_sortAscending;

        UpdateSortButtonText();
        UpdateInventory();
    }


    // ============================================================
    // SORT BUTTON TEXT
    // ============================================================

    private void UpdateSortButtonText()
    {
        SortNameButton.Text =
            _sortMode == InventorySortMode.Name
                ? "Name ✓"
                : "Name";

        SortQuantityButton.Text =
            _sortMode == InventorySortMode.Quantity
                ? "Quantity ✓"
                : "Quantity";

        SortValueButton.Text =
            _sortMode == InventorySortMode.TotalValue
                ? "Gold Value ✓"
                : "Gold Value";


        SortDirectionButton.Text =
            _sortAscending
                ? "⬆ Ascending"
                : "⬇ Descending";

        bool defaultFoodSort =
            _selectedCategory == InventoryCategory.Food &&
            _useDefaultFoodSort;

        SetSortButtonAppearance(
            SortNameButton,
            _sortMode == InventorySortMode.Name && !defaultFoodSort);

        SetSortButtonAppearance(
            SortQuantityButton,
            _sortMode == InventorySortMode.Quantity);

        SetSortButtonAppearance(
            SortValueButton,
            _sortMode == InventorySortMode.TotalValue);

        if (defaultFoodSort)
            SortNameButton.Text = "Healing ✓";

        SortDirectionButton.Variant = GoldSliceButtonVariant.Neutral;
    }

    public void SetActive(bool active)
    {
        if (_isActive == active)
            return;

        _isActive = active;

        if (active)
            _inventory.InventoryChanged += OnInventoryChanged;
        else
            _inventory.InventoryChanged -= OnInventoryChanged;
    }

    public void RefreshDisplay()
    {
        UpdateInventory();
    }

    public void Dispose()
    {
        SetActive(false);
    }


    // ============================================================
    // CATEGORY TABS
    // ============================================================

    private void OnEquipmentTabClicked(
        object? sender,
        EventArgs e)
    {
        SetCategory(InventoryCategory.Equipment);
    }

    private void OnFoodTabClicked(
        object? sender,
        EventArgs e)
    {
        SetCategory(InventoryCategory.Food);
    }

    private void OnJunkTabClicked(
        object? sender,
        EventArgs e)
    {
        SetCategory(InventoryCategory.Junk);
    }

    private void SetCategory(InventoryCategory category)
    {
        if (_selectedCategory == category)
            return;

        _selectedCategory = category;
        if (category == InventoryCategory.Food)
        {
            _useDefaultFoodSort = true;
            _sortMode = InventorySortMode.Name;
            _sortAscending = true;
        }
        UpdateCategoryTabs();
        UpdateSortButtonText();
        UpdateInventory();
    }

    private void UpdateCategoryTabs()
    {
        SetTabButtonAppearance(
            EquipmentTabButton,
            _selectedCategory == InventoryCategory.Equipment);

        SetTabButtonAppearance(
            FoodTabButton,
            _selectedCategory == InventoryCategory.Food);

        SetTabButtonAppearance(
            JunkTabButton,
            _selectedCategory == InventoryCategory.Junk);
    }

    private static void SetTabButtonAppearance(
        GoldSliceButton button,
        bool isSelected)
    {
        button.Variant = isSelected
            ? GoldSliceButtonVariant.Green
            : GoldSliceButtonVariant.Neutral;

        button.TextColor = Colors.White;
        button.FontAttributes = isSelected
            ? FontAttributes.Bold
            : FontAttributes.None;
    }


    // ============================================================
    // INVENTORY CAPACITY
    // ============================================================

    private void UpdateInventoryCapacity()
    {
        int nextSlotCost = _inventory.NextSlotCost;
        int coinCount = _inventory.GetQuantity(ItemData.Coins);

        InventoryCapacityLabel.Text =
            $"Slots: {_inventory.UsedSlots} / {_inventory.SlotCapacity}";

        double capacityUsed = _inventory.SlotCapacity > 0
            ? (double)_inventory.UsedSlots / _inventory.SlotCapacity
            : 1;

        InventoryCapacityLabel.TextColor = capacityUsed >= 0.9
            ? Color.FromArgb("#E85A5A")
            : capacityUsed >= 0.7
                ? Color.FromArgb("#FFD24A")
                : Color.FromArgb("#65C875");

        InventoryCapacityHintLabel.Text =
            $"{coinCount:N0} coins";

        BuyInventorySlotButton.Text =
            $"Buy Slot ({nextSlotCost:N0})";

        // GoldSliceButton has pixel-art caps, so give the button enough room
        // for the formatted price as the exponential slot cost grows.
        double textWidth = (BuyInventorySlotButton.Text.Length *
                            BuyInventorySlotButton.FontSize * 0.56) + 24;
        BuyInventorySlotButton.WidthRequest = Math.Max(140, textWidth);

        BuyInventorySlotButton.IsEnabled = coinCount >= nextSlotCost;
    }

    private void OnBuyInventorySlotClicked(
        object? sender,
        EventArgs e)
    {
        if (_inventory.TryPurchaseNextSlot())
        {
            UpdateInventoryCapacity();
        }
    }

    private static void SetSortButtonAppearance(
        GoldSliceButton button,
        bool isSelected)
    {
        button.Variant = isSelected
            ? GoldSliceButtonVariant.Green
            : GoldSliceButtonVariant.Neutral;

        button.TextColor = Colors.White;
        button.FontAttributes = isSelected
            ? FontAttributes.Bold
            : FontAttributes.None;
    }


    // ============================================================
    // UPDATE INVENTORY
    // ============================================================

    private void UpdateInventory()
    {
        UpdateInventoryCapacity();
        UpdateCombineAllEquipmentButton();
        UpdateSellCategoryButtons();
        UpdateSellJunkButton();

    // --------------------------------------------------------
    // Get every item currently in the inventory.
    // --------------------------------------------------------

        List<InventoryItem> sortedItems =
            _inventory.Items
                .Where(item => item.Quantity > 0 &&
                               IsInSelectedCategory(item.Item))
                .ToList();


        // --------------------------------------------------------
        // Sort items.
        // --------------------------------------------------------

        if (_selectedCategory == InventoryCategory.Food &&
            _useDefaultFoodSort)
        {
            sortedItems = sortedItems
                .OrderBy(item => item.Item.HealingAmount)
                .ThenBy(item => item.Item.Name)
                .ToList();
        }
        else
        {
            switch (_sortMode)
            {
                case InventorySortMode.Name:

                    sortedItems =
                        _sortAscending

                            ? sortedItems
                                .OrderBy(item =>
                                    item.Item.Name)
                                .ToList()

                            : sortedItems
                                .OrderByDescending(item =>
                                    item.Item.Name)
                                .ToList();

                    break;


                case InventorySortMode.Quantity:

                    sortedItems =
                        _sortAscending

                            ? sortedItems
                                .OrderBy(item =>
                                    item.Quantity)
                                .ThenBy(item =>
                                    item.Item.Name)
                                .ToList()

                            : sortedItems
                                .OrderByDescending(item =>
                                    item.Quantity)
                                .ThenBy(item =>
                                    item.Item.Name)
                                .ToList();

                    break;


                case InventorySortMode.TotalValue:

                    sortedItems =
                        _sortAscending

                            ? sortedItems
                                .OrderBy(item =>
                                    item.Item.Value *
                                    item.Quantity)
                                .ThenBy(item =>
                                    item.Item.Name)
                                .ToList()

                            : sortedItems
                                .OrderByDescending(item =>
                                    item.Item.Value *
                                    item.Quantity)
                                .ThenBy(item =>
                                    item.Item.Name)
                                .ToList();

                    break;
            }
        }


        // --------------------------------------------------------
        // Dynamically create enough rows for every item.
        //
        // 4 columns:
        //
        // Item 1 | Item 2 | Item 3 | Item 4
        // Item 5 | Item 6 | Item 7 | Item 8
        // etc...
        // --------------------------------------------------------

        // Retain native image handlers and gestures when only quantities or
        // sort positions change. Remove slots that leave the current category.
        HashSet<InventoryItem> visibleItems = sortedItems.ToHashSet();
        foreach (InventoryItem entry in _slots.Keys.ToArray())
        {
            if (visibleItems.Contains(entry))
                continue;
            InventoryGrid.Children.Remove(_slots[entry].Card);
            _slots.Remove(entry);
        }

        int rowCount = Math.Max(1, (sortedItems.Count + Columns - 1) / Columns);
        while (InventoryGrid.RowDefinitions.Count > rowCount)
            InventoryGrid.RowDefinitions.RemoveAt(InventoryGrid.RowDefinitions.Count - 1);
        while (InventoryGrid.RowDefinitions.Count < rowCount)
            InventoryGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        if (sortedItems.Count == 0)
        {
            _emptyCategoryLabel.Text = GetEmptyCategoryText();
            if (!InventoryGrid.Children.Contains(_emptyCategoryHost))
            {
                InventoryGrid.Add(_emptyCategoryHost, 0, 0);
                InventoryGrid.SetColumnSpan(_emptyCategoryHost, Columns);
            }
            return;
        }

        InventoryGrid.Children.Remove(_emptyCategoryHost);
        for (int index = 0; index < sortedItems.Count; index++)
        {
            InventoryItem entry = sortedItems[index];
            if (!_slots.TryGetValue(entry, out InventorySlot? slot))
            {
                slot = CreateItemSlot(entry);
                _slots.Add(entry, slot);
                InventoryGrid.Add(slot.Card, index % Columns, index / Columns);
            }
            else
            {
                InventoryGrid.SetColumn(slot.Card, index % Columns);
                InventoryGrid.SetRow(slot.Card, index / Columns);
            }

            string quantity = $"×{entry.Quantity}";
            if (slot.Quantity.Text != quantity)
                slot.Quantity.Text = quantity;
            Color background = GetSlotBackground(entry.Item);
            if (!Equals(slot.Card.BackgroundColor, background))
                slot.Card.BackgroundColor = background;
        }
    }
    private void UpdateSellJunkButton()
    {
        int junkItemCount = GetCategoryQuantity(InventoryCategory.Junk);

        SellJunkButton.Text = junkItemCount > 0
            ? $"Sell Junk ({junkItemCount:N0})"
            : "Sell Junk";

        SellJunkButton.IsEnabled = junkItemCount > 0;
        SellJunkButton.IsVisible =
            _selectedCategory == InventoryCategory.Junk;
        BulkSellControls.IsVisible = SellJunkButton.IsVisible;
    }

    private void UpdateSellCategoryButtons()
    {
        int equipmentItemCount = GetCategoryQuantity(InventoryCategory.Equipment);
        SellEquipmentButton.Text = equipmentItemCount > 0
            ? $"Sell Equipment ({equipmentItemCount:N0})"
            : "Sell Equipment";
        SellEquipmentButton.IsEnabled = equipmentItemCount > 0;
        SellEquipmentButton.IsVisible =
            _selectedCategory == InventoryCategory.Equipment;

        int foodItemCount = GetCategoryQuantity(InventoryCategory.Food);
        SellFoodButton.Text = foodItemCount > 0
            ? $"Sell Food ({foodItemCount:N0})"
            : "Sell Food";
        SellFoodButton.IsEnabled = foodItemCount > 0;
        SellFoodButton.IsVisible =
            _selectedCategory == InventoryCategory.Food;
    }

    private int GetCategoryQuantity(InventoryCategory category)
    {
        return _inventory.Items
            .Where(entry => entry.Quantity > 0 &&
                            IsInCategory(entry.Item, category))
            .Sum(entry => entry.Quantity);
    }

    private void UpdateCombineAllEquipmentButton()
    {
        int combinablePairs = _inventory.Items
            .Where(entry =>
                entry.Item.Type == ItemType.Equipment &&
                entry.Item.UpgradeLevel < 10)
            .Sum(entry => entry.Quantity / 2);

        CombineAllEquipmentButton.Text = combinablePairs > 0
            ? $"Combine All ({combinablePairs:N0})"
            : "Combine All";
        CombineAllEquipmentButton.IsEnabled = combinablePairs > 0;
        CombineAllEquipmentButton.IsVisible =
            _selectedCategory == InventoryCategory.Equipment;
    }

    private bool IsInSelectedCategory(Item item)
    {
        return IsInCategory(item, _selectedCategory);
    }

    private static bool IsInCategory(
        Item item,
        InventoryCategory category)
    {
        return category switch
        {
            InventoryCategory.Equipment => item.Type == ItemType.Equipment,
            InventoryCategory.Food => item.Type == ItemType.Food,
            InventoryCategory.Junk => item.IsJunk,
            _ => false
        };
    }

    private static string GetCategoryName(InventoryCategory category)
    {
        return category switch
        {
            InventoryCategory.Equipment => "Equipment",
            InventoryCategory.Food => "Food",
            InventoryCategory.Junk => "Junk",
            _ => "Items"
        };
    }

    private static string GetCategoryItemLabel(InventoryCategory category)
    {
        return category switch
        {
            InventoryCategory.Equipment => "equipment items",
            InventoryCategory.Food => "food items",
            InventoryCategory.Junk => "junk items",
            _ => "items"
        };
    }

    private string GetEmptyCategoryText()
    {
        return _selectedCategory switch
        {
            InventoryCategory.Equipment => "No equipment yet.",
            InventoryCategory.Food => "No food yet.",
            InventoryCategory.Junk => "No junk to sell.",
            _ => "Nothing here yet."
        };
    }

    private void OnSellJunkClicked(
        object? sender,
        EventArgs e)
    {
        SellJunk(BulkSellMode.All);
    }

    private async void OnSellEquipmentClicked(
        object? sender,
        EventArgs e)
    {
        await ConfirmSellCategoryAsync(InventoryCategory.Equipment);
    }

    private async void OnSellFoodClicked(
        object? sender,
        EventArgs e)
    {
        await ConfirmSellCategoryAsync(InventoryCategory.Food);
    }

    private async void OnCombineAllEquipmentClicked(
        object? sender,
        EventArgs e)
    {
        if (_inventory.TryCombineAllEquipment(out int combinedPairs))
        {
            UpdateInventory();
            await CustomDialogService.ShowAsync(
                "Equipment combined",
                $"Combined {combinedPairs:N0} equipment pairs.",
                "OK");
        }
    }

    private void OnSellOneJunkClicked(object? sender, EventArgs e) => SellJunk(BulkSellMode.One);
    private void OnSellTenJunkClicked(object? sender, EventArgs e) => SellJunk(BulkSellMode.Ten);
    private void OnSellHalfJunkClicked(object? sender, EventArgs e) => SellJunk(BulkSellMode.Half);

    private enum BulkSellMode { One, Ten, Half, All }

    private void SellJunk(BulkSellMode mode)
    {
        List<InventoryItem> junkItems = _inventory.Items
            .Where(entry => entry.Item.IsJunk)
            .ToList();

        long totalGold = 0;

        foreach (InventoryItem junkItem in junkItems)
        {
            int quantity = mode switch
            {
                BulkSellMode.One => Math.Min(1, junkItem.Quantity),
                BulkSellMode.Ten => Math.Min(10, junkItem.Quantity),
                BulkSellMode.Half => Math.Max(1, junkItem.Quantity / 2),
                _ => junkItem.Quantity
            };
            totalGold += (long)junkItem.Item.Value * quantity;
            _inventory.RemoveItem(
                junkItem.Item,
                quantity);
        }

        if (totalGold > 0)
        {
            _inventory.AddItem(
                ItemData.Coins,
                (int)Math.Min(totalGold, int.MaxValue));
        }
    }

    private async Task ConfirmSellCategoryAsync(InventoryCategory category)
    {
        List<InventoryItem> items = _inventory.Items
            .Where(entry => entry.Quantity > 0 &&
                            IsInCategory(entry.Item, category))
            .ToList();

        if (items.Count == 0)
            return;

        int itemCount = items.Sum(entry => entry.Quantity);
        long totalGold = items.Sum(entry =>
            (long)entry.Item.Value * entry.Quantity);
        string categoryName = GetCategoryName(category);

        string? action = await CustomDialogService.ShowAsync(
            $"Sell All {categoryName}?",
            $"Sell {itemCount:N0} {GetCategoryItemLabel(category)} " +
            $"for {totalGold:N0} coins? This cannot be undone.",
            "Cancel",
            "Sell All");

        if (action == "Sell All")
            SellItems(items);
    }

    private void SellItems(IEnumerable<InventoryItem> items)
    {
        long totalGold = 0;

        foreach (InventoryItem inventoryItem in items)
        {
            int quantity = inventoryItem.Quantity;
            totalGold += (long)inventoryItem.Item.Value * quantity;
            _inventory.RemoveItem(inventoryItem.Item, quantity);

            if (_player.EquippedFood == inventoryItem.Item)
                _player.ClearEquippedFood();
        }

        if (totalGold > 0)
        {
            _inventory.AddItem(
                ItemData.Coins,
                (int)Math.Min(totalGold, int.MaxValue));
        }

        UpdateInventory();
    }


    // ============================================================
    // CREATE ITEM SLOT
    // ============================================================

    private InventorySlot CreateItemSlot(
        InventoryItem inventoryItem)
    {
        Grid slotContent =
            new Grid();


        // --------------------------------------------------------
        // Item icon.
        // --------------------------------------------------------

        // --------------------------------------------------------
        // Quantity.
        // --------------------------------------------------------

        Label quantityLabel =
            new Label
            {
                Text =
                    $"×{inventoryItem.Quantity}",

                FontSize = 12,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor = Colors.White,

                HorizontalOptions =
                    LayoutOptions.End,

                VerticalOptions =
                    LayoutOptions.End
            };


        slotContent.Children.Add(
            RarityVisuals.CreateItemVisual(
                inventoryItem.Item,
                32));

        slotContent.Children.Add(
            quantityLabel);


        // --------------------------------------------------------
        // Item slot.
        // --------------------------------------------------------

        Border itemSlot =
            new Border
            {
                Content = slotContent,
                WidthRequest = 60,
                HeightRequest = 60,
                Padding = 0,
                BackgroundColor = GetSlotBackground(inventoryItem.Item),
                Stroke = GetItemStrokeColor(inventoryItem.Item),
                StrokeThickness = 2,
                VerticalOptions = LayoutOptions.Start
            };


        // --------------------------------------------------------
        // Clicking an item.
        // --------------------------------------------------------

        TapGestureRecognizer tapGesture =
            new TapGestureRecognizer();


        tapGesture.Tapped +=
            (sender, e) =>
            {
                _ = VisualEffects.PulseAsync(itemSlot, 1.06, 70);
                DisplayItemInfo(
                    inventoryItem);
            };


        itemSlot.GestureRecognizers.Add(
            tapGesture);


        return new InventorySlot(itemSlot, quantityLabel);
    }

    private Color GetSlotBackground(Item item) =>
        item.Type == ItemType.Equipment && !_player.MeetsEquipmentRequirement(item)
            ? Color.FromArgb("#5A1E1E")
            : item == _player.EquippedFood
                ? Color.FromArgb("#2D7D46")
                : Color.FromArgb("#4A4A4A");

    private static Color GetItemStrokeColor(Item item)
    {
        DropRarity rarity = RarityVisuals.GetRarity(item);
        return rarity == DropRarity.Common
            ? Color.FromArgb("#D99032")
            : rarity switch
            {
                DropRarity.Uncommon => Color.FromArgb("#62C7FF"),
                DropRarity.Rare => Color.FromArgb("#C882FF"),
                DropRarity.VeryRare => Color.FromArgb("#FF87C2"),
                DropRarity.SuperRare => Color.FromArgb("#FFD24A"),
                DropRarity.MegaRare => Color.FromArgb("#FF6868"),
                _ => Color.FromArgb("#D99032")
        };
    }

    // ============================================================
    // ITEM INFORMATION
    // ============================================================

    private async void DisplayItemInfo(
        InventoryItem inventoryItem)
    {
        Item item = inventoryItem.Item;
        string primaryAction = item.Type switch
        {
            ItemType.Equipment when item.EquipmentSlot != EquipmentSlot.None
                => "Equip",
            ItemType.Food when item.HealingAmount > 0
                => "Equip Food",
            _ => ""
        };

        bool canCombine = item.Type == ItemType.Equipment &&
                          item.UpgradeLevel < 10 &&
                          inventoryItem.Quantity >= 2;

        bool canCombineAll = item.Type == ItemType.Equipment &&
                             item.UpgradeLevel < 10 &&
                             inventoryItem.Quantity >= 4;

        bool canSellAll = item.Type == ItemType.Food ||
                          item.Type == ItemType.Equipment ||
                          item.IsJunk;

        List<string> dialogButtons = new();
        if (!string.IsNullOrWhiteSpace(primaryAction))
            dialogButtons.Add(primaryAction);
        if (canCombine)
            dialogButtons.Add("Combine");
        if (canCombineAll)
            dialogButtons.Add("Combine All");
        if (canSellAll)
            dialogButtons.Add("Sell All");
        dialogButtons.Add("Drop");
        dialogButtons.Add("Close");

        string? action = await CustomDialogService.ShowItemAsync(
            item.Name,
            item.IconImage,
            BuildItemDescription(inventoryItem),
            GameThemeCache.GetItemRarityColor(item),
            dialogButtons.ToArray());

        if (action == "Drop")
        {
            DropOneItem(item);
            return;
        }

        if (action == "Combine")
        {
            if (_inventory.TryCombineEquipment(item, out Item upgradedItem))
            {
                UpdateInventory();
                await CustomDialogService.ShowAsync(
                    "Equipment combined",
                    $"Your {item.Name} became {upgradedItem.Name}.",
                    "OK");
            }
            else
            {
                UpdateInventory();
            }
            return;
        }

        if (action == "Combine All")
        {
            if (_inventory.TryCombineAllEquipment(item, out EquipmentCombineAllResult result))
            {
                UpdateInventory();
                string createdItems = string.Join(
                    ", ",
                    result.CreatedStacks.Select(stack =>
                        $"{stack.Item.Name} ×{stack.Quantity}"));

                await CustomDialogService.ShowAsync(
                    "Equipment combined",
                    $"Combined {result.ConsumedCopies} {item.Name} copies into " +
                    createdItems + ".",
                    "OK");
            }
            else
            {
                UpdateInventory();
                await CustomDialogService.ShowAsync(
                    "Unable to combine all",
                    "Free an inventory slot, then try again.",
                    "OK");
            }

            return;
        }

        if (action == "Sell All")
        {
            SellAllItem(item, inventoryItem.Quantity);
            return;
        }

        if (action == "Equip")
        {
            if (!_player.MeetsEquipmentRequirement(item))
            {
                await CustomDialogService.ShowAsync(
                    "Equipment requirement",
                    $"{item.Name} requires " +
                    $"{_player.GetEquipmentRequirementText(item)} " +
                    "before you can equip it.",
                    "OK");

                return;
            }

            _player.EquipItem(item);
            return;
        }

        if (action == "Equip Food")
        {
            _player.SelectFood(item);
            UpdateInventory();
        }
    }

    private string BuildItemDescription(InventoryItem inventoryItem)
    {
        Item item = inventoryItem.Item;
        string description = $"Quantity: {inventoryItem.Quantity}\n";

        description += item.Type switch
        {
            ItemType.Equipment =>
                $"{FormatEquipmentSlot(item.EquipmentSlot)} equipment\n" +
                FormatEquipmentBonuses(item),
            ItemType.Food =>
                $"Restores {item.HealingAmount} HP when auto-eaten in combat.",
            ItemType.Pet => "A loyal companion collected from your adventures.",
            ItemType.Currency => "Currency used to buy inventory slots.",
            _ => "Tradeable crafting material."
        };

        if (item.Type == ItemType.Equipment && item.UpgradeLevel > 0)
            description += $"\nUpgrade level: +{item.UpgradeLevel}";

        return description +
               $"\n\n{item.Description}" +
               $"\n\nValue: {item.Value:N0} coins each";
    }

    private static string FormatEquipmentSlot(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Head => "Head",
            EquipmentSlot.Body => "Body",
            EquipmentSlot.Legs => "Legs",
            EquipmentSlot.Weapon => "Weapon",
            EquipmentSlot.Shield => "Off-hand",
            EquipmentSlot.Gloves => "Gloves",
            EquipmentSlot.Boots => "Boots",
            EquipmentSlot.Amulet => "Amulet",
            EquipmentSlot.Ring => "Ring",
            _ => "Equipment"
        };
    }

    private static string FormatEquipmentBonuses(Item item)
    {
        List<string> bonuses = new();

        if (item.AttackBonus != 0)
            bonuses.Add($"Attack +{item.AttackBonus}");
        if (item.StrengthBonus != 0)
            bonuses.Add($"Strength +{item.StrengthBonus}");
        if (item.DefenseBonus != 0)
            bonuses.Add($"Defense +{item.DefenseBonus}");
        if (item.HPBonus != 0)
            bonuses.Add($"HP +{item.HPBonus}");

        if (item.EquipmentSlot == EquipmentSlot.Weapon)
            bonuses.Add($"{item.AttackSpeedTicks} attack ticks");

        if (item.RequiredAttackLevel > 1 || item.RequiredDefenseLevel > 1)
        {
            string requirement = item.EquipmentSlot == EquipmentSlot.Weapon
                ? $"Requires Attack {item.RequiredAttackLevel}"
                : $"Requires Defense {item.RequiredDefenseLevel}";
            bonuses.Add(requirement);
        }

        return bonuses.Count == 0
            ? "No combat bonuses."
            : string.Join("\n", bonuses);
    }

    private void DropOneItem(Item item)
    {
        if (!_inventory.RemoveItem(item))
            return;

        if (_player.EquippedFood == item &&
            !_inventory.HasItem(item))
        {
            _player.ClearEquippedFood();
        }

        UpdateInventory();
    }

    private void SellAllItem(Item item, int quantity)
    {
        if (quantity <= 0 || !_inventory.RemoveItem(item, quantity))
            return;

        if (_player.EquippedFood == item)
            _player.ClearEquippedFood();

        long totalGold = (long)item.Value * quantity;
        if (totalGold > 0)
        {
            _inventory.AddItem(
                ItemData.Coins,
                (int)Math.Min(totalGold, int.MaxValue));
        }

        UpdateInventory();
    }
}
