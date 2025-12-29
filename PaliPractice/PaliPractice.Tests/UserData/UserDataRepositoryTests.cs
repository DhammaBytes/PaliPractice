using PaliPractice.Models;
using PaliPractice.Models.Inflection;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;
using SQLite;

namespace PaliPractice.Tests.UserData;

/// <summary>
/// Tests for UserDataRepository type-specific methods.
/// Uses in-memory SQLite database for isolation.
/// </summary>
[TestFixture]
public class UserDataRepositoryTests
{
    SQLiteConnection _connection = null!;
    UserDataRepository _repository = null!;

    [SetUp]
    public void Setup()
    {
        _connection = new SQLiteConnection(":memory:");

        // Create tables
        _connection.CreateTable<NounsFormMastery>();
        _connection.CreateTable<VerbsFormMastery>();
        _connection.CreateTable<NounsPracticeHistory>();
        _connection.CreateTable<VerbsPracticeHistory>();
        _connection.CreateTable<NounsCombinationDifficulty>();
        _connection.CreateTable<VerbsCombinationDifficulty>();
        _connection.CreateTable<UserSetting>();
        _connection.CreateTable<DailyProgress>();

        _repository = new UserDataRepository(_connection);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    #region Noun Form Mastery Tests

    [Test]
    public void RecordNounPracticeResult_NewForm_CreatesMasteryRecord()
    {
        var formId = 123456789L;

        _repository.RecordNounPracticeResult(formId, wasEasy: true);

        var mastery = _repository.GetNounFormMastery(formId);
        mastery.Should().NotBeNull();
        mastery!.FormId.Should().Be(formId);
        mastery.MasteryLevel.Should().BeGreaterThan(CooldownCalculator.DefaultLevel);
    }

    [Test]
    public void RecordNounPracticeResult_ExistingForm_UpdatesMasteryLevel()
    {
        var formId = 123456789L;

        // First practice (easy)
        _repository.RecordNounPracticeResult(formId, wasEasy: true);
        var firstLevel = _repository.GetNounFormMastery(formId)!.MasteryLevel;

        // Second practice (hard)
        _repository.RecordNounPracticeResult(formId, wasEasy: false);
        var secondLevel = _repository.GetNounFormMastery(formId)!.MasteryLevel;

        secondLevel.Should().BeLessThan(firstLevel);
    }

    [Test]
    public void RecordNounPracticeResult_CreatesHistoryRecord()
    {
        var formId = 123456789L;

        _repository.RecordNounPracticeResult(formId, wasEasy: true);

        var history = _repository.GetRecentNounHistory(limit: 10);
        history.Should().HaveCount(1);
        history[0].FormId.Should().Be(formId);
        // FormText is resolved on load via InflectionService, not stored in DB
    }

    [Test]
    public void GetPracticedNounFormIds_ReturnsOnlyNounForms()
    {
        // Add noun mastery record
        _repository.RecordNounPracticeResult(111111111L, wasEasy: true);

        // Add verb mastery record (separate table)
        _repository.RecordVerbPracticeResult(2222222222L, wasEasy: true);

        var nounIds = _repository.GetPracticedNounFormIds();
        nounIds.Should().Contain(111111111L);
        nounIds.Should().NotContain(2222222222L);
    }

    #endregion

    #region Verb Form Mastery Tests

    [Test]
    public void RecordVerbPracticeResult_NewForm_CreatesMasteryRecord()
    {
        var formId = 1234567890L;

        _repository.RecordVerbPracticeResult(formId, wasEasy: true);

        var mastery = _repository.GetVerbFormMastery(formId);
        mastery.Should().NotBeNull();
        mastery!.FormId.Should().Be(formId);
    }

    [Test]
    public void RecordVerbPracticeResult_CreatesHistoryRecord()
    {
        var formId = 1234567890L;

        _repository.RecordVerbPracticeResult(formId, wasEasy: false);

        var history = _repository.GetRecentVerbHistory(limit: 10);
        history.Should().HaveCount(1);
        history[0].FormId.Should().Be(formId);
        // FormText is resolved on load via InflectionService, not stored in DB
    }

    [Test]
    public void GetPracticedVerbFormIds_ReturnsOnlyVerbForms()
    {
        // Add noun mastery record
        _repository.RecordNounPracticeResult(111111111L, wasEasy: true);

        // Add verb mastery record
        _repository.RecordVerbPracticeResult(2222222222L, wasEasy: true);

        var verbIds = _repository.GetPracticedVerbFormIds();
        verbIds.Should().Contain(2222222222L);
        verbIds.Should().NotContain(111111111L);
    }

    #endregion

    #region Type-Dispatching Helper Tests

    [Test]
    public void RecordPracticeResult_Declension_DispatchesToNounMethod()
    {
        var formId = 111111111L;

        _repository.RecordPracticeResult(formId, PracticeType.Declension, wasEasy: true);

        _repository.GetNounFormMastery(formId).Should().NotBeNull();
        _repository.GetVerbFormMastery(formId).Should().BeNull();
    }

    [Test]
    public void RecordPracticeResult_Conjugation_DispatchesToVerbMethod()
    {
        var formId = 2222222222L;

        _repository.RecordPracticeResult(formId, PracticeType.Conjugation, wasEasy: true);

        _repository.GetVerbFormMastery(formId).Should().NotBeNull();
        _repository.GetNounFormMastery(formId).Should().BeNull();
    }

    [Test]
    public void GetRecentHistory_Declension_ReturnsNounHistory()
    {
        _repository.RecordNounPracticeResult(111111111L, wasEasy: true);
        _repository.RecordVerbPracticeResult(2222222222L, wasEasy: true);

        var history = _repository.GetRecentHistory(PracticeType.Declension, limit: 10);

        history.Should().HaveCount(1);
        history[0].FormId.Should().Be(111111111L);
        // FormText is resolved on load via InflectionService, not stored in DB
    }

    [Test]
    public void GetRecentHistory_Conjugation_ReturnsVerbHistory()
    {
        _repository.RecordNounPracticeResult(111111111L, wasEasy: true);
        _repository.RecordVerbPracticeResult(2222222222L, wasEasy: true);

        var history = _repository.GetRecentHistory(PracticeType.Conjugation, limit: 10);

        history.Should().HaveCount(1);
        history[0].FormId.Should().Be(2222222222L);
        // FormText is resolved on load via InflectionService, not stored in DB
    }

    #endregion

    #region Combination Difficulty Tests

    [Test]
    public void UpdateDeclensionDifficulty_NewCombo_CreatesRecord()
    {
        _repository.UpdateDeclensionDifficulty(Case.Genitive, Gender.Masculine, Number.Singular, wasHard: true);

        var difficulty = _repository.GetDeclensionDifficulty(Case.Genitive, Gender.Masculine, Number.Singular);
        difficulty.Should().NotBeNull();
        difficulty!.DifficultyScore.Should().BeGreaterThan(0.5);
        difficulty.TotalAttempts.Should().Be(1);
    }

    [Test]
    public void UpdateDeclensionDifficulty_ExistingCombo_UpdatesScore()
    {
        // First: hard
        _repository.UpdateDeclensionDifficulty(Case.Genitive, Gender.Masculine, Number.Singular, wasHard: true);
        var firstScore = _repository.GetDeclensionDifficulty(Case.Genitive, Gender.Masculine, Number.Singular)!.DifficultyScore;

        // Second: easy (should lower score)
        _repository.UpdateDeclensionDifficulty(Case.Genitive, Gender.Masculine, Number.Singular, wasHard: false);
        var secondScore = _repository.GetDeclensionDifficulty(Case.Genitive, Gender.Masculine, Number.Singular)!.DifficultyScore;

        secondScore.Should().BeLessThan(firstScore);
    }

    [Test]
    public void UpdateConjugationDifficulty_NewCombo_CreatesRecord()
    {
        _repository.UpdateConjugationDifficulty(Tense.Present, Person.Third, Number.Singular, reflexive: false, wasHard: true);

        var difficulty = _repository.GetConjugationDifficulty(Tense.Present, Person.Third, Number.Singular, reflexive: false);
        difficulty.Should().NotBeNull();
        difficulty!.TotalAttempts.Should().Be(1);
    }

    [Test]
    public void GetHardestNounCombinations_ReturnsOrderedByDifficulty()
    {
        // Add two combos with different difficulties
        _repository.UpdateDeclensionDifficulty(Case.Genitive, Gender.Masculine, Number.Singular, wasHard: true);
        _repository.UpdateDeclensionDifficulty(Case.Nominative, Gender.Masculine, Number.Singular, wasHard: false);

        var hardest = _repository.GetHardestNounCombinations(limit: 10);

        hardest.Should().HaveCount(2);
        hardest[0].DifficultyScore.Should().BeGreaterThan(hardest[1].DifficultyScore);
    }

    [Test]
    public void GetHardestVerbCombinations_ReturnsOrderedByDifficulty()
    {
        // Add two combos with different difficulties
        _repository.UpdateConjugationDifficulty(Tense.Present, Person.Third, Number.Singular, reflexive: false, wasHard: true);
        _repository.UpdateConjugationDifficulty(Tense.Present, Person.First, Number.Singular, reflexive: false, wasHard: false);

        var hardest = _repository.GetHardestVerbCombinations(limit: 10);

        hardest.Should().HaveCount(2);
        hardest[0].DifficultyScore.Should().BeGreaterThan(hardest[1].DifficultyScore);
    }

    #endregion

    #region IPracticeHistory Interface Tests

    [Test]
    public void NounsPracticeHistory_ImplementsIPracticeHistory()
    {
        _repository.RecordNounPracticeResult(111111111L, wasEasy: true);

        var history = _repository.GetRecentHistory(PracticeType.Declension, limit: 10);
        var record = history[0];

        // Verify interface properties (FormText resolved on load, not stored)
        record.FormId.Should().Be(111111111L);
        record.IsImproved.Should().BeTrue(); // Easy increases level
        record.NewLevelPercent.Should().BeGreaterThan(0);
    }

    [Test]
    public void VerbsPracticeHistory_ImplementsIPracticeHistory()
    {
        _repository.RecordVerbPracticeResult(2222222222L, wasEasy: false);

        var history = _repository.GetRecentHistory(PracticeType.Conjugation, limit: 10);
        var record = history[0];

        // Verify interface properties (FormText resolved on load, not stored)
        record.FormId.Should().Be(2222222222L);
        record.IsImproved.Should().BeFalse(); // Hard decreases level
    }

    #endregion

    #region Table Isolation Tests

    [Test]
    public void NounAndVerbTables_AreCompletleyIsolated()
    {
        var nounFormId = 111111111L;
        var verbFormId = 111111111L; // Same ID, different tables

        _repository.RecordNounPracticeResult(nounFormId, wasEasy: true);
        _repository.RecordVerbPracticeResult(verbFormId, wasEasy: false);

        // Each should have their own record
        var nounMastery = _repository.GetNounFormMastery(nounFormId);
        var verbMastery = _repository.GetVerbFormMastery(verbFormId);

        nounMastery.Should().NotBeNull();
        verbMastery.Should().NotBeNull();

        // They should have different levels (easy vs hard)
        nounMastery!.MasteryLevel.Should().NotBe(verbMastery!.MasteryLevel);

        // Histories should be in separate tables
        _repository.GetRecentNounHistory(limit: 10).Should().HaveCount(1);
        _repository.GetRecentVerbHistory(limit: 10).Should().HaveCount(1);
    }

    #endregion
}
