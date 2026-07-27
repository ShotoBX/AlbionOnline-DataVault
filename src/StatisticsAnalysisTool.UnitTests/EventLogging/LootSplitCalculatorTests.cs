using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.EventLogging;
using System.Collections.Generic;

namespace StatisticsAnalysisTool.UnitTests.EventLogging;

[TestFixture]
public class LootSplitCalculatorTests
{
    [Test]
    public void Calculate_EqualContributions_ProducesZeroBalances()
    {
        var lootedValues = new Dictionary<string, long>
        {
            ["Alice"] = 100,
            ["Bob"] = 100,
            ["Carol"] = 100
        };

        var result = LootSplitCalculator.Calculate(lootedValues);

        result.TotalValue.Should().Be(300);
        result.ParticipantCount.Should().Be(3);
        result.FairShare.Should().Be(100);
        result.BalanceByName["Alice"].Should().Be(0);
        result.BalanceByName["Bob"].Should().Be(0);
        result.BalanceByName["Carol"].Should().Be(0);
    }

    [Test]
    public void Calculate_UnequalContributions_ComputesPositiveAndNegativeBalances()
    {
        var lootedValues = new Dictionary<string, long>
        {
            ["Alice"] = 300,
            ["Bob"] = 0
        };

        var result = LootSplitCalculator.Calculate(lootedValues);

        result.TotalValue.Should().Be(300);
        result.FairShare.Should().Be(150);
        result.BalanceByName["Alice"].Should().Be(150);
        result.BalanceByName["Bob"].Should().Be(-150);
    }

    [Test]
    public void Calculate_NoParticipants_ReturnsZeroesWithoutDividingByZero()
    {
        var result = LootSplitCalculator.Calculate(new Dictionary<string, long>());

        result.TotalValue.Should().Be(0);
        result.ParticipantCount.Should().Be(0);
        result.FairShare.Should().Be(0);
        result.BalanceByName.Should().BeEmpty();
    }

    [Test]
    public void Calculate_NullInput_TreatedAsEmpty()
    {
        var result = LootSplitCalculator.Calculate(null);

        result.TotalValue.Should().Be(0);
        result.ParticipantCount.Should().Be(0);
    }
}
