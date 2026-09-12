using System.Text.Json;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.UserData;

namespace PaliPractice.Tests.Practice.Simulation;

[TestFixture]
public class SrsLivenessTests
{
    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task FrozenDueCohort_DrainsWithinAvailableCapacity(PracticeType type)
    {
        using var corpus = new BundledSrsCorpus();
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start));
        var filter = SrsFilter.Broad(type);
        filter.Apply(sim.UserData, type);
        var eligible = corpus.Eligible(type, filter);
        var cohort = eligible.Order().Take(600).ToHashSet();
        Assert.That(cohort, Has.Count.EqualTo(600));
        foreach (var (id, i) in cohort.Order().Select((id, i) => (id, i)))
            sim.SeedMastery(type, id, i % 10 + 1, SrsSimulationTests.Start.UtcDateTime.AddYears(-3));
        var served = new HashSet<long>();
        for (int visit = 0; visit < 8 && served.Count < cohort.Count; visit++)
        {
            var session = await sim.RunSession(type, 100, 100, eligible, (_, _) => false);
            served.UnionWith(session.Answers.Select(a => a.FormId).Where(cohort.Contains));
        }
        Assert.That(served, Is.EquivalentTo(cohort), "800 answer slots can service 600 due cards plus the planned new share");
        Assert.That(sim.Clock.UtcNow - SrsSimulationTests.Start, Is.LessThan(TimeSpan.FromDays(1)),
            "No answered card may become due again during this drain test");
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task IneligibleMastery_DoesNotChangeEligibleQueueOrService(PracticeType type)
    {
        var corpus = SrsSimulationTests.SyntheticCorpus(150);
        var eligible = SrsSimulationTests.SyntheticEligible(type, 20);
        async Task<string> Run(bool addDisabled)
        {
            using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start));
            sim.UserData.SetSetting(type == PracticeType.Declension ? SettingsKeys.NounsLemmaMax : SettingsKeys.VerbsLemmaMax, 20);
            foreach (var id in eligible.Order().Take(20))
                sim.SeedMastery(type, id, 1, SrsSimulationTests.Start.UtcDateTime.AddDays(-10));
            if (addDisabled)
                foreach (var id in SrsSimulationTests.SyntheticEligible(type, 150).Except(eligible))
                    sim.SeedMastery(type, id, 1, SrsSimulationTests.Start.UtcDateTime.AddYears(-2));
            return JsonSerializer.Serialize(await sim.RunSession(type, 50, 50, eligible, (_, _) => true));
        }
        Assert.That(await Run(true), Is.EqualTo(await Run(false)));
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task RetiredPool_RemainsUnavailableAfterOneYear(PracticeType type)
    {
        var corpus = SrsSimulationTests.SyntheticCorpus();
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start));
        var eligible = SrsSimulationTests.SyntheticEligible(type);
        foreach (var id in eligible)
            sim.SeedMastery(type, id, 11, SrsSimulationTests.Start.UtcDateTime.AddYears(-1));
        sim.Clock.UtcNow += TimeSpan.FromDays(365);
        var session = await sim.RunSession(type, 50, 50, eligible, (_, _) => false);
        Assert.That(session.Answers, Is.Empty);
        Assert.That(session.DueBefore, Is.Zero);
        Assert.That(sim.UserData.GetPracticeCount(type), Is.Zero);
        Assert.That(sim.AllMastery(type).Select(m => m.MasteryLevel), Is.All.EqualTo(11));
    }
}
