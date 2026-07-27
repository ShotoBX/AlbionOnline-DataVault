using StatisticsAnalysisTool.Models;
using System.Windows.Media.Imaging;

namespace StatisticsAnalysisTool.Crafting;

public class CraftingOptimizerResult
{
    public Item Item { get; init; }
    public string ItemName => Item?.LocalizedName ?? string.Empty;
    public BitmapImage Icon => Item?.Icon;
    public decimal OutputUnitPrice { get; init; }
    public decimal NetMaterialCosts { get; init; }
    public decimal Profit { get; init; }
    public decimal ProfitPerItem { get; init; }
    public decimal RoiPercent { get; init; }
    public decimal BreakEvenPrice { get; init; }
}
