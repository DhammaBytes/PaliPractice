using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Practice;

namespace PaliPractice.Tests.Practice.Simulation;

[TestFixture]
public class NewFormScheduleTests
{
    [Test]
    public async Task FirstAnswerSeedRemainsStableAcrossTimeFiltersAndReopening(
        [Values(PracticeType.Declension, PracticeType.Conjugation)] PracticeType type)
    {
        var corpus = SrsSimulationTests.SyntheticCorpus(20);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"srs-seed-{Guid.NewGuid():N}.db");
        DateTime first;
        try
        {
            using (var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start), path))
            {
                Assert.That(sim.UserData.GetFirstPracticeUtc(type), Is.Null);
                await sim.RunSession(type, 50, 1, SrsSimulationTests.SyntheticEligible(type, 20), (_, _) => true);
                first = sim.UserData.GetFirstPracticeUtc(type)!.Value;
                Assert.That(first, Is.EqualTo(SrsSimulationTests.Start.UtcDateTime.AddSeconds(20)));
                sim.Clock.UtcNow = SrsSimulationTests.Start.AddDays(30);
                SrsFilter.Default(type).Apply(sim.UserData, type);
                await sim.RunSession(type, 50, 1, SrsSimulationTests.SyntheticEligible(type, 20), (_, _) => true);
                Assert.That(sim.UserData.GetFirstPracticeUtc(type), Is.EqualTo(first));
            }
            using var reopened = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start.AddDays(60)), path);
            Assert.That(reopened.UserData.GetFirstPracticeUtc(type), Is.EqualTo(first));
        }
        finally { System.IO.File.Delete(path); }
    }

    [Test]
    public void CadenceHasBoundedVariableGapsAndBalancedAdmission(
        [Values(PracticeType.Declension, PracticeType.Conjugation)] PracticeType type)
    {
        var slots = NewFormSchedule.Build(0, 3000, type, SrsSimulationTests.Start.UtcDateTime).Slots;
        var positions = slots.Select((isNew, i) => (isNew, i)).Where(x => x.isNew).Select(x => x.i).ToArray();
        var gaps = positions.Prepend(-1).Zip(positions, (a, b) => b - a).ToArray();
        Assert.That(gaps, Has.All.InRange(5, 7));
        Assert.That(gaps.Distinct(), Is.EquivalentTo(new[] { 5, 6, 7 }));
        Assert.That(slots.Chunk(30).Select(block => block.Count(x => x)), Has.All.EqualTo(5));
    }

    [Test]
    public void InterruptedPlansMatchUninterruptedPlanAndReviewOrdinal(
        [Values(PracticeType.Declension, PracticeType.Conjugation)] PracticeType type)
    {
        var full = NewFormSchedule.Build(0, 600, type, SrsSimulationTests.Start.UtcDateTime).Slots;
        for (int completed = 0; completed < 500; completed++)
        {
            var resumed = NewFormSchedule.Build(completed, 100, type, SrsSimulationTests.Start.UtcDateTime);
            Assert.That(resumed.Slots, Is.EqualTo(full.Skip(completed).Take(100)));
            Assert.That(resumed.ReviewsBefore, Is.EqualTo(full.Take(completed).LongCount(x => !x)));
        }
        var later = NewFormSchedule.Build(100, 100, type, SrsSimulationTests.Start.UtcDateTime.AddYears(1),
            SrsSimulationTests.Start.UtcDateTime);
        Assert.That(later.Slots, Is.EqualTo(full.Skip(100).Take(100)), "Recorded seed must override the current build date");
    }
}
