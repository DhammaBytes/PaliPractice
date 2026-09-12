using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;

namespace PaliPractice.Tests.Practice.Simulation;

[TestFixture]
public class SrsEvidenceTests
{
    [Test]
    public void ExplicitLearners_DoNotChangeWithOrderingSeed(
        [Values(PracticeType.Declension, PracticeType.Conjugation)] PracticeType type,
        [Values(SrsStudentProfile.AlwaysEasy, SrsStudentProfile.WeakPlural)] SrsStudentProfile profile)
    {
        var items = SrsSimulationTests.SyntheticEligible(type).Select(id =>
            new PracticeItem(id, type, 0, PracticeItemSource.NewForm, 0, 0)).ToArray();
        var first = SrsStudent.Answers(profile, type, "daily", 17);
        var second = SrsStudent.Answers(profile, type, "daily", 83);
        Assert.That(items.Select((item, i) => first(item, i)),
            Is.EqualTo(items.Select((item, i) => second(item, i))));
        var hardCount = items.Count(item => !first(item, 0));
        var expected = profile == SrsStudentProfile.AlwaysEasy ? 0 : type == PracticeType.Declension ? 10 : 15;
        Assert.That(hardCount, Is.EqualTo(expected));
    }

    [Test]
    public void Debt_IncludesMidBreakArrivalsAndExcludesRetiredAndDisabledCards()
    {
        var start = SrsSimulationTests.Start.UtcDateTime;
        var records = new[]
        {
            Mastery(1, 1, start.AddDays(-2)), Mastery(2, 3, start.AddDays(1)),
            Mastery(3, 11, start.AddDays(-10)), Mastery(4, 5, start.AddDays(-10))
        };
        var debt = new double[5];
        SrsBacklog.AccumulateDebt(debt, records, new HashSet<long> { 1, 2, 3 }, start, start.AddDays(3));
        Assert.That(debt, Is.EqualTo(new double[] { 3, 2, 0, 0, 0 }).Within(1e-9));
    }

    [Test]
    public void Buckets_ReportUnselectedCardsAndAttributeServiceToThePreviousLevel()
    {
        var now = SrsSimulationTests.Start.UtcDateTime;
        var due = new[] { Mastery(1, 2, now.AddDays(-3)), Mastery(2, 2, now.AddDays(-2)), Mastery(3, 9, now.AddDays(-20)) };
        var answers = new[] { new SrsAnswer(1, PracticeItemSource.DueForReview, 2, 3, now, "answer") };
        var buckets = SrsBacklog.Capture(due, due.Skip(1).ToArray(), new HashSet<long> { 1, 3 },
            answers, new Dictionary<long, int> { [2] = 1, [3] = 7 }, new double[5], now);
        Assert.That(buckets[0].Served, Is.EqualTo(1));
        Assert.That(buckets[1].Served, Is.Zero);
        Assert.That(buckets[0].EnteredDueSet, Is.EqualTo(1));
        Assert.That(buckets[4].DueBefore, Is.EqualTo(1));
        Assert.That(buckets[4].MaxEligibleVisitsSkipped, Is.EqualTo(7));
        Assert.That(buckets[4].OldestUnservedFormId, Is.EqualTo(3));
        Assert.That(buckets[4].MaxOverdueDays, Is.EqualTo(20).Within(1e-9));
    }

    static NounsFormMastery Mastery(long id, int level, DateTime due) => new()
    {
        FormId = id, MasteryLevel = level,
        LastPracticedUtc = due.AddHours(-CooldownCalculator.GetCooldownHours(level))
    };
}
