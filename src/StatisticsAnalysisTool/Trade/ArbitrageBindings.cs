using Serilog;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Common.UserSettings;
using StatisticsAnalysisTool.Exceptions;
using StatisticsAnalysisTool.GameFileData;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace StatisticsAnalysisTool.Trade;

/// <summary>
/// Scans a user-picked watchlist for cross-city buy-low/sell-high spreads, using the same
/// Albion Data Project price API the rest of the app already calls. Only Normal (quality 1)
/// prices are considered, and margin is net of the configured market tax + setup fee.
/// </summary>
public class ArbitrageBindings : BaseViewModel
{
    private const int NormalQuality = 1;
    private CancellationTokenSource _scanCancellation;

    public ObservableCollection<Item> AvailableItems { get; }

    public ObservableCollection<Item> Watchlist { get; } = [];

    public ObservableCollection<ArbitrageOpportunity> Results { get; } = [];

    /// <summary>First entry (null) means "cualquiera / mejor precio" - falls back to auto-detecting the best city on that side.</summary>
    public KeyValuePair<MarketLocation?, string>[] CityFilterLocations { get; } =
        new KeyValuePair<MarketLocation?, string>[] { new(null, LocalizationController.Translation("ARBITRAGE_ANY_CITY")) }
            .Concat(Locations.OnceMarketLocations.Select(x => new KeyValuePair<MarketLocation?, string>(x.Key, x.Value)))
            .ToArray();

    /// <summary>
    /// Bound via SelectedItem rather than SelectedValue/SelectedValuePath - matching a null Key through
    /// SelectedValuePath is unreliable in WPF, so the full entry is tracked instead of just the Key.
    /// </summary>
    public KeyValuePair<MarketLocation?, string> SelectedBuyLocationEntry
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public KeyValuePair<MarketLocation?, string> SelectedSellLocationEntry
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public MarketLocation? SelectedBuyLocation => SelectedBuyLocationEntry.Key;
    public MarketLocation? SelectedSellLocation => SelectedSellLocationEntry.Key;

    /// <summary>Root shop categories (weapons, armor, resources, ...), same source as the main item-search filter.</summary>
    public ObservableCollection<CategoryDropdownItem> Categories { get; } = [];

    public CategoryDropdownItem SelectedCategory
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Plain text so an empty box means "no tier limit" - parsed on scan, same convention as other numeric text fields in this app.</summary>
    public string MinTierText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = string.Empty;

    public string MaxTierText
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = string.Empty;

    public ArbitrageBindings()
    {
        AvailableItems = new ObservableCollection<Item>(ItemController.Items.OrderBy(x => x.LocalizedName));
        SelectedBuyLocationEntry = CityFilterLocations[0];
        SelectedSellLocationEntry = CityFilterLocations[0];

        foreach (var category in ItemController.GetRootCategories().OrderBy(cat => cat.Value, StringComparer.Ordinal))
        {
            Categories.Add(new CategoryDropdownItem
            {
                Id = category.Id,
                Value = category.Value,
                DisplayName = LocalizationController.Translation("@MARKETPLACEGUI_ROLLOUT_SHOPCATEGORY_" + category.Id.ToUpperInvariant())
            });
        }

        Results.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(BestOpportunity));
            OnPropertyChanged(nameof(BestOpportunityVisibility));
            OnPropertyChanged(nameof(BestOpportunityEmptyVisibility));
        };
    }

    public ArbitrageOpportunity BestOpportunity => Results.FirstOrDefault();

    public Visibility BestOpportunityVisibility => BestOpportunity == null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility BestOpportunityEmptyVisibility => BestOpportunity == null ? Visibility.Visible : Visibility.Collapsed;

    public Item SelectedItemToAdd
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsScanning
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

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

    public void AddToWatchlist()
    {
        if (SelectedItemToAdd == null || Watchlist.Contains(SelectedItemToAdd))
        {
            return;
        }

        Watchlist.Add(SelectedItemToAdd);
    }

    public void RemoveFromWatchlist(Item item)
    {
        if (item == null)
        {
            return;
        }

        Watchlist.Remove(item);

        foreach (var stale in Results.Where(x => x.Item == item).ToList())
        {
            Results.Remove(stale);
        }
    }

    public void CancelScan()
    {
        _scanCancellation?.Cancel();
    }

    public async Task ScanAsync()
    {
        await RunScanAsync(Watchlist.ToList());
    }

    /// <summary>
    /// Scans every item in the selected shop category (optionally tier-limited) instead of a manually built
    /// watchlist. Still bounded to one category at a time - scanning every item in the game would take
    /// 20-30+ minutes against the price API and risks hitting its rate limit.
    /// </summary>
    public async Task ScanCategoryAsync()
    {
        if (SelectedCategory == null)
        {
            return;
        }

        var minTier = int.TryParse(MinTierText, out var min) ? min : (int?) null;
        var maxTier = int.TryParse(MaxTierText, out var max) ? max : (int?) null;

        var items = ItemController.Items
            .Where(x => x.FullItemInformation?.ShopCategory == SelectedCategory.Id
                        && (!minTier.HasValue || x.Tier >= minTier.Value)
                        && (!maxTier.HasValue || x.Tier <= maxTier.Value))
            .ToList();

        await RunScanAsync(items);
    }

    private async Task RunScanAsync(List<Item> items)
    {
        if (IsScanning || items.Count == 0)
        {
            return;
        }

        _scanCancellation = new CancellationTokenSource();
        var token = _scanCancellation.Token;

        try
        {
            IsScanning = true;
            Results.Clear();

            var scanned = 0;
            foreach (var item in items)
            {
                if (token.IsCancellationRequested)
                {
                    StatusText = LocalizationController.Translation("ARBITRAGE_CANCELLED");
                    break;
                }

                scanned++;
                StatusText = string.Format(LocalizationController.Translation("ARBITRAGE_SCANNING"), scanned, items.Count, item.LocalizedName);

                var opportunity = await ScanItemAsync(item, SelectedBuyLocation, SelectedSellLocation);
                if (opportunity != null)
                {
                    InsertSortedByMarginDescending(opportunity);
                }

                await Task.Delay(150, token);
            }

            if (!token.IsCancellationRequested)
            {
                StatusText = string.Format(LocalizationController.Translation("ARBITRAGE_DONE"), Results.Count);
            }
        }
        catch (TooManyRequestsException)
        {
            StatusText = LocalizationController.Translation("ARBITRAGE_RATE_LIMITED");
        }
        catch (TaskCanceledException)
        {
            StatusText = LocalizationController.Translation("ARBITRAGE_CANCELLED");
        }
        catch (Exception e)
        {
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            StatusText = LocalizationController.Translation("ARBITRAGE_ERROR");
        }
        finally
        {
            IsScanning = false;
        }
    }

    private static async Task<ArbitrageOpportunity> ScanItemAsync(Item item, MarketLocation? buyLocation, MarketLocation? sellLocation)
    {
        var prices = await ApiController.GetCityItemPricesFromJsonAsync(item.UniqueName);
        var normalQuality = prices?
            .Where(x => x.QualityLevel == NormalQuality && x.SellPriceMin > 0 && x.BuyPriceMax > 0)
            .ToList();

        if (normalQuality == null || normalQuality.Count < 2)
        {
            return null;
        }

        MarketResponse cheapest;
        MarketResponse priciest;

        if (buyLocation.HasValue)
        {
            cheapest = normalQuality.FirstOrDefault(x => x.MarketLocation == buyLocation.Value);
        }
        else
        {
            cheapest = normalQuality.Where(x => x.MarketLocation != sellLocation).OrderBy(x => x.SellPriceMin).FirstOrDefault();
        }

        if (sellLocation.HasValue)
        {
            priciest = normalQuality.FirstOrDefault(x => x.MarketLocation == sellLocation.Value);
        }
        else
        {
            priciest = normalQuality.Where(x => x.MarketLocation != buyLocation).OrderByDescending(x => x.BuyPriceMax).FirstOrDefault();
        }

        if (cheapest == null || priciest == null || cheapest.MarketLocation == priciest.MarketLocation)
        {
            return null;
        }

        var buyPrice = (decimal) cheapest.SellPriceMin;
        var sellPriceGross = (decimal) priciest.BuyPriceMax;
        var taxRate = ((decimal) SettingsController.CurrentSettings.TradeMonitoringMarketTaxRate + (decimal) SettingsController.CurrentSettings.TradeMonitoringMarketTaxSetupRate) / 100m;
        var sellPriceNet = Math.Round(sellPriceGross * (1m - taxRate), 2);
        var marginPerItem = Math.Round(sellPriceNet - buyPrice, 2);

        if (marginPerItem <= 0m)
        {
            return null;
        }

        var marginPercent = buyPrice > 0m ? Math.Round(marginPerItem / buyPrice * 100m, 2) : 0m;
        var locationNames = Locations.OnceMarketLocations.ToDictionary(x => x.Key, x => x.Value);

        return new ArbitrageOpportunity
        {
            Item = item,
            BuyLocation = cheapest.MarketLocation,
            BuyLocationName = locationNames.GetValueOrDefault(cheapest.MarketLocation, cheapest.City),
            BuyPrice = buyPrice,
            SellLocation = priciest.MarketLocation,
            SellLocationName = locationNames.GetValueOrDefault(priciest.MarketLocation, priciest.City),
            SellPriceGross = sellPriceGross,
            SellPriceNet = sellPriceNet,
            MarginPerItem = marginPerItem,
            MarginPercent = marginPercent
        };
    }

    private void InsertSortedByMarginDescending(ArbitrageOpportunity opportunity)
    {
        var index = 0;
        while (index < Results.Count && Results[index].MarginPercent >= opportunity.MarginPercent)
        {
            index++;
        }

        Results.Insert(index, opportunity);
    }

    public static string TranslationArbitrage => LocalizationController.Translation("ARBITRAGE");
    public static string TranslationWatchlist => LocalizationController.Translation("ARBITRAGE_WATCHLIST");
    public static string TranslationItem => LocalizationController.Translation("ITEM");
    public static string TranslationAddToWatchlist => LocalizationController.Translation("ARBITRAGE_ADD");
    public static string TranslationScan => LocalizationController.Translation("ARBITRAGE_SCAN");
    public static string TranslationCancel => LocalizationController.Translation("CRAFTING_OPTIMIZER_CANCEL_SCAN");
    public static string TranslationBuyAt => LocalizationController.Translation("ARBITRAGE_BUY_AT");
    public static string TranslationSellAt => LocalizationController.Translation("ARBITRAGE_SELL_AT");
    public static string TranslationBuyCity => LocalizationController.Translation("ARBITRAGE_BUY_CITY");
    public static string TranslationSellCity => LocalizationController.Translation("ARBITRAGE_SELL_CITY");
    public static string TranslationCategoryScan => LocalizationController.Translation("ARBITRAGE_CATEGORY_SCAN");
    public static string TranslationCategory => LocalizationController.Translation("ARBITRAGE_CATEGORY");
    public static string TranslationTierRange => LocalizationController.Translation("ARBITRAGE_TIER_RANGE");
    public static string TranslationScanCategory => LocalizationController.Translation("ARBITRAGE_SCAN_CATEGORY");
    public static string TranslationMargin => LocalizationController.Translation("ARBITRAGE_MARGIN");
    public static string TranslationBestOpportunity => LocalizationController.Translation("ARBITRAGE_BEST_OPPORTUNITY");
    public static string TranslationBestOpportunityEmpty => LocalizationController.Translation("ARBITRAGE_BEST_OPPORTUNITY_EMPTY");
}
