using FluentAssertions;
using NUnit.Framework;
using StatisticsAnalysisTool.Models;
using System;
using System.Linq;
using ValueType = StatisticsAnalysisTool.Enumerations.ValueType;

namespace StatisticsAnalysisTool.UnitTests.Models;

[TestFixture]
public class DashboardStatisticsTests
{
    // Dates must stay within DashboardStatistics' rolling retention window (Settings.Default.KeepDashboardStatisticsForDays,
    // pruned once per instance on the first Add call), so tests use dates relative to "today" rather than a fixed date.

    [Test]
    public void Add_DailyValues_SameDateAndType_MergesIntoSingleEntry()
    {
        var statistics = new DashboardStatistics();
        var date = DateTime.Today;

        statistics.Add(new DailyValues(ValueType.Silver, 100, date));
        statistics.Add(new DailyValues(ValueType.Silver, 50, date));

        statistics.DailyValues.Should().ContainSingle();
        statistics.DailyValues[0].Value.Should().Be(150);
    }

    [Test]
    public void Add_DailyValues_DifferentValueTypesSameDate_KeepsSeparateEntries()
    {
        var statistics = new DashboardStatistics();
        var date = DateTime.Today;

        statistics.Add(new DailyValues(ValueType.Silver, 100, date));
        statistics.Add(new DailyValues(ValueType.Fame, 200, date));

        statistics.DailyValues.Should().HaveCount(2);
        statistics.DailyValues.Sum(x => x.Value).Should().Be(300);
    }

    [Test]
    public void Add_DailyValues_DifferentDates_KeepsSeparateEntries()
    {
        var statistics = new DashboardStatistics();

        statistics.Add(new DailyValues(ValueType.Silver, 100, DateTime.Today));
        statistics.Add(new DailyValues(ValueType.Silver, 100, DateTime.Today.AddDays(-1)));

        statistics.DailyValues.Should().HaveCount(2);
    }

    [Test]
    public void Add_HourlyValues_SameHourAndType_MergesIntoSingleEntry()
    {
        var statistics = new DashboardStatistics();
        var baseHour = DateTime.Today.AddHours(14);

        statistics.Add(new HourlyValues(ValueType.Fame, 10, baseHour.AddMinutes(23)));
        statistics.Add(new HourlyValues(ValueType.Fame, 5, baseHour.AddMinutes(47)));

        statistics.HourlyValues.Should().ContainSingle();
        statistics.HourlyValues[0].Value.Should().Be(15);
    }
}
