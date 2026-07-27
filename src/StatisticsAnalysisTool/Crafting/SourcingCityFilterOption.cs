using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.ViewModels;

namespace StatisticsAnalysisTool.Crafting;

/// <summary>
/// One city checkbox in the multi-city sourcing "excluded cities" filter. Excluded cities are
/// skipped by OptimizePurchaseAsync when it looks for the cheapest buy city per resource.
/// Session-only - not persisted.
/// </summary>
public class SourcingCityFilterOption : BaseViewModel
{
    public MarketLocation Location { get; init; }

    public string DisplayName { get; init; }

    public bool IsExcluded
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
}
