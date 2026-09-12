using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Path = System.IO.Path;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData.Entities;

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
    public Task Timeline(PracticeType type, string name, int seed) =>
        RunTimeline(type, name, seed, SrsStudentProfile.Legacy);

    [Test]
    public Task ExtendedTimeline(
        [Values(PracticeType.Declension, PracticeType.Conjugation)] PracticeType type,
        [Values(SrsStudentProfile.AlwaysEasy, SrsStudentProfile.WeakPlural)] SrsStudentProfile profile,
        [Values(17, 83)] int seed) => RunTimeline(type, "daily", seed, profile, 365);

    static async Task RunTimeline(PracticeType type, string name, int seed, SrsStudentProfile profile, int? days = null)
    {
        using var corpus = new BundledSrsCorpus();
        var start = SrsSimulationTests.Start.AddDays(seed);
        using var sim = new SrsSimulation(corpus.Nouns, corpus.Verbs, new(start));
        var waiting = new Dictionary<long, int>();
        var points = new List<TimelinePoint>();
        var priorEligible = new HashSet<long>();
        var previousEnd = start.UtcDateTime;
        var debt = new double[5];
        var priorDue = new HashSet<long>();
        var answer = SrsStudent.Answers(profile, type, name, seed);
        foreach (var step in Steps(type, name, seed, days))
        {
            sim.Clock.UtcNow = start.AddDays(step.Day);
            var now = sim.Clock.UtcNow.UtcDateTime;
            // Exact debt between sessions while the prior filter remains active.
            SrsBacklog.AccumulateDebt(debt, sim.AllMastery(type), priorEligible, previousEnd, now);
            var masteryBefore = MasteryFingerprint(sim, type);
            var answersBefore = sim.UserData.GetPracticeCount(type);
            step.Filter.Apply(sim.UserData, type);
            var eligible = corpus.Eligible(type, step.Filter);
            if (name == "one-lemma")
                Assert.That(eligible.Select(id => id / (type == PracticeType.Declension ? 10000 : 100000)).Distinct().Count(), Is.EqualTo(1));
            Assert.That(sim.EligibleForms(type), Is.EquivalentTo(eligible), step.Filter.Name);
            Assert.That(MasteryFingerprint(sim, type), Is.EqualTo(masteryBefore), "Filters must not rewrite mastery");
            Assert.That(sim.UserData.GetPracticeCount(type), Is.EqualTo(answersBefore));
            var due = Due(sim, type, eligible);
            var session = await sim.RunSession(type, step.Goal, step.Budget, eligible, answer);
            var served = session.Answers.Select(a => a.FormId).ToHashSet();
            var stillWaiting = due.Where(m => !served.Contains(m.FormId)).Select(m => m.FormId).ToHashSet();
            foreach (var id in waiting.Keys.Except(stillWaiting).ToArray()) waiting.Remove(id);
            foreach (var id in stillWaiting) waiting[id] = waiting.GetValueOrDefault(id) + 1;
            var maxOverdue = due.Count == 0 ? 0 : due.Max(m => (now - m.NextDueUtc).TotalDays);
            var dueAfter = Due(sim, type, eligible);
            var buckets = SrsBacklog.Capture(due, dueAfter, priorDue, session.Answers, waiting, debt, now);
            Assert.That(buckets.Sum(b => b.DueBefore), Is.EqualTo(session.DueBefore));
            Assert.That(buckets.Sum(b => b.DueAfter), Is.EqualTo(session.DueAfter));
            Assert.That(buckets.Sum(b => b.Served), Is.EqualTo(session.Answers.Count(a => a.Source == PracticeItemSource.DueForReview)));
            points.Add(new TimelinePoint(step.Day, step.Filter, step.Budget, maxOverdue,
                waiting.Values.DefaultIfEmpty().Max(), session, buckets));
            priorEligible = eligible;
            priorDue = dueAfter.Select(m => m.FormId).ToHashSet();
            previousEnd = sim.Clock.UtcNow.UtcDateTime;
        }
        WriteReport(type, name, seed, profile, points, debt.Sum());
    }

    static List<FormMasteryBase> Due(SrsSimulation sim, PracticeType type, IReadOnlySet<long> eligible) =>
        sim.AllMastery(type).Where(m => eligible.Contains(m.FormId) && m.MasteryLevel is >= 1 and <= 10 &&
            m.NextDueUtc <= sim.Clock.UtcNow.UtcDateTime).ToList();

    static string MasteryFingerprint(SrsSimulation sim, PracticeType type) => JsonSerializer.Serialize(
        sim.AllMastery(type).Select(m => new { m.FormId, m.MasteryLevel, m.PreviousLevel, m.LastPracticedUtc }));

    static IEnumerable<Step> Steps(PracticeType type, string name, int seed, int? days)
    {
        var attendance = new Random(seed); // Separate from the date-seeded production queue.
        var total = days ?? (name switch { "daily" or "short" => 90, "weekly-small" => 26, "long-return" => 6, _ => 30 });
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

    static void WriteReport(PracticeType type, string name, int seed, SrsStudentProfile profile,
        List<TimelinePoint> points, double dueCardDays)
    {
        var trace = JsonSerializer.Serialize(points);
        var answers = points.SelectMany(p => p.Session.Answers).ToList();
        var divisor = type == PracticeType.Declension ? 10000 : 100000;
        var repeats = points.Sum(p => p.Session.Answers.Zip(p.Session.Answers.Skip(1))
            .Count(pair => pair.First.FormId / divisor == pair.Second.FormId / divisor));
        var report = new
        {
            Scenario = name, Type = type.ToString(), Seed = seed,
            ReportVersion = 2, LearnerProfile = profile.ToString(),
            SchedulerSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(TestPaths.RepositoryRoot,
                "PaliPractice", "PaliPractice", "Services", "Practice", "PracticeQueueBuilder.cs")))),
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
        var suffix = profile == SrsStudentProfile.Legacy ? "" : $"-{profile}-{points.Count}days";
        var path = Path.Combine(directory, $"{type}-{name}-{seed}{suffix}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(path, "Deterministic SRS timeline and unselected backlog");
        TestContext.Out.WriteLine($"{type}/{name}/{seed}: {answers.Count} answers; pools {string.Join(',', points.Select(p => p.Session.EligibleCards).Distinct())}; report {path}");
    }

    sealed record Step(int Day, SrsFilter Filter, int Goal, int Budget);
    sealed record TimelinePoint(int Day, SrsFilter Filter, int Budget, double MaxOverdueDays,
        int MaxEligibleVisitsSkipped, SrsSession Session, SrsBucketBacklog[] Buckets);
}
