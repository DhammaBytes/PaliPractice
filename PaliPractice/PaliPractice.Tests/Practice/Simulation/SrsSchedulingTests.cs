using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;

namespace PaliPractice.Tests.Practice.Simulation;

[TestFixture]
public class SrsSchedulingTests
{
    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task SpacingSkip_PreservesUrgencyOfUnselectedReviews(PracticeType type)
    {
        var corpus = SrsSimulationTests.SyntheticCorpus(2);
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start));
        var eligible = SrsSimulationTests.SyntheticEligible(type, 2);
        var divisor = type == PracticeType.Declension ? 10000 : 100000;
        var lemmas = eligible.Order().GroupBy(id => id / divisor).Select(g => g.Take(4).ToArray()).ToArray();
        var active = lemmas.SelectMany(ids => ids).ToHashSet();
        foreach (var (id, i) in eligible.Order().Select((id, i) => (id, i)))
            sim.SeedMastery(type, id, active.Contains(id) ? 1 : 11,
                SrsSimulationTests.Start.UtcDateTime.AddDays(-10).AddSeconds(i));

        var session = await sim.RunSession(type, 50, 5, eligible, (_, _) => true);
        // A1, B2, A3, B1 satisfy the same spacing in either implementation.
        // At position 4 both A2 and A4 fit, but A2 became due earlier.
        Assert.That(session.Answers.Take(4).Select(a => a.FormId),
            Is.EqualTo(new[] { lemmas[0][0], lemmas[1][1], lemmas[0][2], lemmas[1][0] }));
        Assert.That(session.Answers[4].FormId, Is.EqualTo(lemmas[0][1]),
            "Skipping an urgent card for spacing must not put it behind a newer compatible review");
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task SparseBuckets_ReceiveEqualServiceAcrossRestarts(PracticeType type)
    {
        var corpus = SrsSimulationTests.SyntheticCorpus(20);
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start));
        var eligible = SrsSimulationTests.SyntheticEligible(type, 20);
        foreach (var (id, i) in eligible.Order().Select((id, i) => (id, i)))
            sim.SeedMastery(type, id, i % 2 == 0 ? 1 : 9, SrsSimulationTests.Start.UtcDateTime.AddYears(-2));
        var levels = new List<int>();
        for (int i = 0; i < 10; i++)
            levels.Add((await sim.RunSession(type, 50, 1, eligible, (_, _) => true)).Answers.Single().BeforeLevel);
        Assert.That(levels, Is.EqualTo(Enumerable.Range(0, 10).Select(i => i % 2 == 0 ? 1 : 9)));
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task OneCardRestarts_ServiceNewCardsAndEveryMasteryBucket(PracticeType type)
    {
        var corpus = SrsSimulationTests.SyntheticCorpus(20);
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start));
        var eligible = SrsSimulationTests.SyntheticEligible(type, 20);
        foreach (var (id, i) in eligible.Order().Take(60).Select((id, i) => (id, i)))
            sim.SeedMastery(type, id, i % 5 * 2 + 1, SrsSimulationTests.Start.UtcDateTime.AddYears(-2));
        var answers = new List<SrsAnswer>();
        for (int session = 0; session < 30; session++)
            answers.AddRange((await sim.RunSession(type, 50, 1, eligible, (_, _) => true)).Answers);
        Assert.That(answers.Count(a => a.Source == PracticeItemSource.NewForm), Is.EqualTo(5));
        var reviews = answers.Where(a => a.Source == PracticeItemSource.DueForReview).ToList();
        Assert.That(reviews.GroupBy(a => (a.BeforeLevel - 1) / 2).Select(g => g.Count()),
            Is.EquivalentTo(new[] { 5, 5, 5, 5, 5 }));
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task DueAdmission_ConsidersUrgencyBeyondFiveHundredEligibleRows(PracticeType type)
    {
        var corpus = SrsSimulationTests.SyntheticCorpus(150);
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start));
        sim.UserData.SetSetting(type == PracticeType.Declension ? SettingsKeys.NounsLemmaMax : SettingsKeys.VerbsLemmaMax, 150);
        var eligible = SrsSimulationTests.SyntheticEligible(type, 150);
        var urgent = eligible.Max();
        foreach (var id in eligible)
            sim.SeedMastery(type, id, id == urgent ? 1 : 2,
                SrsSimulationTests.Start.UtcDateTime.AddDays(id == urgent ? -2.5 : -3));
        var session = await sim.RunSession(type, 50, 1, eligible, (_, _) => true);
        Assert.That(session.DueBefore, Is.GreaterThan(500));
        Assert.That(session.Answers.Single().FormId, Is.EqualTo(urgent));
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task TwoLemmas_AlternateWhileBothHaveCompatibleDueCards(PracticeType type)
    {
        var corpus = SrsSimulationTests.SyntheticCorpus(2);
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start));
        var eligible = SrsSimulationTests.SyntheticEligible(type, 2);
        foreach (var (id, i) in eligible.Order().Select((id, i) => (id, i)))
            sim.SeedMastery(type, id, 1, SrsSimulationTests.Start.UtcDateTime.AddDays(-10).AddSeconds(i));
        var session = await sim.RunSession(type, 50, 4, eligible, (_, _) => true);
        var divisor = type == PracticeType.Declension ? 10000 : 100000;
        var lemmas = session.Answers.Select(a => a.FormId / divisor).ToArray();
        Assert.That(lemmas, Is.EqualTo(new[] { lemmas[0], lemmas[0] + 1, lemmas[0], lemmas[0] + 1 }));
    }
}
