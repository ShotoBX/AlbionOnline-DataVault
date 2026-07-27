using Serilog;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Diagnostics;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Exceptions;
using StatisticsAnalysisTool.GameFileData;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Models.ItemsJsonModel;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace StatisticsAnalysisTool.Crafting;

/// <summary>
/// Single-item refining calculator (Wood -> Planks, Ore -> Metal Bar, Fiber -> Cloth, Hide -> Leather, Rock -> Stone
/// Block). Reuses CraftingCalculator's profit math and CraftingLocationData's return-rate model as-is; the only new
/// piece is RefiningRecipeResolver, which reads the refining recipes Albion already encodes on SimpleItem. Refining
/// has no Focus or Journal mechanics in-game, so those are intentionally absent here (unlike CraftingBindings).
/// </summary>
public class RefiningBindings : BaseViewModel
{
    private readonly CraftingCalculator _calculator = new();
    private readonly RefiningRecipeResolver _recipeResolver = new();
    private readonly CraftingStationFeeService _stationFeeService = new();
    private string _itemSearchText = string.Empty;
    private string _refiningLocationSearchText = string.Empty;
    private string _sellPriceOptionsItemUniqueName = string.Empty;
    private CategoryDropdownItem _selectedResourceType;
    private bool _isUpdatingPercentInputText;
    private string _returnRatePercentText = FormatPercentInput(0m);
    private string _salesTaxPercentText = FormatPercentInput(4m);
    private string _setupFeePercentText = FormatPercentInput(2.5m);
    private int _amountCrafted = 1;

    public RefiningBindings()
    {
        var refinableItems = ItemController.Items
            .Where(_recipeResolver.IsRefinable)
            .OrderBy(x => x.LocalizedName)
            .ToList();
        RefinableItems = new ObservableCollection<Item>(refinableItems);
        LoadResourceTypesToDropdown(refinableItems);
        RefinableItemsView = CollectionViewSource.GetDefaultView(RefinableItems);
        RefinableItemsView.Filter = FilterRefinableItem;
        SelectedDailyBonus = DailyBonusOptions.First();
        SelectedHideoutBonus = HideoutBonusOptions.First();
        RefreshRefiningLocations(null);

        foreach (var location in ResourceMarketLocations)
        {
            SourcingCityFilters.Add(new SourcingCityFilterOption
            {
                Location = location.Key,
                DisplayName = location.Value
            });
        }
    }

    public ObservableCollection<Item> RefinableItems { get; }

    public ICollectionView RefinableItemsView { get; }

    public ObservableCollection<CraftingResourceEntry> Resources { get; } = [];

    public ObservableCollection<CraftingItemSearchResult> ListBoxItemSearchItems { get; } = [];

    public ObservableCollection<CraftingLocationOption> RefiningLocations { get; } = [];

    public ObservableCollection<CraftingLocationOption> ListBoxRefiningLocationItems { get; } = [];

    public ObservableCollection<CraftingSellPriceOption> SellPriceOptions { get; } = [];

    public ObservableCollection<CategoryDropdownItem> ResourceTypeOptions { get; } = [];

    public RefiningOptimizerBindings Optimizer { get; } = new();

    public CraftingDailyBonusOption[] DailyBonusOptions { get; } =
    [
        new() { Name = CraftingBindings.TranslationNone, BonusPercent = 0m },
        new() { Name = "10%", BonusPercent = 10m },
        new() { Name = "20%", BonusPercent = 20m }
    ];

    public CraftingHideoutBonusOption[] HideoutBonusOptions { get; } = HideoutData.GetHideoutBonusOptions();

    public KeyValuePair<MarketLocation, string>[] MarketLocations { get; } = [.. Locations.OnceMarketLocations];

    public MarketLocation SelectedBuyMarketLocation
    {
        get;
        set
        {
            field = value;
            if (UseGlobalCity)
            {
                SyncResourceSourceLocationsToGlobal();
            }
            OnPropertyChanged();
        }
    }
    = MarketLocation.CaerleonMarket;

    public MarketLocation SelectedSellMarketLocation
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = MarketLocation.CaerleonMarket;

    public Dictionary<ItemTier, string> ItemTiers => FrequentlyValues.ItemTiers;

    public Dictionary<ItemLevel, string> ItemLevels => FrequentlyValues.ItemLevels;

    public Item SelectedItem
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedItemHeaderVisibility));
            OnPropertyChanged(nameof(SelectedItemEmptyVisibility));
            ClearSellPriceOptions();
            ApplySelectedItem(value);
        }
    }

    public Visibility SelectedItemHeaderVisibility => SelectedItem == null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility SelectedItemEmptyVisibility => SelectedItem == null ? Visibility.Visible : Visibility.Collapsed;

    public string ItemSearchText
    {
        get => _itemSearchText;
        set
        {
            _itemSearchText = value;

            if (SelectedItem != null && !string.Equals(SelectedItem.LocalizedName, value, StringComparison.Ordinal))
            {
                SelectedItem = null;
            }

            RefinableItemsView?.Refresh();
            UpdateItemSearchListBox(value, true);
            OnPropertyChanged();
        }
    }

    public bool IsItemSearchPopupOpen
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public string RefiningLocationSearchText
    {
        get => _refiningLocationSearchText;
        set
        {
            _refiningLocationSearchText = value;
            UpdateRefiningLocationListBox(value);
            OnPropertyChanged();
        }
    }

    public bool IsRefiningLocationPopupOpen
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsSellPricePopupOpen
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public CategoryDropdownItem SelectedResourceType
    {
        get => _selectedResourceType;
        set
        {
            _selectedResourceType = value;
            RefreshItemSearchFilters();
            OnPropertyChanged();
        }
    }

    public ItemTier SelectedItemTier
    {
        get;
        set
        {
            field = value;
            RefreshItemSearchFilters();
            OnPropertyChanged();
        }
    }
    = ItemTier.Unknown;

    public ItemLevel SelectedItemLevel
    {
        get;
        set
        {
            field = value;
            RefreshItemSearchFilters();
            OnPropertyChanged();
        }
    }
    = ItemLevel.Unknown;

    public int RefiningRuns
    {
        get;
        set
        {
            field = Math.Max(1, value);
            Recalculate();
            OnPropertyChanged();
        }
    }
    = 1;

    public decimal ReturnRatePercent
    {
        get;
        set
        {
            field = Math.Clamp(value, 0m, 100m);
            UpdatePercentInputText(nameof(ReturnRatePercentText), field);
            Recalculate();
            OnPropertyChanged();
        }
    }

    public string ReturnRatePercentText
    {
        get => _returnRatePercentText;
        set
        {
            if (_returnRatePercentText == value)
            {
                return;
            }

            _returnRatePercentText = value;
            UpdatePercentValueFromInput(value, percent => ReturnRatePercent = percent);
            OnPropertyChanged();
        }
    }

    public CraftingLocationOption SelectedRefiningLocation
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRefiningLocationBonusSummary));
        }
    }

    public CraftingDailyBonusOption SelectedDailyBonus
    {
        get;
        set
        {
            field = value ?? DailyBonusOptions.First();
            ApplySelectedRefiningLocationReturnRate();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRefiningLocationBonusSummary));
        }
    }

    public CraftingHideoutBonusOption SelectedHideoutBonus
    {
        get;
        set
        {
            field = value ?? HideoutBonusOptions.First();
            ApplySelectedRefiningLocationReturnRate();
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRefiningLocationBonusSummary));
        }
    }

    public string SelectedRefiningLocationBonusSummary =>
        SelectedRefiningLocation == null ? string.Empty : TranslationBonus + " " + EffectiveRefiningBonusPercent.ToString("N2") + "%"
                         + "\n" + TranslationExpectedRrr + " " + GetSelectedRefiningLocationReturnRate().ToString("N2") + "%";

    public decimal EffectiveRefiningBonusPercent => (SelectedRefiningLocation?.TotalProductionBonusPercent ?? 0m) + (SelectedDailyBonus?.BonusPercent ?? 0m) + GetSelectedHideoutBonusPercent();

    public decimal StationFee
    {
        get;
        set
        {
            field = Math.Max(0m, value);
            Recalculate();
            OnPropertyChanged();
        }
    }

    public decimal SalesTaxPercent
    {
        get;
        set
        {
            field = Math.Clamp(value, 0m, 100m);
            UpdatePercentInputText(nameof(SalesTaxPercentText), field);
            Recalculate();
            OnPropertyChanged();
        }
    }
    = 4m;

    public string SalesTaxPercentText
    {
        get => _salesTaxPercentText;
        set
        {
            if (_salesTaxPercentText == value)
            {
                return;
            }

            _salesTaxPercentText = value;
            UpdatePercentValueFromInput(value, percent => SalesTaxPercent = percent);
            OnPropertyChanged();
        }
    }

    public decimal SetupFeePercent
    {
        get;
        set
        {
            field = Math.Clamp(value, 0m, 100m);
            UpdatePercentInputText(nameof(SetupFeePercentText), field);
            Recalculate();
            OnPropertyChanged();
        }
    }
    = 2.5m;

    public string SetupFeePercentText
    {
        get => _setupFeePercentText;
        set
        {
            if (_setupFeePercentText == value)
            {
                return;
            }

            _setupFeePercentText = value;
            UpdatePercentValueFromInput(value, percent => SetupFeePercent = percent);
            OnPropertyChanged();
        }
    }

    public decimal OtherCosts
    {
        get;
        set
        {
            field = Math.Max(0m, value);
            Recalculate();
            OnPropertyChanged();
        }
    }

    public decimal OutputUnitPrice
    {
        get;
        set
        {
            field = Math.Max(0m, value);
            Recalculate();
            OnPropertyChanged();
        }
    }

    public CraftingCalculationResult Calculation
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = new();

    public string StatusText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = string.Empty;

    public ICommand NewCommand => field ??= new CommandHandler(_ => ResetItemFilters(), true);

    public ICommand LoadPricesCommand => field ??= new CommandHandler(_ => PerformLoadPrices(), true);

    #region Multi-city resource sourcing

    private bool _isSyncingSourceLocations;

    /// <summary>
    /// Buy-city choices for per-resource sourcing: the sellable market locations minus the Black
    /// Market (players can only sell there, never buy resources from it). Same list CraftingBindings
    /// exposes, reused here rather than duplicated.
    /// </summary>
    public KeyValuePair<MarketLocation, string>[] ResourceMarketLocations { get; } =
        CraftingBindings.SellPriceMarketLocations
            .Where(x => x != MarketLocation.BlackMarket)
            .Select(x => new KeyValuePair<MarketLocation, string>(x, Locations.GetDisplayName(x)))
            .ToArray();

    public ObservableCollection<SourcingCityFilterOption> SourcingCityFilters { get; } = [];

    public bool UseGlobalCity
    {
        get;
        set
        {
            field = value;
            if (value)
            {
                SyncResourceSourceLocationsToGlobal();
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPerCitySourcingEnabled));
            OnPropertyChanged(nameof(PerCitySourcingVisibility));
        }
    }
    = true;

    public bool IsPerCitySourcingEnabled => !UseGlobalCity;

    public Visibility PerCitySourcingVisibility => UseGlobalCity ? Visibility.Collapsed : Visibility.Visible;

    public bool IsOptimizingPurchase
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotOptimizingPurchase));
        }
    }

    public bool IsNotOptimizingPurchase => !IsOptimizingPurchase;

    public string OptimizationSummaryText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = string.Empty;

    public ICommand OptimizePurchaseCommand => field ??= new CommandHandler(_ => OptimizePurchaseAsync(), true);

    private void SyncResourceSourceLocationsToGlobal()
    {
        _isSyncingSourceLocations = true;
        try
        {
            foreach (var resource in Resources)
            {
                resource.SourceLocation = SelectedBuyMarketLocation;
            }
        }
        finally
        {
            _isSyncingSourceLocations = false;
        }
    }

    private async void OnResourceSourceLocationChanged(CraftingResourceEntry resource)
    {
        if (resource == null || _isSyncingSourceLocations || IsOptimizingPurchase || UseGlobalCity)
        {
            return;
        }

        try
        {
            resource.UnitPrice = await CraftingBindings.LoadPriceAsync(resource.UniqueName, resource.SourceLocation, false);
            StatusText = LocalizationController.Translation("CRAFTING_MARKET_PRICES_LOADED");
        }
        catch (Exception e)
        {
            Log.Error(e, "Resource price for city could not be loaded");
            StatusText = LocalizationController.Translation("CRAFTING_MARKET_PRICES_COULD_NOT_BE_LOADED");
        }
    }

    /// <summary>
    /// Queries every allowed city's price for each resource, picks the cheapest per resource, and
    /// reports the estimated savings vs buying everything in the current global buy city. Mirrors
    /// CraftingBindings.OptimizePurchaseAsync exactly.
    /// </summary>
    private async void OptimizePurchaseAsync()
    {
        if (IsOptimizingPurchase)
        {
            return;
        }

        if (SelectedItem == null || Resources.Count == 0)
        {
            StatusText = LocalizationController.Translation("CRAFTING_SELECT_ITEM_BEFORE_LOADING_PRICES");
            return;
        }

        var allowedLocations = SourcingCityFilters
            .Where(x => !x.IsExcluded)
            .Select(x => x.Location)
            .ToHashSet();

        if (allowedLocations.Count == 0)
        {
            OptimizationSummaryText = LocalizationController.Translation("CRAFTING_OPTIMIZE_NO_DATA");
            return;
        }

        IsOptimizingPurchase = true;
        OptimizationSummaryText = string.Empty;
        StatusText = LocalizationController.Translation("CRAFTING_OPTIMIZING");

        try
        {
            UseGlobalCity = false;

            var globalLocation = SelectedBuyMarketLocation;
            var baselineTotal = 0m;
            var optimizedTotal = 0m;
            var comparableResources = 0;

            foreach (var resource in Resources)
            {
                var prices = await ApiController.GetCityItemPricesFromJsonAsync(resource.UniqueName).ConfigureAwait(true) ?? [];

                var best = ResourceMarketLocations
                    .Where(x => allowedLocations.Contains(x.Key))
                    .Select(x => new { Location = x.Key, Value = CraftingBindings.GetSellPriceOptionValue(prices, x.Key) })
                    .Where(x => x.Value.Price > 0m)
                    .OrderBy(x => x.Value.Price)
                    .FirstOrDefault();

                if (best == null)
                {
                    continue;
                }

                resource.SourceLocation = best.Location;
                resource.UnitPrice = best.Value.Price;

                var baselineValue = CraftingBindings.GetSellPriceOptionValue(prices, globalLocation);
                if (baselineValue.Price > 0m)
                {
                    baselineTotal += baselineValue.Price * resource.GrossQuantity;
                    optimizedTotal += best.Value.Price * resource.GrossQuantity;
                    comparableResources++;
                }

                await Task.Delay(150).ConfigureAwait(true);
            }

            OptimizationSummaryText = comparableResources > 0
                ? string.Format(LocalizationController.Translation("CRAFTING_OPTIMIZE_SAVINGS"),
                    (baselineTotal - optimizedTotal).ToString("N0", CultureInfo.CurrentCulture),
                    Locations.GetDisplayName(globalLocation))
                : LocalizationController.Translation("CRAFTING_OPTIMIZE_NO_DATA");
            StatusText = LocalizationController.Translation("CRAFTING_MARKET_PRICES_LOADED");
        }
        catch (TooManyRequestsException)
        {
            StatusText = LocalizationController.Translation("CRAFTING_OPTIMIZER_RATE_LIMITED");
        }
        catch (Exception e)
        {
            Log.Error(e, "Refining purchase optimization failed");
            StatusText = LocalizationController.Translation("CRAFTING_MARKET_PRICES_COULD_NOT_BE_LOADED");
        }
        finally
        {
            IsOptimizingPurchase = false;
        }
    }

    #endregion

    public void SelectItemSearchResult(CraftingItemSearchResult searchResult)
    {
        if (searchResult?.Value == null)
        {
            return;
        }

        _itemSearchText = searchResult.Name;
        OnPropertyChanged(nameof(ItemSearchText));
        SelectedItem = searchResult.Value;
        IsItemSearchPopupOpen = false;
    }

    public void OpenItemSearch()
    {
        UpdateItemSearchListBox(ItemSearchText, true);
    }

    public void CloseItemSearch()
    {
        IsItemSearchPopupOpen = false;
    }

    public void OpenRefiningLocationSearch()
    {
        UpdateRefiningLocationListBox(RefiningLocationSearchText);
    }

    public void SelectRefiningLocation(CraftingLocationOption location)
    {
        if (location == null)
        {
            return;
        }

        SelectedRefiningLocation = location;
        _refiningLocationSearchText = location.DisplayName;
        OnPropertyChanged(nameof(RefiningLocationSearchText));
        IsRefiningLocationPopupOpen = false;
        ApplySelectedRefiningLocationReturnRate();
    }

    public async void OpenSellPriceOptions()
    {
        try
        {
            if (SelectedItem == null)
            {
                SellPriceOptions.Clear();
                IsSellPricePopupOpen = false;
                return;
            }

            if (string.Equals(_sellPriceOptionsItemUniqueName, SelectedItem.UniqueName, StringComparison.Ordinal)
                && SellPriceOptions.Count > 0)
            {
                IsSellPricePopupOpen = true;
                return;
            }

            await LoadSellPriceOptionsAsync();
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "Refining sell price options could not be loaded");
            IsSellPricePopupOpen = false;
        }
    }

    public void SelectSellPriceOption(CraftingSellPriceOption sellPriceOption)
    {
        if (sellPriceOption == null)
        {
            return;
        }

        OutputUnitPrice = sellPriceOption.Price;
        IsSellPricePopupOpen = false;
    }

    public void CloseSellPriceOptions()
    {
        IsSellPricePopupOpen = false;
    }

    public async void OpenResourcePriceOptions(CraftingResourceEntry resource)
    {
        try
        {
            if (resource == null || string.IsNullOrWhiteSpace(resource.UniqueName))
            {
                return;
            }

            await CraftingBindings.LoadPriceOptionsAsync(resource.UniqueName, resource.PriceOptions, CraftingPricePreference.LowerIsBetter);
            CloseAllPriceOptionPopups();
            resource.IsPricePopupOpen = resource.PriceOptions.Count > 0;
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "Refining resource price options could not be loaded");
            resource.IsPricePopupOpen = false;
        }
    }

    public void SelectResourcePriceOption(CraftingResourceEntry resource, CraftingSellPriceOption priceOption)
    {
        if (resource == null || priceOption == null)
        {
            return;
        }

        resource.UnitPrice = priceOption.Price;
        resource.IsPricePopupOpen = false;
    }

    public void CloseAllPriceOptionPopups()
    {
        IsSellPricePopupOpen = false;

        foreach (var resource in Resources)
        {
            resource.IsPricePopupOpen = false;
        }
    }

    public void ResetItemFilters()
    {
        ItemSearchText = string.Empty;
        SelectedResourceType = null;
        SelectedItemTier = ItemTier.Unknown;
        SelectedItemLevel = ItemLevel.Unknown;
    }

    public async void PerformLoadPrices()
    {
        try
        {
            if (SelectedItem == null)
            {
                StatusText = LocalizationController.Translation("CRAFTING_SELECT_ITEM_BEFORE_LOADING_PRICES");
                return;
            }

            OutputUnitPrice = await CraftingBindings.LoadPriceAsync(SelectedItem.UniqueName, SelectedSellMarketLocation, false);

            foreach (var resource in Resources)
            {
                resource.UnitPrice = await CraftingBindings.LoadPriceAsync(resource.UniqueName, UseGlobalCity ? SelectedBuyMarketLocation : resource.SourceLocation, false);
            }

            StatusText = LocalizationController.Translation("CRAFTING_MARKET_PRICES_LOADED");
        }
        catch (Exception e)
        {
            Log.Error(e, "Error loading refining market prices");
            StatusText = LocalizationController.Translation("CRAFTING_MARKET_PRICES_COULD_NOT_BE_LOADED");
        }
    }

    private bool FilterRefinableItem(object value)
    {
        if (value is not Item item)
        {
            return false;
        }

        return ItemMatchesFilter(item)
               && (string.IsNullOrWhiteSpace(ItemSearchText) || ItemMatchesSearchText(item, ItemSearchText));
    }

    private bool ItemMatchesFilter(Item item)
    {
        var resourceTypeMatch = SelectedResourceType == null
                                 || string.IsNullOrWhiteSpace(SelectedResourceType.Id)
                                 || string.Equals((item.FullItemInformation as SimpleItem)?.ResourceType, SelectedResourceType.Id, StringComparison.OrdinalIgnoreCase);
        var tierMatch = SelectedItemTier == ItemTier.Unknown || (ItemTier) item.Tier == SelectedItemTier;
        var levelMatch = SelectedItemLevel == ItemLevel.Unknown || (ItemLevel) item.Level == SelectedItemLevel;

        return resourceTypeMatch && tierMatch && levelMatch;
    }

    private static bool ItemMatchesSearchText(Item item, string searchText)
    {
        return (item.LocalizedNameAndEnglish ?? item.UniqueName ?? string.Empty)
            .Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private static bool RefiningLocationMatchesSearchText(CraftingLocationOption location, string searchText)
    {
        if (location == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return (location.DisplayName ?? string.Empty).Contains(searchText, StringComparison.OrdinalIgnoreCase)
               || (location.ClusterId ?? string.Empty).Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshItemSearchFilters()
    {
        RefinableItemsView?.Refresh();
        UpdateItemSearchListBox(ItemSearchText, false);
    }

    private void UpdateItemSearchListBox(string searchText, bool shouldOpenPopup)
    {
        ListBoxItemSearchItems.Clear();

        var filteredItems = RefinableItems
            .Where(x => ItemMatchesFilter(x)
                        && (string.IsNullOrWhiteSpace(searchText) || ItemMatchesSearchText(x, searchText)))
            .Take(50);

        foreach (var item in filteredItems)
        {
            ListBoxItemSearchItems.Add(new CraftingItemSearchResult
            {
                Name = item.LocalizedName,
                Icon = item.Icon,
                Value = item
            });
        }

        IsItemSearchPopupOpen = shouldOpenPopup && ListBoxItemSearchItems.Count > 0;
    }

    private void UpdateRefiningLocationListBox(string searchText)
    {
        ListBoxRefiningLocationItems.Clear();

        var locations = RefiningLocations
            .Where(x => RefiningLocationMatchesSearchText(x, searchText))
            .Take(20)
            .ToList();

        foreach (var location in locations)
        {
            ListBoxRefiningLocationItems.Add(location);
        }

        IsRefiningLocationPopupOpen = ListBoxRefiningLocationItems.Count > 0;
    }

    private void LoadResourceTypesToDropdown(IEnumerable<Item> refinableItems)
    {
        var resourceTypes = refinableItems
            .Select(x => (x.FullItemInformation as SimpleItem)?.ResourceType)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

        ResourceTypeOptions.Clear();
        ResourceTypeOptions.Add(new CategoryDropdownItem
        {
            Id = string.Empty,
            Value = string.Empty,
            DisplayName = LocalizationController.Translation("ALL")
        });

        foreach (var resourceType in resourceTypes)
        {
            ResourceTypeOptions.Add(new CategoryDropdownItem
            {
                Id = resourceType,
                Value = resourceType,
                DisplayName = LocalizationController.Translation("@MARKETPLACEGUI_ROLLOUT_SHOPCATEGORY_" + resourceType.ToUpperInvariant())
            });
        }
    }

    private void ApplySelectedItem(Item item)
    {
        if (item == null)
        {
            Resources.Clear();
            Calculation = new CraftingCalculationResult();
            return;
        }

        _amountCrafted = _recipeResolver.GetAmountCrafted(item);
        RefreshRefiningLocations(item, SelectedRefiningLocation?.ClusterId);
        Resources.Clear();

        foreach (var resource in _recipeResolver.GetResources(item))
        {
            AddResource(resource);
        }

        Recalculate();
    }

    private void RefreshRefiningLocations(Item item, string selectedClusterId = null)
    {
        var locations = CraftingLocationData.GetCraftingLocations(item, useRefiningBonus: true);
        var selectedId = selectedClusterId ?? SelectedRefiningLocation?.ClusterId;

        RefiningLocations.Clear();

        foreach (var location in locations)
        {
            RefiningLocations.Add(location);
        }

        SelectedRefiningLocation = RefiningLocations.FirstOrDefault(x => string.Equals(x.ClusterId, selectedId, StringComparison.OrdinalIgnoreCase))
                                   ?? RefiningLocations.FirstOrDefault(x => string.Equals(x.ClusterId, "3003", StringComparison.OrdinalIgnoreCase))
                                   ?? RefiningLocations.FirstOrDefault();
        _refiningLocationSearchText = SelectedRefiningLocation?.DisplayName;
        OnPropertyChanged(nameof(RefiningLocationSearchText));
        ApplySelectedRefiningLocationReturnRate();
        UpdateRefiningLocationListBox(_refiningLocationSearchText);
        IsRefiningLocationPopupOpen = false;
    }

    private void ApplySelectedRefiningLocationReturnRate()
    {
        ReturnRatePercent = GetSelectedRefiningLocationReturnRate();
        OnPropertyChanged(nameof(SelectedRefiningLocationBonusSummary));
    }

    private decimal GetSelectedRefiningLocationReturnRate()
    {
        return CraftingLocationData.GetExpectedReturnRatePercent(EffectiveRefiningBonusPercent);
    }

    private decimal GetSelectedHideoutBonusPercent()
    {
        if (SelectedHideoutBonus == null || !IsHideoutRefiningLocation())
        {
            return 0m;
        }

        return SelectedHideoutBonus.GetBonusPercent(ShouldApplySpecialistHideoutBonus());
    }

    private bool IsHideoutRefiningLocation()
    {
        var clusterType = SelectedRefiningLocation?.ClusterType ?? string.Empty;
        var clusterId = SelectedRefiningLocation?.ClusterId ?? string.Empty;

        return clusterType.Contains("OPENPVP_BLACK", StringComparison.OrdinalIgnoreCase)
               || clusterType.Contains("TUNNEL", StringComparison.OrdinalIgnoreCase)
               || clusterId.StartsWith("TNL-", StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldApplySpecialistHideoutBonus()
    {
        return (SelectedRefiningLocation?.MatchingModifierPercent ?? 0m) > 0m;
    }

    private void UpdatePercentValueFromInput(string text, Action<decimal> updateValue)
    {
        if (_isUpdatingPercentInputText || IsPendingDecimalInput(text))
        {
            return;
        }

        if (TryParseDecimalInput(text, out var value))
        {
            updateValue(value);
        }
    }

    private void UpdatePercentInputText(string propertyName, decimal value)
    {
        if (_isUpdatingPercentInputText)
        {
            return;
        }

        var text = FormatPercentInput(value);
        var changed = false;

        switch (propertyName)
        {
            case nameof(ReturnRatePercentText) when _returnRatePercentText != text:
                _returnRatePercentText = text;
                changed = true;
                break;

            case nameof(SalesTaxPercentText) when _salesTaxPercentText != text:
                _salesTaxPercentText = text;
                changed = true;
                break;

            case nameof(SetupFeePercentText) when _setupFeePercentText != text:
                _setupFeePercentText = text;
                changed = true;
                break;
        }

        if (!changed)
        {
            return;
        }

        try
        {
            _isUpdatingPercentInputText = true;
            OnPropertyChanged(propertyName);
        }
        finally
        {
            _isUpdatingPercentInputText = false;
        }
    }

    private static bool TryParseDecimalInput(string text, out decimal value)
    {
        const NumberStyles numberStyles = NumberStyles.AllowLeadingSign
                                         | NumberStyles.AllowDecimalPoint
                                         | NumberStyles.AllowLeadingWhite
                                         | NumberStyles.AllowTrailingWhite;

        if (decimal.TryParse(text, numberStyles, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        return decimal.TryParse(NormalizeDecimalSeparator(text), numberStyles, CultureInfo.CurrentCulture, out value);
    }

    private static bool IsPendingDecimalInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return GetDecimalSeparators().Any(text.EndsWith);
    }

    private static string NormalizeDecimalSeparator(string text)
    {
        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        var alternateDecimalSeparator = decimalSeparator == "," ? "." : ",";

        if (text.Contains(decimalSeparator, StringComparison.Ordinal)
            || !text.Contains(alternateDecimalSeparator, StringComparison.Ordinal))
        {
            return text;
        }

        return text.Replace(alternateDecimalSeparator, decimalSeparator);
    }

    private static string[] GetDecimalSeparators()
    {
        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        return decimalSeparator switch
        {
            "," => [",", "."],
            "." => [".", ","],
            _ => [decimalSeparator, ",", "."]
        };
    }

    private static string FormatPercentInput(decimal value)
    {
        return value.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private void AddResource(CraftingResourceEntry resource)
    {
        resource.ValuesChanged = Recalculate;
        resource.SourceLocationChanged = OnResourceSourceLocationChanged;

        if (UseGlobalCity)
        {
            _isSyncingSourceLocations = true;
            try
            {
                resource.SourceLocation = SelectedBuyMarketLocation;
            }
            finally
            {
                _isSyncingSourceLocations = false;
            }
        }

        Resources.Add(resource);
    }

    private void Recalculate()
    {
        var calculationInput = CreateCalculationInput();
        Calculation = _calculator.Calculate(calculationInput);
        ApplyCalculationToBindings(Calculation);
    }

    private CraftingCalculationInput CreateCalculationInput()
    {
        return new CraftingCalculationInput
        {
            ItemUniqueName = SelectedItem?.UniqueName,
            CraftingRuns = Math.Max(1, RefiningRuns),
            AmountCrafted = Math.Max(1, _amountCrafted),
            ReturnRatePercent = ReturnRatePercent,
            UsesFocus = false,
            OutputUnitPrice = OutputUnitPrice,
            StationFee = StationFee,
            NutritionConsumedPerRun = _stationFeeService.GetNutritionConsumedPerRun(SelectedItem),
            SalesTaxPercent = SalesTaxPercent,
            SetupFeePercent = SetupFeePercent,
            OtherCosts = OtherCosts,
            OutputUnitWeight = ItemController.GetWeight(SelectedItem?.FullItemInformation),
            Resources = Resources
                .Select(x => new CraftingResourceInput
                {
                    UniqueName = x.UniqueName,
                    QuantityPerRun = x.QuantityPerRun,
                    UnitPrice = x.UnitPrice,
                    UnitWeight = x.UnitWeight,
                    IsReturnable = x.IsReturnable,
                    MaxReturnQuantityPerRun = x.MaxReturnQuantityPerRun,
                    ResourceKind = x.ResourceKind
                })
                .ToList(),
            Journal = null
        };
    }

    private void ApplyCalculationToBindings(CraftingCalculationResult result)
    {
        foreach (var resourceResult in result.Resources)
        {
            var resource = Resources.FirstOrDefault(x => x.UniqueName == resourceResult.UniqueName);
            if (resource == null)
            {
                continue;
            }

            resource.GrossQuantity = resourceResult.GrossQuantity;
            resource.ExpectedReturnQuantity = resourceResult.ExpectedReturnQuantity;
            resource.NetQuantity = resourceResult.NetQuantity;
            resource.GrossCost = resourceResult.GrossCost;
            resource.NetCost = resourceResult.NetCost;
        }
    }

    private async Task LoadSellPriceOptionsAsync()
    {
        var selectedItem = SelectedItem;
        if (selectedItem == null)
        {
            return;
        }

        var itemUniqueName = selectedItem.UniqueName;

        await CraftingBindings.LoadPriceOptionsAsync(itemUniqueName, SellPriceOptions, CraftingPricePreference.HigherIsBetter);

        _sellPriceOptionsItemUniqueName = itemUniqueName;
        IsSellPricePopupOpen = SellPriceOptions.Count > 0;
    }

    private void ClearSellPriceOptions()
    {
        _sellPriceOptionsItemUniqueName = string.Empty;
        SellPriceOptions.Clear();
        IsSellPricePopupOpen = false;
    }

    public static string TranslationRefining => LocalizationController.Translation("REFINING");
    public static string TranslationRefiningLocation => LocalizationController.Translation("REFINING_LOCATION");
    public static string TranslationRefiningSettingsSection => LocalizationController.Translation("REFINING_SETTINGS_SECTION");
    public static string TranslationResourceType => LocalizationController.Translation("RESOURCE_TYPE");
    public static string TranslationSelectItemToStart => LocalizationController.Translation("CRAFTING_SELECT_ITEM_TO_START");
    public static string TranslationProfitable => LocalizationController.Translation("CRAFTING_PROFITABLE");
    public static string TranslationNotProfitable => LocalizationController.Translation("CRAFTING_NOT_PROFITABLE");
    public static string TranslationNumberOfRuns => LocalizationController.Translation("NUMBER_OF_RUNS");
    public static string TranslationReturnRatePercent => LocalizationController.Translation("RETURN_RATE_PERCENT");
    public static string TranslationDailyBonusRrr => LocalizationController.Translation("DAILY_BONUS_RRR");
    public static string TranslationHideoutBonus => LocalizationController.Translation("HIDEOUT_BONUS");
    public static string TranslationPriceMarket => LocalizationController.Translation("PRICE_MARKET");
    public static string TranslationBuyMarketLocation => LocalizationController.Translation("REFINING_BUY_MARKET_LOCATION");
    public static string TranslationSellMarketLocation => LocalizationController.Translation("REFINING_SELL_MARKET_LOCATION");
    public static string TranslationStationFee => LocalizationController.Translation("STATION_FEE");
    public static string TranslationSalesTaxPercent => LocalizationController.Translation("SALES_TAX_PERCENT");
    public static string TranslationSetupFee => LocalizationController.Translation("SETUP_FEE");
    public static string TranslationOtherCosts => LocalizationController.Translation("OTHER_COSTS");
    public static string TranslationSellPrice => LocalizationController.Translation("SELL_PRICE");
    public static string TranslationPrices => LocalizationController.Translation("PRICES");
    public static string TranslationResources => LocalizationController.Translation("RESOURCES");
    public static string TranslationUseGlobalCity => LocalizationController.Translation("USE_GLOBAL_CITY");
    public static string TranslationOptimizePurchase => LocalizationController.Translation("OPTIMIZE_PURCHASE");
    public static string TranslationBuyCity => LocalizationController.Translation("CRAFTING_BUY_CITY");
    public static string TranslationExcludedCities => LocalizationController.Translation("CRAFTING_EXCLUDED_CITIES");
    public static string TranslationResults => LocalizationController.Translation("RESULTS");
    public static string TranslationRevenue => LocalizationController.Translation("REVENUE");
    public static string TranslationCosts => LocalizationController.Translation("COSTS");
    public static string TranslationProfitabilitySection => LocalizationController.Translation("PROFITABILITY_SECTION");
    public static string TranslationWeightSection => LocalizationController.Translation("WEIGHT");
    public static string TranslationOutput => LocalizationController.Translation("OUTPUT");
    public static string TranslationTotalCosts => LocalizationController.Translation("TOTAL_COSTS");
    public static string TranslationSalesGross => LocalizationController.Translation("SALES_GROSS");
    public static string TranslationSalesNet => LocalizationController.Translation("SALES_NET");
    public static string TranslationGrossMaterials => LocalizationController.Translation("GROSS_MATERIALS");
    public static string TranslationNetMaterials => LocalizationController.Translation("NET_MATERIALS");
    public static string TranslationNonReturnable => LocalizationController.Translation("NON_RETURNABLE");
    public static string TranslationStation => LocalizationController.Translation("STATION");
    public static string TranslationProfit => LocalizationController.Translation("PROFIT");
    public static string TranslationProfitPerItem => LocalizationController.Translation("PROFIT_PER_ITEM");
    public static string TranslationRoi => LocalizationController.Translation("ROI");
    public static string TranslationBreakEven => LocalizationController.Translation("BREAK_EVEN_PRICE");
    public static string TranslationWeightBefore => LocalizationController.Translation("WEIGHT_BEFORE");
    public static string TranslationWeightAfter => LocalizationController.Translation("WEIGHT_AFTER");
    public static string TranslationBonus => LocalizationController.Translation("BONUS");
    public static string TranslationExpectedRrr => LocalizationController.Translation("EXPECTED_RRR");
    public static string TranslationIcon => LocalizationController.Translation("ICON");
    public static string TranslationItem => LocalizationController.Translation("ITEM");
    public static string TranslationType => LocalizationController.Translation("TYPE");
    public static string TranslationGross => LocalizationController.Translation("GROSS");
    public static string TranslationExpectedReturn => LocalizationController.Translation("EXPECTED_RETURN");
    public static string TranslationNet => LocalizationController.Translation("NET");
    public static string TranslationPrice => LocalizationController.Translation("PRICE");
    public static string TranslationNetCost => LocalizationController.Translation("NET_COST");
    public static string TranslationTier => "Tier";
}
