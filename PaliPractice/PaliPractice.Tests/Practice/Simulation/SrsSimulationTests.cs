using System.Text.Json;
using Path = System.IO.Path;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.UserData.Entities;
using PaliPractice.Tests.Practice.Builders;
using PaliPractice.Tests.Practice.Fakes;

namespace PaliPractice.Tests.Practice.Simulation;

[TestFixture]
public class SrsSimulationTests
{
    internal static readonly DateTimeOffset Start = new(2035, 1, 2, 12, 0, 0, TimeSpan.Zero);

    internal static FakeDatabaseService SyntheticCorpus(int count = 5) => new TestScenarioBuilder()
        .AddNouns(count, NounPattern.AMasc, Gender.Masculine).AddVerbs(count, VerbPattern.Ati).Build();

    internal static HashSet<long> SyntheticEligible(PracticeType type, int count = 5) => type == PracticeType.Declension
        ? (from lemma in Enumerable.Range(10001, count) from nounCase in new[] { 1, 2 }
           from number in new[] { 1, 2 } select (long)lemma * 10000 + nounCase * 1000 + 100 + number * 10).ToHashSet()
        : (from lemma in Enumerable.Range(70001, count) from person in new[] { 1, 2, 3 }
           from number in new[] { 1, 2 } where person != 3 || number != 1
           select (long)lemma * 100000 + 10000 + person * 1000 + number * 100 + 10).ToHashSet();

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task AbandonedAndBufferedCards_DoNotRecordAnswers(PracticeType type)
    {
        var corpus = SyntheticCorpus();
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(Start));
        var eligible = SyntheticEligible(type);
        var abandoned = await sim.RunSession(type, 50, 0, eligible, (_, _) => true);
        Assert.That(abandoned.PendingFormId, Is.Not.Null);
        Assert.That(sim.AllMastery(type), Is.Empty);
        Assert.That(sim.UserData.GetRecentHistory(type), Is.Empty);
        Assert.That(sim.UserData.GetPracticeCount(type), Is.Zero);
        var partial = await sim.RunSession(type, 50, 3, eligible, (_, _) => true);
        Assert.That(partial.Answers, Has.Count.EqualTo(3));
        Assert.That(sim.AllMastery(type), Has.Count.EqualTo(3));
        Assert.That(sim.UserData.GetRecentHistory(type), Has.Count.EqualTo(3));
        Assert.That(sim.UserData.GetPracticeCount(type), Is.EqualTo(3));
        var progress = sim.UserData.GetTodayProgress();
        Assert.That(type == PracticeType.Declension ? progress.DeclensionsCompleted : progress.ConjugationsCompleted, Is.EqualTo(3));
        Assert.That(sim.Mastery(type, partial.PendingFormId!.Value), Is.Null);
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task Rebuilds_RespectActualBudgetAndStopAtCooldown(PracticeType type)
    {
        var corpus = SyntheticCorpus();
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(Start));
        var eligible = SyntheticEligible(type);
        var rebuilt = await sim.RunSession(type, 5, 13, eligible, (_, _) => true);
        Assert.That(rebuilt.QueueBuilds, Is.EqualTo(3));
        Assert.That(rebuilt.Answers, Has.Count.EqualTo(13));
        var exhausted = await sim.RunSession(type, 50, 50, eligible, (_, _) => true);
        Assert.That(exhausted.Answers, Has.Count.EqualTo(eligible.Count - 13));
        Assert.That(exhausted.PendingFormId, Is.Null);
        var cooldown = await sim.RunSession(type, 50, 50, eligible, (_, _) => true);
        Assert.That(cooldown.Answers, Is.Empty);
        Assert.That(sim.UserData.GetRecentHistory(type, 100), Has.Count.EqualTo(eligible.Count));
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task ReopenedDatabase_ReplaysAndPreservesHistory(PracticeType type)
    {
        var corpus = SyntheticCorpus();
        var directory = Path.Combine(Path.GetTempPath(), "pali-srs-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            async Task<string> Run(bool reopen)
            {
                var clock = new SimulationTimeProvider(Start);
                var path = reopen ? Path.Combine(directory, "practice.db") : ":memory:";
                var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, clock, path);
                try
                {
                    var eligible = SyntheticEligible(type);
                    var first = await sim.RunSession(type, 50, 3, eligible, (_, i) => i % 2 == 0);
                    var history = JsonSerializer.Serialize(sim.UserData.GetRecentHistory(type).Cast<PracticeHistoryBase>());
                    if (reopen)
                    {
                        sim.Dispose();
                        sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, clock, path);
                    }
                    Assert.That(JsonSerializer.Serialize(sim.UserData.GetRecentHistory(type).Cast<PracticeHistoryBase>()), Is.EqualTo(history));
                    clock.UtcNow = Start.AddDays(30);
                    var second = await sim.RunSession(type, 5, 8, eligible, (_, i) => i % 3 == 0);
                    var saved = sim.UserData.GetRecentHistory(type, 100).Cast<PracticeHistoryBase>().ToList();
                    Assert.That(saved, Has.Count.EqualTo(11));
                    Assert.That(JsonSerializer.Serialize(saved.TakeLast(3)), Is.EqualTo(history));
                    return JsonSerializer.Serialize(new { first, second, saved });
                }
                finally { sim.Dispose(); }
            }
            Assert.That(await Run(true), Is.EqualTo(await Run(false)));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public async Task BundledCorpus_ReplaysAcrossTimeWithIndependentEligibility(PracticeType type)
    {
        using var corpus = new BundledSrsCorpus();
        var eligible = corpus.DefaultEligible(type);
        Assert.That(eligible.Count, Is.GreaterThan(130));
        async Task<string> Run()
        {
            using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(Start));
            var first = await sim.RunSession(type, 50, 130, eligible, (_, _) => false);
            Assert.That(first.Answers, Has.Count.EqualTo(130));
            Assert.That(first.QueueBuilds, Is.EqualTo(3));
            sim.Clock.UtcNow = Start.AddDays(7);
            var second = await sim.RunSession(type, 50, 30, eligible, (_, i) => i % 3 != 0);
            Assert.That(second.DueBefore, Is.EqualTo(130));
            return JsonSerializer.Serialize(new { first, second });
        }
        Assert.That(await Run(), Is.EqualTo(await Run()));
        TestContext.Out.WriteLine($"{type}: {eligible.Count} independently eligible default cards");
    }
}
