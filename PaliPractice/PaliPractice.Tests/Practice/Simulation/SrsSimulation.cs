using PaliPractice.Models.Words;
using PaliPractice.Presentation.Practice.Providers;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Grammar;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;
using PaliPractice.Tests.Practice.Fakes;
using SQLite;

namespace PaliPractice.Tests.Practice.Simulation;

internal sealed record SrsAnswer(long FormId, PracticeItemSource Source, int BeforeLevel,
    int AfterLevel, DateTime AtUtc, string FormText);

internal sealed record SrsSession(DateTime StartedUtc, int Goal, int EligibleCards, int DueBefore,
    int DueAfter, int QueueBuilds, long? PendingFormId, IReadOnlyList<SrsAnswer> Answers);

/// <summary>
/// Drives the same load / record / progress / advance sequence as practice.
/// Callers supply expected eligibility independently of the queue and advance the
/// clock between sessions. Only initial conditions may be seeded directly.
/// </summary>
internal sealed class SrsSimulation : IDisposable
{
    readonly SQLiteConnection _connection;
    readonly RepositoryBackedTestDatabaseService _database;
    readonly PracticeQueueBuilder _queue;
    readonly InflectionService _inflection;

    public SimulationTimeProvider Clock { get; }
    public UserDataRepository UserData { get; }

    public SrsSimulation(INounRepository nouns, IVerbRepository verbs,
        SimulationTimeProvider clock, string userDatabasePath = ":memory:")
    {
        Clock = clock;
        _connection = new SQLiteConnection(userDatabasePath);
        try
        {
            PracticeDatabaseMigrations.Apply(_connection);
            UserData = new UserDataRepository(_connection, clock);
            UserData.InitializeDefaultsIfNeeded();
            _database = new RepositoryBackedTestDatabaseService(nouns, verbs, UserData);
            _queue = new PracticeQueueBuilder(_database, clock);
            _inflection = new InflectionService(_database);
        }
        catch
        {
            _connection.Dispose();
            throw;
        }
    }

    public async Task<SrsSession> RunSession(PracticeType type, int goal, int completedCards,
        IReadOnlySet<long> eligible, Func<PracticeItem, int, bool> answer)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(completedCards);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(goal);
        UserData.SetSetting(type == PracticeType.Declension ? SettingsKeys.NounsDailyGoal : SettingsKeys.VerbsDailyGoal, goal);
        var started = Clock.GetUtcNow().UtcDateTime;
        var dueBefore = DueCount(type, eligible);
        IPracticeProvider provider = type == PracticeType.Declension
            ? new DeclensionPracticeProvider(_queue, _database)
            : new ConjugationPracticeProvider(_queue, _database);
        await provider.LoadAsync();
        var answers = new List<SrsAnswer>();
        var seenInQueue = new HashSet<long>();
        var builds = 1;
        var available = provider.Current != null;

        for (var index = 0; available && index < completedCards; index++)
        {
            var item = provider.Current!;
            CheckSelection(item, eligible, seenInQueue);
            var snapshot = CaptureAnswer(provider, type);
            var wasEasy = answer(item, index);
            Clock.UtcNow += TimeSpan.FromSeconds(20);
            UserData.RecordPracticeResult(item.FormId, type, wasEasy, snapshot);
            UserData.IncrementProgress(type);
            var saved = Mastery(type, item.FormId)!;
            answers.Add(new SrsAnswer(item.FormId, item.Source, saved.PreviousLevel,
                saved.MasteryLevel, saved.LastPracticedUtc, snapshot.FormText));

            if (!provider.HasNext)
            {
                builds++;
                seenInQueue.Clear();
            }
            available = provider.MoveNext();
        }

        return new SrsSession(started, goal, eligible.Count, dueBefore, DueCount(type, eligible),
            builds, available ? provider.Current?.FormId : null, answers);
    }

    void CheckSelection(PracticeItem item, IReadOnlySet<long> eligible, HashSet<long> seen)
    {
        var location = $"{item.Type} at {Clock.UtcNow:O}, form {item.FormId}";
        Assert.That(eligible, Does.Contain(item.FormId), location);
        Assert.That(seen.Add(item.FormId), Is.True, $"Duplicate within queue: {location}");
        var mastery = Mastery(item.Type, item.FormId);
        if (item.Source == PracticeItemSource.NewForm)
        {
            Assert.That(mastery, Is.Null, $"Previously practiced new card: {location}");
            return;
        }
        Assert.That(item.Source, Is.EqualTo(PracticeItemSource.DueForReview), location);
        Assert.That(mastery, Is.Not.Null, location);
        Assert.That(mastery!.MasteryLevel, Is.InRange(1, 10), location);
        Assert.That(mastery.NextDueUtc, Is.LessThanOrEqualTo(Clock.GetUtcNow().UtcDateTime), location);
    }

    PracticeSnapshot CaptureAnswer(IPracticeProvider provider, PracticeType type)
    {
        var lemma = provider.GetCurrentLemma()!;
        string? form;
        long formId;
        if (type == PracticeType.Declension)
        {
            var (nounCase, _, number) = ((Case, Gender, Number))provider.GetCurrentParameters();
            var declension = _inflection.GenerateNounForms((Noun)lemma.Primary, nounCase, number);
            form = declension.Primary?.Form;
            formId = declension.FormId;
        }
        else
        {
            var (tense, person, number, voice) = ((Tense, Person, Number, Voice))provider.GetCurrentParameters();
            var conjugation = _inflection.GenerateVerbForms((Verb)lemma.Primary, person, number, tense, voice == Voice.Reflexive);
            form = conjugation.Primary?.Form;
            formId = conjugation.FormId;
        }
        Assert.That(formId, Is.EqualTo(provider.Current!.FormId));
        Assert.That(form, Is.Not.Null.And.Not.Empty, "Selected card must have an attested answer");
        return PracticeSnapshot.Capture(formId, type, form!, lemma.BaseForm);
    }

    public FormMasteryBase? Mastery(PracticeType type, long formId) => type == PracticeType.Declension
        ? UserData.GetNounFormMastery(formId) : UserData.GetVerbFormMastery(formId);

    public HashSet<long> EligibleForms(PracticeType type) => _queue.GetEligibleFormIds(type).ToHashSet();

    public IReadOnlyList<PracticeItem> BuildQueue(PracticeType type, int count, DateTime seedDate) =>
        _queue.BuildQueue(type, count, seedDate);

    public IReadOnlyList<FormMasteryBase> AllMastery(PracticeType type) => type == PracticeType.Declension
        ? _connection.Table<NounsFormMastery>().OrderBy(f => f.FormId).ToList()
        : _connection.Table<VerbsFormMastery>().OrderBy(f => f.FormId).ToList();

    int DueCount(PracticeType type, IReadOnlySet<long> eligible) => AllMastery(type)
        .Count(f => eligible.Contains(f.FormId) && f.MasteryLevel is >= 1 and <= 10 && f.NextDueUtc <= Clock.GetUtcNow().UtcDateTime);

    public void SeedMastery(PracticeType type, long formId, int level, DateTime practicedUtc)
    {
        FormMasteryBase record = type == PracticeType.Declension ? new NounsFormMastery() : new VerbsFormMastery();
        record.FormId = formId;
        record.MasteryLevel = level;
        record.LastPracticedUtc = practicedUtc;
        _connection.Insert(record);
    }

    public void Dispose() => _connection.Dispose();
}
