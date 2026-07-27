using Serilog;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Enumerations;
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

namespace StatisticsAnalysisTool.Crafting;

/// <summary>
/// Scans every refinable resource (Wood/Ore/Fiber/Hide/Rock at every tier/enchantment) and ranks them by estimated
/// refining ROI, mirroring CraftingOptimizerBindings but sourced from RefiningRecipeResolver. Refining has no
/// Focus mechanic, so there's no return-rate-from-Focus toggle here - ReturnRatePercent is a flat manual input.
/// </summary>
public class RefiningOptimizerBindings : BaseViewModel
{
    private readonly RefiningRecipeResolver _recipeResolver = new();
    private readonly CraftingCalculator _calculator = new();
    private CancellationTokenSource _scanCancellation;

    public ObservableCollection<CraftingOptimizerResult> Results { get; } = [];

    public Dictionary<ItemTier, string> ItemTiers => FrequentlyValues.ItemTiers;

    public KeyValuePair<MarketLocation, string>[] MarketLocations { get; } = [.. Locations.OnceMarketLocations];

    public ItemTier SelectedTier
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = ItemTier.Unknown;

    public MarketLocation SelectedMarketLocation
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = MarketLocation.CaerleonMarket;

    public decimal SalesTaxPercent
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = 4m;

    public decimal SetupFeePercent
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = 2.5m;

    public decimal ReturnRatePercent
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
    = 0m;

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

    public void CancelScan()
    {
        _scanCancellation?.Cancel();
    }

    public async Task ScanAsync()
    {
        if (IsScanning)
        {
            return;
        }

        _scanCancellation = new CancellationTokenSource();
        var token = _scanCancellation.Token;

        try
        {
            IsScanning = true;
            Results.Clear();

            var candidates = ItemController.Items
                .Where(_recipeResolver.IsRefinable)
                .Where(item => SelectedTier == ItemTier.Unknown || item.Tier == (int) SelectedTier)
                .OrderBy(item => item.LocalizedName)
                .ToList();

            var scanned = 0;
            foreach (var item in candidates)
            {
                if (token.IsCancellationRequested)
                {
                    StatusText = LocalizationController.Translation("CRAFTING_OPTIMIZER_CANCELLED");
                    break;
                }

                scanned++;
                StatusText = string.Format(LocalizationController.Translation("CRAFTING_OPTIMIZER_SCANNING"), scanned, candidates.Count, item.LocalizedName);

                var result = await ScanItemAsync(item);
                if (result is { Profit: > 0 })
                {
                    InsertSortedByRoiDescending(result);
                }

                await Task.Delay(150, token);
            }

            if (!token.IsCancellationRequested)
            {
                StatusText = string.Format(LocalizationController.Translation("CRAFTING_OPTIMIZER_DONE"), Results.Count);
            }
        }
        catch (TooManyRequestsException)
        {
            StatusText = LocalizationController.Translation("CRAFTING_OPTIMIZER_RATE_LIMITED");
        }
        catch (TaskCanceledException)
        {
            StatusText = LocalizationController.Translation("CRAFTING_OPTIMIZER_CANCELLED");
        }
        catch (Exception e)
        {
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            StatusText = LocalizationController.Translation("CRAFTING_OPTIMIZER_ERROR");
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task<CraftingOptimizerResult> ScanItemAsync(Item item)
    {
        var resources = _recipeResolver.GetResources(item);
        if (resources.Count == 0)
        {
            return null;
        }

        var outputUnitPrice = await CraftingBindings.LoadPriceAsync(item.UniqueName, SelectedMarketLocation, SelectedMarketLocation == MarketLocation.BlackMarket);
        if (outputUnitPrice <= 0m)
        {
            return null;
        }

        var resourceInputs = new List<CraftingResourceInput>();
        foreach (var resource in resources)
        {
            var unitPrice = await CraftingBindings.LoadPriceAsync(resource.UniqueName, SelectedMarketLocation, false);
            resourceInputs.Add(new CraftingResourceInput
            {
                UniqueName = resource.UniqueName,
                QuantityPerRun = resource.QuantityPerRun,
                UnitPrice = unitPrice,
                UnitWeight = resource.UnitWeight,
                IsReturnable = resource.IsReturnable,
                MaxReturnQuantityPerRun = resource.MaxReturnQuantityPerRun,
                ResourceKind = resource.ResourceKind
            });
        }

        var input = new CraftingCalculationInput
        {
            ItemUniqueName = item.UniqueName,
            CraftingRuns = 1,
            AmountCrafted = _recipeResolver.GetAmountCrafted(item),
            ReturnRatePercent = ReturnRatePercent,
            OutputUnitPrice = outputUnitPrice,
            SalesTaxPercent = SalesTaxPercent,
            SetupFeePercent = SetupFeePercent,
            Resources = resourceInputs
        };

        var calculation = _calculator.Calculate(input);

        return new CraftingOptimizerResult
        {
            Item = item,
            OutputUnitPrice = outputUnitPrice,
            NetMaterialCosts = calculation.NetMaterialCosts,
            Profit = calculation.Profit,
            ProfitPerItem = calculation.ProfitPerItem,
            RoiPercent = calculation.RoiPercent,
            BreakEvenPrice = calculation.BreakEvenPrice
        };
    }

    public static string TranslationOptimizer => LocalizationController.Translation("REFINING_OPTIMIZER");
    public static string TranslationTier => LocalizationController.Translation("TIER");
    public static string TranslationScan => LocalizationController.Translation("CRAFTING_OPTIMIZER_SCAN");
    public static string TranslationCancel => LocalizationController.Translation("CRAFTING_OPTIMIZER_CANCEL_SCAN");
    public static string TranslationRoi => LocalizationController.Translation("CRAFTING_OPTIMIZER_ROI");
    public static string TranslationProfitPerItem => LocalizationController.Translation("CRAFTING_OPTIMIZER_PROFIT_PER_ITEM");
    public static string TranslationBreakEvenPrice => LocalizationController.Translation("BREAK_EVEN_PRICE");

    private void InsertSortedByRoiDescending(CraftingOptimizerResult result)
    {
        var index = 0;
        while (index < Results.Count && Results[index].RoiPercent >= result.RoiPercent)
        {
            index++;
        }

        Results.Insert(index, result);
    }
}
