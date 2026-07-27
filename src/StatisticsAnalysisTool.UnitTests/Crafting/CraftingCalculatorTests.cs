using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Crafting;
using System.Collections.Generic;

namespace StatisticsAnalysisTool.UnitTests.Crafting;

[TestFixture]
public class CraftingCalculatorTests
{
    private readonly CraftingCalculator _calculator = new();

    [Test]
    public void Calculate_WithNoReturnRate_ChargesFullMaterialCost()
    {
        var input = new CraftingCalculationInput
        {
            CraftingRuns = 10,
            AmountCrafted = 1,
            OutputUnitPrice = 100m,
            SalesTaxPercent = 0m,
            SetupFeePercent = 0m,
            Resources =
            [
                new CraftingResourceInput { UniqueName = "T4_WOOD", QuantityPerRun = 4m, UnitPrice = 5m, IsReturnable = true }
            ]
        };

        var result = _calculator.Calculate(input);

        result.GrossMaterialCosts.Should().Be(200m); // 10 runs * 4 qty * 5 price
        result.NetMaterialCosts.Should().Be(200m); // 0% return rate -> no reduction
        result.SalesRevenueGross.Should().Be(1000m); // 10 output * 100 price
        result.Profit.Should().Be(800m);
    }

    [Test]
    public void Calculate_WithReturnRate_ReducesNetMaterialCost()
    {
        var input = new CraftingCalculationInput
        {
            CraftingRuns = 10,
            AmountCrafted = 1,
            ReturnRatePercent = 50m,
            OutputUnitPrice = 100m,
            Resources =
            [
                new CraftingResourceInput { UniqueName = "T4_WOOD", QuantityPerRun = 4m, UnitPrice = 5m, IsReturnable = true }
            ]
        };

        var result = _calculator.Calculate(input);

        result.GrossMaterialCosts.Should().Be(200m);
        result.NetMaterialCosts.Should().Be(100m); // 50% of materials returned
    }

    [Test]
    public void Calculate_NonReturnableResource_IgnoresReturnRate()
    {
        var input = new CraftingCalculationInput
        {
            CraftingRuns = 5,
            AmountCrafted = 1,
            ReturnRatePercent = 100m,
            OutputUnitPrice = 50m,
            Resources =
            [
                new CraftingResourceInput { UniqueName = "FAVOR_TOKEN", QuantityPerRun = 1m, UnitPrice = 20m, IsReturnable = false }
            ]
        };

        var result = _calculator.Calculate(input);

        result.NetMaterialCosts.Should().Be(100m); // 5 runs * 1 qty * 20 price, no return
        result.NonReturnableMaterialCosts.Should().Be(100m);
    }

    [Test]
    public void Calculate_UnprofitableRecipe_ReturnsNegativeProfitAndRoi()
    {
        var input = new CraftingCalculationInput
        {
            CraftingRuns = 1,
            AmountCrafted = 1,
            OutputUnitPrice = 10m,
            SetupFeePercent = 0m,
            Resources =
            [
                new CraftingResourceInput { UniqueName = "T4_WOOD", QuantityPerRun = 10m, UnitPrice = 5m, IsReturnable = false }
            ]
        };

        var result = _calculator.Calculate(input);

        result.Profit.Should().Be(-40m); // 10 revenue - 50 cost
        result.RoiPercent.Should().BeLessThan(0m);
    }

    [Test]
    public void Calculate_SalesTaxAndSetupFee_ReduceNetRevenue()
    {
        var input = new CraftingCalculationInput
        {
            CraftingRuns = 1,
            AmountCrafted = 1,
            OutputUnitPrice = 100m,
            SalesTaxPercent = 4m,
            SetupFeePercent = 2.5m,
            Resources = new List<CraftingResourceInput>()
        };

        var result = _calculator.Calculate(input);

        result.SalesTax.Should().Be(4m);
        result.SetupFee.Should().Be(2.5m);
        result.SalesRevenueNet.Should().Be(93.5m);
    }
}
