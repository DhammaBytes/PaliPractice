using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Path = System.IO.Path;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Practice;

namespace PaliPractice.Tests.Practice.Simulation;

[TestFixture]
public class SrsTimelineTests
{
    static IEnumerable<TestCaseData> Scenarios()
    {
        foreach (var type in new[] { PracticeType.Declension, PracticeType.Conjugation })
        foreach (var name in new[] { "daily", "short", "weekly-small", "changing-filters", "long-return", "one-lemma" })
        foreach (var seed in new[] { 17, 83 })
            yield return new TestCaseData(type, name, seed);
    }

    [TestCaseSource(nameof(Scenarios))]
    public async Task Timeline(PracticeType type, string name, int seed)
    {
        using var corpus = new BundledSrsCorpus();
        var start = SrsSimulationTests.Start.AddDays(seed);
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(start));
        var waiting = new Dictionary<long, int>();
        var points = new List<TimelinePoint>();
        var priorEligible = new HashSet<long>();
        var previousEnd = start.UtcDateTime;
        double dueCardDays = 0;
        foreach (var step in Steps(type, name, seed))
        {
            sim.Clock.UtcNow = start.AddDays(step.Day);
            var now = sim.Clock.UtcNow.UtcDateTime;
            // Exact debt between sessions while the prior filter remains active.
            dueCardDays += sim.AllMastery(type).Where(m => priorEligible.Contains(m.FormId) && m.MasteryLevel is >= 1 and <= 10)
                .Sum(m => Math.Max(0, (now - (m.NextDueUtc > previousEnd ? m.NextDueUtc : previousEnd)).TotalDays));
            var masteryBefore = MasteryFingerprint(sim, type);
            var answersBefore = sim.UserData.GetPracticeCount(type);
            step.Filter.Apply(sim.UserData, type);
            var eligible = corpus.Eligible(type, step.Filter);
            if (name == "one-lemma")
                Assert.That(eligible.Select(id => id / (type == PracticeType.Declension ? 10000 : 100000)).Distinct().Count(), Is.EqualTo(1));
            Assert.That(sim.EligibleForms(type), Is.EquivalentTo(eligible), step.Filter.Name);
            Assert.That(MasteryFingerprint(sim, type), Is.EqualTo(masteryBefore), "Filters must not rewrite mastery");
            Assert.That(sim.UserData.GetPracticeCount(type), Is.EqualTo(answersBefore));
            var due = sim.AllMastery(type).Where(m => eligible.Contains(m.FormId) && m.MasteryLevel is >= 1 and <= 10 && m.NextDueUtc <= now).ToList();
            var session = await sim.RunSession(type, step.Goal, step.Budget, eligible,
                (item, _) => name == "daily" || (name != "short" && (item.FormId / 10 + seed) % 4 != 0));
            var served = session.Answers.Select(a => a.FormId).ToHashSet();
            var stillWaiting = due.Where(m => !served.Contains(m.FormId)).Select(m => m.FormId).ToHashSet();
            foreach (var id in waiting.Keys.Except(stillWaiting).ToArray()) waiting.Remove(id);
            foreach (var id in stillWaiting) waiting[id] = waiting.GetValueOrDefault(id) + 1;
            var maxOverdue = due.Count == 0 ? 0 : due.Max(m => (now - m.NextDueUtc).TotalDays);
            points.Add(new TimelinePoint(step.Day, step.Filter, step.Budget, maxOverdue,
                waiting.Values.DefaultIfEmpty().Max(), session));
            priorEligible = eligible;
            previousEnd = sim.Clock.UtcNow.UtcDateTime;
        }
        WriteReport(type, name, seed, points, dueCardDays);
    }

    static string MasteryFingerprint(SrsSimulation sim, PracticeType type) => JsonSerializer.Serialize(
        sim.AllMastery(type).Select(m => new { m.FormId, m.MasteryLevel, m.PreviousLevel, m.LastPracticedUtc }));

    static IEnumerable<Step> Steps(PracticeType type, string name, int seed)
    {
        var attendance = new Random(seed); // Separate from the date-seeded production queue.
        var total = name switch { "daily" or "short" => 90, "weekly-small" => 26, "long-return" => 6, _ => 30 };
        int day = 0;
        for (int visit = 0; visit < total; visit++)
        {
            var filter = SelectFilter(type, name, visit);
            var goal = name == "one-lemma" ? 5 : 50;
            var budget = name switch { "short" => 1, "weekly-small" => 10, "one-lemma" => 20,
                "changing-filters" => new[] { 3, 25, 80 }[visit % 3], _ => 50 };
            if (name == "long-return") day = new[] { 0, 1, 7, 30, 90, 365 }[visit];
            yield return new Step(day, filter, goal, budget);
            day += name switch { "weekly-small" => 7, "changing-filters" => attendance.Next(1, 15), _ => 1 };
        }
    }

    static SrsFilter SelectFilter(PracticeType type, string name, int visit)
    {
        var broad = SrsFilter.Broad(type);
        return name switch
        {
            "short" => SrsFilter.Default(type),
            "weekly-small" => broad with { Name = "two-lemmas", MaxRank = 2 },
            "one-lemma" => SrsFilter.OneLemma(type),
            "changing-filters" => (visit / 5) switch
            {
                0 or 5 => SrsFilter.Default(type), 1 or 4 => broad,
                2 => SrsFilter.Rare(type),
                _ => broad with { Name = "rank-window", MinRank = 101, MaxRank = 125 }
            },
            "long-return" when visit is 2 or 3 => SrsFilter.Rare(type),
            _ => broad
        };
    }

    static void WriteReport(PracticeType type, string name, int seed, List<TimelinePoint> points, double dueCardDays)
    {
        var trace = JsonSerializer.Serialize(points);
        var answers = points.SelectMany(p => p.Session.Answers).ToList();
        var divisor = type == PracticeType.Declension ? 10000 : 100000;
        var repeats = points.Sum(p => p.Session.Answers.Zip(p.Session.Answers.Skip(1))
            .Count(pair => pair.First.FormId / divisor == pair.Second.FormId / divisor));
        var report = new
        {
            Scenario = name, Type = type.ToString(), Seed = seed,
            DictionarySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(TestPaths.PaliDbPath))),
            TraceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trace))),
            Summary = new { Sessions = points.Count, Answers = answers.Count,
                UniqueForms = answers.Select(a => a.FormId).Distinct().Count(),
                NewAnswers = answers.Count(a => a.Source == PracticeItemSource.NewForm),
                ReviewAnswers = answers.Count(a => a.Source == PracticeItemSource.DueForReview),
                FillRate = (double)answers.Count / points.Sum(p => p.Budget),
                Rebuilds = points.Sum(p => p.Session.QueueBuilds - 1),
                WithinSessionLemmaRepeats = repeats, BetweenSessionDueCardDays = dueCardDays,
                MaxEligibleVisitsSkipped = points.Max(p => p.MaxEligibleVisitsSkipped),
                MaxOverdueDays = points.Max(p => p.MaxOverdueDays),
                ExposureByCaseOrTense = answers.GroupBy(a => a.FormId % divisor / (divisor / 10))
                    .OrderBy(g => g.Key).ToDictionary(g => g.Key, g => g.Count()) },
            Sessions = points
        };
        var directory = Environment.GetEnvironmentVariable("PALIPRACTICE_SRS_REPORT_DIR")
            ?? Path.Combine(Path.GetTempPath(), "pali-srs-reports", Environment.ProcessId.ToString());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{type}-{name}-{seed}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(path, "Deterministic SRS timeline and unselected backlog");
        TestContext.Out.WriteLine($"{type}/{name}/{seed}: {answers.Count} answers; pools {string.Join(',', points.Select(p => p.Session.EligibleCards).Distinct())}; report {path}");
    }

    sealed record Step(int Day, SrsFilter Filter, int Goal, int Budget);
    sealed record TimelinePoint(int Day, SrsFilter Filter, int Budget, double MaxOverdueDays,
        int MaxEligibleVisitsSkipped, SrsSession Session);
}
