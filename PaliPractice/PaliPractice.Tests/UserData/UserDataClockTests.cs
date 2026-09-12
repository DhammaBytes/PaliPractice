using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;
using PaliPractice.Tests.Practice.Simulation;
using SQLite;

namespace PaliPractice.Tests.UserData;

[TestFixture]
public class UserDataClockTests
{
    SQLiteConnection _connection = null!;
    SimulationTimeProvider _clock = null!;
    UserDataRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SQLiteConnection(":memory:");
        PracticeDatabaseMigrations.Apply(_connection);
        _clock = new SimulationTimeProvider(new DateTimeOffset(2035, 1, 2, 12, 0, 0, TimeSpan.Zero));
        _repository = new UserDataRepository(_connection, _clock);
    }

    [TearDown]
    public void TearDown() => _connection.Dispose();

    [Test]
    public void DueQueries_IncludeExactlyTheCooldownBoundary(
        [Values(PracticeType.Declension, PracticeType.Conjugation)] PracticeType type,
        [Range(1, 10)] int level)
    {
        var practicedUtc = _clock.UtcNow.UtcDateTime;
        var formId = FormId(type);
        FormMasteryBase mastery = type == PracticeType.Declension
            ? new NounsFormMastery() : new VerbsFormMastery();
        mastery.FormId = formId;
        mastery.MasteryLevel = level;
        mastery.LastPracticedUtc = practicedUtc;
        _connection.Insert(mastery);
        var dueUtc = CooldownCalculator.CalculateNextDue(practicedUtc, level);

        _clock.UtcNow = new DateTimeOffset(dueUtc.AddTicks(-1));
        DueIds(type).Should().BeEmpty();
        _clock.UtcNow = new DateTimeOffset(dueUtc);
        DueIds(type).Should().Equal(formId);
        _clock.UtcNow = new DateTimeOffset(dueUtc.AddTicks(1));
        DueIds(type).Should().Equal(formId);
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public void Answers_UseClockAndAppendHistoryWithoutRewritingEarlierAnswers(PracticeType type)
    {
        var formId = FormId(type);
        var firstAt = _clock.UtcNow.UtcDateTime;
        var snapshot = new PracticeSnapshot("first answer", "lemma", "grammar");
        _repository.RecordPracticeResult(formId, type, wasEasy: true, snapshot);
        var first = _repository.GetRecentHistory(type).Single();
        first.PracticedUtc.Should().Be(firstAt);
        first.OldLevel.Should().Be(4);
        first.NewLevel.Should().Be(5);
        var captured = (PracticeHistoryBase)first;
        captured.FormText.Should().Be(snapshot.FormText);
        captured.LemmaText.Should().Be(snapshot.LemmaText);
        captured.GrammarText.Should().Be(snapshot.GrammarText);
        captured.SnapshotOrigin.Should().Be(HistorySnapshotOrigin.Practiced);

        _clock.UtcNow += TimeSpan.FromDays(30);
        _repository = new UserDataRepository(_connection, _clock);
        _repository.RecordPracticeResult(formId, type, wasEasy: false);

        var history = _repository.GetRecentHistory(type);
        history.Should().HaveCount(2);
        history[0].PracticedUtc.Should().Be(_clock.UtcNow.UtcDateTime);
        history[0].OldLevel.Should().Be(5);
        history[0].NewLevel.Should().Be(4);
        history[1].Should().BeEquivalentTo(first);
        FormMasteryBase? mastery = type == PracticeType.Declension
            ? _repository.GetNounFormMastery(formId) : _repository.GetVerbFormMastery(formId);
        mastery!.LastPracticedUtc.Should().Be(history[0].PracticedUtc);
        mastery.MasteryLevel.Should().Be(4);
        mastery.PreviousLevel.Should().Be(5);
    }

    [TestCase(PracticeType.Declension)]
    [TestCase(PracticeType.Conjugation)]
    public void TodayHistory_UsesUtcDayFromClock(PracticeType type)
    {
        _clock.UtcNow = new DateTimeOffset(2035, 1, 2, 23, 59, 59, TimeSpan.Zero);
        _repository.RecordPracticeResult(FormId(type), type, wasEasy: true);
        _clock.UtcNow += TimeSpan.FromSeconds(1);
        _repository.RecordPracticeResult(FormId(type), type, wasEasy: false);

        IReadOnlyList<PracticeHistoryBase> today = type == PracticeType.Declension
            ? _repository.GetTodayNounHistory() : _repository.GetTodayVerbHistory();
        today.Should().ContainSingle().Which.PracticedUtc.Should().Be(_clock.UtcNow.UtcDateTime);
        _repository.GetRecentHistory(type).Should().HaveCount(2);
    }

    [TestCase(-4)]
    [TestCase(9)]
    public void DailyProgress_ResetsAtFiveLocalAndPreservesPreviousDay(int offsetHours)
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Simulation", TimeSpan.FromHours(offsetHours), "Simulation", "Simulation");
        _clock = new SimulationTimeProvider(
            new DateTimeOffset(2035, 1, 2, 4, 59, 59, TimeSpan.FromHours(offsetHours)).ToUniversalTime(), zone);
        _repository = new UserDataRepository(_connection, _clock);
        _repository.IncrementProgress(PracticeType.Declension);
        _repository.IncrementProgress(PracticeType.Conjugation);
        _repository.GetTodayProgress().Date.Should().Be(20350101);

        _clock.UtcNow += TimeSpan.FromSeconds(1);
        var today = _repository.GetTodayProgress();
        today.Date.Should().Be(20350102);
        today.DeclensionsCompleted.Should().Be(0);
        today.ConjugationsCompleted.Should().Be(0);
        _repository.IncrementProgress(PracticeType.Conjugation);
        var yesterday = _connection.Find<DailyProgress>(20350101);
        yesterday.DeclensionsCompleted.Should().Be(1);
        yesterday.ConjugationsCompleted.Should().Be(1);
        _repository.GetTodayProgress().ConjugationsCompleted.Should().Be(1);
    }

    [Test]
    public void Settings_InsertAndUpdateUseClock()
    {
        _repository.SetSetting(SettingsKeys.NounsDailyGoal, 25);
        var first = _connection.Find<UserSetting>(SettingsKeys.NounsDailyGoal);
        first.UpdatedUtc.Should().Be(_clock.UtcNow.UtcDateTime);
        _clock.UtcNow += TimeSpan.FromDays(1);
        _repository.SetSetting(SettingsKeys.NounsDailyGoal, 50);
        _connection.Find<UserSetting>(SettingsKeys.NounsDailyGoal).UpdatedUtc.Should().Be(_clock.UtcNow.UtcDateTime);
    }

    IReadOnlyList<long> DueIds(PracticeType type) => type == PracticeType.Declension
        ? _repository.GetDueNounForms().Select(f => f.FormId).ToList()
        : _repository.GetDueVerbForms().Select(f => f.FormId).ToList();

    static long FormId(PracticeType type) => type == PracticeType.Declension ? 100011110L : 7000111110L;
}
