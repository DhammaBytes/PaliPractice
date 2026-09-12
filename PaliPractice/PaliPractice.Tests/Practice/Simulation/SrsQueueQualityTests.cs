using System.Security.Cryptography;
using System.Text.Json;
using Path = System.IO.Path;
using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;

namespace PaliPractice.Tests.Practice.Simulation;

[TestFixture]
public class SrsQueueQualityTests
{
    static IEnumerable<TestCaseData> Scenarios()
    {
        foreach (var type in new[] { PracticeType.Declension, PracticeType.Conjugation })
        foreach (var profile in new[] { "default", "broad", "two-lemmas", "one-lemma", "rare", "rank-window", "one-combo" })
        foreach (var state in new[] { "new", "due", "mixed" })
            yield return new TestCaseData(type, profile, state);
    }

    [Test]
    public async Task RestartedPrefixes(
        [Values(PracticeType.Declension, PracticeType.Conjugation)] PracticeType type,
        [Values(1, 3)] int budget)
    {
        var corpus = SrsSimulationTests.SyntheticCorpus(20);
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start));
        var eligible = SrsSimulationTests.SyntheticEligible(type, 20);
        foreach (var (id, i) in eligible.Order().Take(60).Select((id, i) => (id, i)))
            sim.SeedMastery(type, id, i % 5 * 2 + 1, SrsSimulationTests.Start.UtcDateTime.AddYears(-2));
        var sessions = new List<SrsSession>();
        for (int visit = 0; visit < 30; visit++)
            sessions.Add(await sim.RunSession(type, 50, budget, eligible, (_, _) => true));
        // All answers occur before the shortest cooldown can expire.
        Assert.That(sim.Clock.UtcNow - SrsSimulationTests.Start, Is.LessThan(TimeSpan.FromDays(1)));
        WriteReport(type, "restarted", budget.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SrsFilter.Default(type), eligible.Order().ToArray(), [], [], sessions);
    }

    [TestCaseSource(nameof(Scenarios))]
    public void FixedStateAcrossDateSeeds(PracticeType type, string profile, string state)
    {
        using var corpus = new BundledSrsCorpus();
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(SrsSimulationTests.Start));
        var filter = Filter(type, profile);
        filter.Apply(sim.UserData, type);
        var eligible = corpus.Eligible(type, filter);
        Assert.That(sim.EligibleForms(type), Is.EquivalentTo(eligible));
        SeedState(sim, type, state, eligible);
        var mastery = sim.AllMastery(type).ToDictionary(m => m.FormId);
        var inventory = eligible.Order().Select(id => new
        {
            FormId = id, Pattern = Pattern(corpus, type, id),
            Level = mastery.TryGetValue(id, out var m) ? m.MasteryLevel : 0,
            DueUtc = mastery.TryGetValue(id, out var due) ? (DateTime?)due.NextDueUtc : null
        }).ToArray();
        var queues = new List<IReadOnlyList<PracticeItem>>();
        var seeds = Enumerable.Range(0, 24).Select(i => SrsSimulationTests.Start.UtcDateTime.AddDays(i * 17)).ToArray();
        foreach (var seed in seeds)
        {
            var queue = sim.BuildQueue(type, 60, seed);
            Assert.That(queue, Is.EqualTo(sim.BuildQueue(type, 60, seed)), "Same state and seed must replay exactly");
            Assert.That(queue.Select(q => q.FormId).Distinct().Count(), Is.EqualTo(queue.Count));
            Assert.That(queue.All(q => eligible.Contains(q.FormId)), Is.True);
            Assert.That(queue.All(q => q.Source == PracticeItemSource.NewForm
                ? !mastery.ContainsKey(q.FormId) : mastery.ContainsKey(q.FormId)), Is.True);
            queues.Add(queue);
        }
        Assert.That(sim.UserData.GetPracticeCount(type), Is.Zero, "Building and abandoning queues must not record answers");
        WriteReport(type, profile, state, filter, inventory, seeds, queues);
    }

    static void SeedState(SrsSimulation sim, PracticeType type, string state, IEnumerable<long> eligible)
    {
        var random = new Random(2718); // Initial state is independent of queue seeds.
        foreach (var id in eligible.Order())
        {
            var practiced = random.Next(2) == 0;
            var level = random.Next(1, 11);
            var due = SrsSimulationTests.Start.UtcDateTime.AddDays(-30).AddSeconds(random.Next(86400));
            if (state == "new" || (state == "mixed" && !practiced)) continue;
            sim.SeedMastery(type, id, level, due.AddHours(-CooldownCalculator.GetCooldownHours(level)));
        }
    }

    static SrsFilter Filter(PracticeType type, string profile) => profile switch
    {
        "default" => SrsFilter.Default(type), "broad" => SrsFilter.Broad(type),
        "two-lemmas" => SrsFilter.Broad(type) with { Name = profile, MaxRank = 2 },
        "one-lemma" => SrsFilter.OneLemma(type), "rare" => SrsFilter.Rare(type),
        "rank-window" => SrsFilter.Broad(type) with { Name = profile, MinRank = 101, MaxRank = 125 },
        "one-combo" => SrsFilter.Broad(type) with { Name = profile, Categories = [1], Numbers = [1], Persons = [1], Voices = [1],
            Feminine = [], Neuter = [] },
        _ => throw new ArgumentOutOfRangeException(nameof(profile))
    };

    static string Pattern(BundledSrsCorpus corpus, PracticeType type, long id)
    {
        if (type == PracticeType.Declension)
        {
            var noun = (Noun)corpus.Nouns.GetLemma((int)(id / 10000))!.Primary;
            return (noun.Pattern.IsBase() ? noun.Pattern : noun.Pattern.ParentBase()).ToString();
        }
        var verb = (Verb)corpus.Verbs.GetLemma((int)(id / 100000))!.Primary;
        return (verb.Pattern.IsIrregular() ? verb.Pattern.ParentRegular() : verb.Pattern).ToString();
    }

    static void WriteReport(PracticeType type, string profile, string state, SrsFilter filter,
        object inventory, DateTime[] seeds, List<IReadOnlyList<PracticeItem>> queues, List<SrsSession>? sessions = null)
    {
        var report = new
        {
            Type = type.ToString(), Profile = profile, State = state, Filter = filter,
            Corpus = profile == "restarted" ? "synthetic-20-lemmas" : "bundled",
            DictionarySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(TestPaths.PaliDbPath))),
            SchedulerSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(TestPaths.RepositoryRoot,
                "PaliPractice", "PaliPractice", "Services", "Practice", "PracticeQueueBuilder.cs")))),
            Inventory = inventory, Seeds = seeds, Queues = queues, Sessions = sessions
        };
        var directory = Environment.GetEnvironmentVariable("PALIPRACTICE_SRS_REPORT_DIR")
            ?? Path.Combine(Path.GetTempPath(), "pali-srs-reports", Environment.ProcessId.ToString());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"quality-{type}-{profile}-{state}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(report));
        TestContext.AddTestAttachment(path, "Fixed-state queue quality across 24 date seeds");
    }
}
