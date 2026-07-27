using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Models;
using System.Windows.Media.Imaging;

namespace StatisticsAnalysisTool.Trade;

public class ArbitrageOpportunity
{
    public Item Item { get; init; }
    public string ItemName => Item?.LocalizedName ?? string.Empty;
    public BitmapImage Icon => Item?.Icon;
    public MarketLocation BuyLocation { get; init; }
    public string BuyLocationName { get; init; }
    public decimal BuyPrice { get; init; }
    public MarketLocation SellLocation { get; init; }
    public string SellLocationName { get; init; }
    public decimal SellPriceGross { get; init; }
    public decimal SellPriceNet { get; init; }
    public decimal MarginPerItem { get; init; }
    public decimal MarginPercent { get; init; }
}
