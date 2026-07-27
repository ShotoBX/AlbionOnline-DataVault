using System.Collections.Generic;
using System.Linq;

namespace StatisticsAnalysisTool.EventLogging;

public record LootSplitResult(long TotalValue, int ParticipantCount, long FairShare, IReadOnlyDictionary<string, long> BalanceByName);

/// <summary>
/// Pure fair-split math: given each participant's looted value, computes an equal share of the
/// total pool and how far each participant is from that share (positive = took more than their
/// share, negative = took less).
/// </summary>
public static class LootSplitCalculator
{
    public static LootSplitResult Calculate(IReadOnlyDictionary<string, long> lootedValueByName)
    {
        var participants = lootedValueByName ?? new Dictionary<string, long>();
        var totalValue = participants.Values.Sum();
        var participantCount = participants.Count;
        var fairShare = participantCount > 0 ? totalValue / participantCount : 0;

        var balanceByName = participants.ToDictionary(x => x.Key, x => x.Value - fairShare);

        return new LootSplitResult(totalValue, participantCount, fairShare, balanceByName);
    }
}
