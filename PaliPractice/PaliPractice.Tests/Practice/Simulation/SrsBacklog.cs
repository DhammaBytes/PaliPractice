using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData.Entities;

namespace PaliPractice.Tests.Practice.Simulation;

internal sealed record SrsBucketBacklog(int MinLevel, int MaxLevel, int DueBefore,
    int EnteredDueSet, int Served, int DueAfter, double MaxOverdueDays,
    int MaxEligibleVisitsSkipped, long? OldestUnservedFormId, double DueCardDays);

internal static class SrsBacklog
{
    public static int Bucket(int level) => (level - 1) / 2;

    public static void AccumulateDebt(double[] debt, IEnumerable<FormMasteryBase> mastery,
        IReadOnlySet<long> eligible, DateTime previousEnd, DateTime now)
    {
        foreach (var m in mastery.Where(m => eligible.Contains(m.FormId) && m.MasteryLevel is >= 1 and <= 10))
        {
            var start = m.NextDueUtc > previousEnd ? m.NextDueUtc : previousEnd;
            debt[Bucket(m.MasteryLevel)] += Math.Max(0, (now - start).TotalDays);
        }
    }

    public static SrsBucketBacklog[] Capture(IReadOnlyList<FormMasteryBase> dueBefore,
        IReadOnlyList<FormMasteryBase> dueAfter, IReadOnlySet<long> priorDue,
        IReadOnlyList<SrsAnswer> answers, IReadOnlyDictionary<long, int> waiting,
        double[] debt, DateTime now)
    {
        var served = answers.Where(a => a.Source == PracticeItemSource.DueForReview).ToList();
        return Enumerable.Range(0, 5).Select(bucket =>
        {
            var before = dueBefore.Where(m => Bucket(m.MasteryLevel) == bucket).ToList();
            var unserved = before.Where(m => waiting.ContainsKey(m.FormId)).ToList();
            return new SrsBucketBacklog(bucket * 2 + 1, bucket * 2 + 2, before.Count,
                before.Count(m => !priorDue.Contains(m.FormId)),
                served.Count(a => Bucket(a.BeforeLevel) == bucket),
                dueAfter.Count(m => Bucket(m.MasteryLevel) == bucket),
                before.Count == 0 ? 0 : before.Max(m => (now - m.NextDueUtc).TotalDays),
                unserved.Select(m => waiting[m.FormId]).DefaultIfEmpty().Max(),
                unserved.OrderBy(m => m.NextDueUtc).ThenBy(m => m.FormId).FirstOrDefault()?.FormId,
                debt[bucket]);
        }).ToArray();
    }
}
