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


        // --------------------------------------------------------
        // Listen for inventory changes.
        // --------------------------------------------------------

        _inventory.InventoryChanged +=
            OnInventoryChanged;


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
        MainThread.BeginInvokeOnMainThread(() =>
        {
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

        SetSortButtonAppearance(
            SortNameButton,
            _sortMode == InventorySortMode.Name);

        SetSortButtonAppearance(
            SortQuantityButton,
            _sortMode == InventorySortMode.Quantity);

        SetSortButtonAppearance(
            SortValueButton,
            _sortMode == InventorySortMode.TotalValue);

        SortDirectionButton.Variant = GoldSliceButtonVariant.Neutral;
    }

    public void RefreshDisplay()
    {
        UpdateInventory();
    }

    public void Dispose()
    {
        _inventory.InventoryChanged -= OnInventoryChanged;
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
        UpdateCategoryTabs();
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
            UpdateInventory();
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
        UpdateSellJunkButton();

        InventoryGrid.Children.Clear();
        InventoryGrid.RowDefinitions.Clear();

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


        // --------------------------------------------------------
        // Dynamically create enough rows for every item.
        //
        // 4 columns:
        //
        // Item 1 | Item 2 | Item 3 | Item 4
        // Item 5 | Item 6 | Item 7 | Item 8
        // etc...
        // --------------------------------------------------------

        if (sortedItems.Count == 0)
        {
            Label emptyCategoryLabel = new Label
            {
                Text = GetEmptyCategoryText(),
                TextColor = Color.FromArgb("#C8C8C8"),
                FontSize = 15,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 20)
            };

            InventoryGrid.Add(emptyCategoryLabel, 0, 0);
            InventoryGrid.SetColumnSpan(emptyCategoryLabel, Columns);
            return;
        }

        int rowCount =
            (int)Math.Ceiling(
                sortedItems.Count /
                (double)Columns);


        // --------------------------------------------------------
        // Create the required rows.
        // --------------------------------------------------------

        for (int row = 0;
             row < rowCount;
             row++)
        {
            InventoryGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        GridLength.Auto
                });
        }


        // --------------------------------------------------------
        // Add every item.
        // --------------------------------------------------------

        for (int index = 0;
             index < sortedItems.Count;
             index++)
        {
            int row =
                index / Columns;

            int column =
                index % Columns;


            Border itemSlot =
                CreateItemSlot(
                    sortedItems[index]);


            InventoryGrid.Add(
                itemSlot,
                column,
                row);

        }
    }

    private void UpdateSellJunkButton()
    {
        int junkItemCount = _inventory.Items
            .Where(entry => entry.Item.IsJunk)
            .Sum(entry => entry.Quantity);

        SellJunkButton.Text = junkItemCount > 0
            ? $"Sell Junk ({junkItemCount:N0})"
            : "Sell Junk";

        SellJunkButton.IsEnabled = junkItemCount > 0;
        SellJunkButton.IsVisible =
            _selectedCategory == InventoryCategory.Junk;
        BulkSellControls.IsVisible = SellJunkButton.IsVisible;
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
        return _selectedCategory switch
        {
            InventoryCategory.Equipment => item.Type == ItemType.Equipment,
            InventoryCategory.Food => item.Type == ItemType.Food,
            InventoryCategory.Junk => item.IsJunk,
            _ => false
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


    // ============================================================
    // CREATE ITEM SLOT
    // ============================================================

    private Border CreateItemSlot(
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

        bool isLockedEquipment =
            inventoryItem.Item.Type == ItemType.Equipment &&
            !_player.MeetsEquipmentRequirement(inventoryItem.Item);

        bool isEquippedFood =
            inventoryItem.Item == _player.EquippedFood;

        Border itemSlot =
            new Border
            {
                Content = slotContent,
                WidthRequest = 60,
                HeightRequest = 60,
                Padding = 0,
                BackgroundColor = isLockedEquipment
                    ? Color.FromArgb("#5A1E1E")
                    : isEquippedFood
                        ? Color.FromArgb("#2D7D46")
                        : Color.FromArgb("#4A4A4A"),
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
                DisplayItemInfo(
                    inventoryItem);
            };


        itemSlot.GestureRecognizers.Add(
            tapGesture);


        return itemSlot;
    }

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
