using FluentAssertions.Execution;
using PaliPractice.Models.Inflection;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Presentation.Settings.ViewModels;
using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;
using PaliPractice.Tests.Practice.Fakes;
using SQLite;

namespace PaliPractice.Tests.Practice;

/// <summary>
/// Contract tests for the persisted noun-pattern filters consumed by practice.
/// Uses the production user-data repository so empty, missing, and malformed
/// settings cross the same SQLite boundary as they do in the app.
/// </summary>
[TestFixture]
public class DeclensionPatternFilterContractTests
{
    static readonly DateTime TestSeedDate = new(2024, 6, 15);

    [Flags]
    public enum NounGenderSelection
    {
        None = 0,
        Masculine = 1 << 0,
        Feminine = 1 << 1,
        Neuter = 1 << 2,
        All = Masculine | Feminine | Neuter
    }

    readonly record struct GenderFixture(
        NounGenderSelection Selection,
        Gender Gender,
        NounPattern Pattern,
        string SettingKey,
        NounPattern[] Defaults);

    static readonly GenderFixture[] GenderFixtures =
    [
        new(
            NounGenderSelection.Masculine,
            Gender.Masculine,
            NounPattern.AMasc,
            SettingsKeys.NounsMascPatterns,
            SettingsKeys.NounsDefaultMascPatterns),
        new(
            NounGenderSelection.Feminine,
            Gender.Feminine,
            NounPattern.ĀFem,
            SettingsKeys.NounsFemPatterns,
            SettingsKeys.NounsDefaultFemPatterns),
        new(
            NounGenderSelection.Neuter,
            Gender.Neuter,
            NounPattern.ANeut,
            SettingsKeys.NounsNeutPatterns,
            SettingsKeys.NounsDefaultNeutPatterns)
    ];

    static IEnumerable<TestCaseData> EveryNonEmptyGenderSelection()
    {
        for (var value = 1; value <= (int)NounGenderSelection.All; value++)
        {
            var selection = (NounGenderSelection)value;
            yield return new TestCaseData(selection);
        }
    }

    static IEnumerable<TestCaseData> EveryInvalidGenderSetting()
    {
        foreach (var fixture in GenderFixtures)
        foreach (var value in new string?[] { null, "not-a-pattern-list", "   ", "0", "999999" })
            yield return new TestCaseData(fixture.Gender, value);
    }

    SQLiteConnection _connection = null!;
    UserDataRepository _userData = null!;
    FakeNounRepository _nouns = null!;
    IDatabaseService _database = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SQLiteConnection(":memory:");
        _connection.CreateTable<UserSetting>();
        _connection.CreateTable<NounsFormMastery>();

        _userData = new UserDataRepository(_connection);
        _userData.InitializeDefaultsIfNeeded();

        _userData.SetSetting(SettingsKeys.NounsLemmaMin, 1);
        _userData.SetSetting(SettingsKeys.NounsLemmaMax, GenderFixtures.Length);
        _userData.SetSetting(
            SettingsKeys.NounsCases,
            SettingsHelpers.ToCsv([Case.Nominative]));
        _userData.SetSetting(
            SettingsKeys.NounsNumbers,
            SettingsHelpers.ToCsv([Number.Singular]));

        _nouns = new FakeNounRepository();
        for (var index = 0; index < GenderFixtures.Length; index++)
        {
            var fixture = GenderFixtures[index];
            var lemmaId = 10001 + index;
            _nouns.AddLemma(FakeLemma.CreateNoun(
                lemmaId,
                $"noun_{fixture.Gender}",
                fixture.Gender,
                fixture.Pattern,
                ebtCount: GenderFixtures.Length - index));
            _nouns.AddAttestedForm(
                lemmaId,
                Case.Nominative,
                fixture.Gender,
                Number.Singular);
        }

        _database = new RepositoryBackedTestDatabaseService(
            _nouns,
            new FakeVerbRepository(),
            _userData);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [TestCaseSource(nameof(EveryNonEmptyGenderSelection))]
    public void EligibleForms_ExactlyMatchEveryValidGenderSelection(
        NounGenderSelection selection)
    {
        StoreSelection(selection);

        var expectedGenders = ExpectedGenders(selection);
        var actualFormIds = GetEligibleFormIds();
        var actualGenders = actualFormIds
            .Select(formId => Declension.ParseId(formId).Gender)
            .ToArray();

        using var assertions = new AssertionScope();
        actualFormIds.Should().HaveCount(expectedGenders.Count);
        actualGenders.Should().BeEquivalentTo(expectedGenders,
            "practice eligibility must exactly reflect all persisted gender filters");
    }

    [TestCaseSource(nameof(EveryNonEmptyGenderSelection))]
    public void ReadingEligibleForms_DoesNotMutateAnyValidGenderSelection(
        NounGenderSelection selection)
    {
        StoreSelection(selection);
        var settingsBefore = ReadPatternSettings();

        _ = GetEligibleFormIds();
        _ = GetEligibleFormIds();

        ReadPatternSettings().Should().BeEquivalentTo(settingsBefore,
            "eligibility calculation is a repeatable read and explicit empty filters are valid state");
    }

    [TestCaseSource(nameof(EveryInvalidGenderSetting))]
    public void InvalidGenderSetting_RecoversAndPersistsOnlyThatGenderDefault(
        Gender gender,
        string? value)
    {
        StoreSelection(NounGenderSelection.All);
        var fixture = FixtureFor(gender);
        var expectedSettings = ReadPatternSettings();
        expectedSettings[fixture.SettingKey] = SettingsHelpers.ToCsv(fixture.Defaults);
        CorruptGenderSetting(fixture, value);

        using var assertions = new AssertionScope();
        DecodedEligibleGenders().Should().BeEquivalentTo(
            ExpectedGenders(NounGenderSelection.All));
        ReadPatternSettings().Should().BeEquivalentTo(expectedSettings,
            "recovery must persist canonical defaults without changing valid filters");
        _ = GetEligibleFormIds();
        ReadPatternSettings().Should().BeEquivalentTo(expectedSettings);
    }

    [TestCaseSource(nameof(EveryInvalidGenderSetting))]
    public void InvalidGenderSetting_DoesNotRestoreIntentionallyDisabledOtherGenders(
        Gender gender,
        string? value)
    {
        StoreSelection(NounGenderSelection.None);
        var fixture = FixtureFor(gender);
        var expectedSettings = ReadPatternSettings();
        expectedSettings[fixture.SettingKey] = SettingsHelpers.ToCsv(fixture.Defaults);
        CorruptGenderSetting(fixture, value);

        using var assertions = new AssertionScope();
        DecodedEligibleGenders().Should().BeEquivalentTo([gender]);
        ReadPatternSettings().Should().BeEquivalentTo(expectedSettings);
    }

    [TestCaseSource(nameof(EveryInvalidGenderSetting))]
    public void InvalidGenderSetting_SettingsSavePreservesRecovery(
        Gender gender,
        string? value)
    {
        StoreSelection(NounGenderSelection.All);
        var fixture = FixtureFor(gender);
        var expectedSettings = ReadPatternSettings();
        expectedSettings[fixture.SettingKey] = SettingsHelpers.ToCsv(fixture.Defaults);
        CorruptGenderSetting(fixture, value);

        var settings = new DeclensionSettingsViewModel(null!, _database);
        settings.DailyGoal += 1;

        using var assertions = new AssertionScope();
        ReadPatternSettings().Should().BeEquivalentTo(expectedSettings,
            "an unrelated settings edit must not convert corruption into an intentional empty selection");
        DecodedEligibleGenders().Should().BeEquivalentTo(ExpectedGenders(NounGenderSelection.All));
        var reloaded = new DeclensionSettingsViewModel(null!, _database);
        reloaded.DailyGoal += 1;
        ReadPatternSettings().Should().BeEquivalentTo(expectedSettings);
    }

    [TestCaseSource(nameof(EveryNonEmptyGenderSelection))]
    public void SettingsSave_PreservesIntentionalEmptyGenders(NounGenderSelection selection)
    {
        StoreSelection(selection);
        var settingsBefore = ReadPatternSettings();

        var settings = new DeclensionSettingsViewModel(null!, _database);
        settings.DailyGoal += 1;

        using var assertions = new AssertionScope();
        DecodedEligibleGenders().Should().BeEquivalentTo(ExpectedGenders(selection));
        ReadPatternSettings().Should().BeEquivalentTo(settingsBefore);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AllGenderSettingsEmpty_RecoversBeforePracticeOrSettings(bool openSettingsFirst)
    {
        StoreSelection(NounGenderSelection.None);
        var expectedSettings = GenderFixtures.ToDictionary(
            fixture => fixture.SettingKey,
            fixture => SettingsHelpers.ToCsv(fixture.Defaults));

        if (openSettingsFirst)
        {
            var settings = new DeclensionSettingsViewModel(null!, _database);
            settings.DailyGoal += 1;
            ReadPatternSettings().Should().BeEquivalentTo(expectedSettings);
        }

        var queuedGenders = new PracticeQueueBuilder(_database)
            .BuildQueue(PracticeType.Declension, GenderFixtures.Length, TestSeedDate)
            .Select(item => Declension.ParseId(item.FormId).Gender);

        using var assertions = new AssertionScope();
        queuedGenders.Should().BeEquivalentTo(ExpectedGenders(NounGenderSelection.All));
        ReadPatternSettings().Should().BeEquivalentTo(expectedSettings);
        var reloaded = new DeclensionSettingsViewModel(null!, _database);
        reloaded.DailyGoal += 1;
        ReadPatternSettings().Should().BeEquivalentTo(expectedSettings);
    }

    [Test]
    public void SettingsRoundTrip_PreservesFullyDisabledFemininePatternsAfterPracticeRead()
    {
        var settings = new DeclensionSettingsViewModel(
            navigator: null!, // Navigation is not exercised by this settings contract test.
            db: _database);

        settings.PatternMascI = false;
        settings.PatternMascILong = false;
        settings.PatternMascU = false;
        settings.PatternMascULong = false;
        settings.PatternMascAs = false;
        settings.PatternMascAr = false;
        settings.PatternMascAnt = false;

        settings.PatternFemALong = false;
        settings.PatternFemI = false;
        settings.PatternFemILong = false;
        settings.PatternFemU = false;
        settings.PatternFemAr = false;

        settings.PatternNtI = false;
        settings.PatternNtU = false;

        _userData.GetSetting(SettingsKeys.NounsFemPatterns, "missing")
            .Should().BeEmpty("the settings view model stores an explicitly disabled gender as an empty list");

        var queuedGenders = new PracticeQueueBuilder(_database)
            .BuildQueue(
                PracticeType.Declension,
                count: GenderFixtures.Length,
                seedDate: TestSeedDate)
            .Select(item => Declension.ParseId(item.FormId).Gender)
            .ToHashSet();

        var reloaded = new DeclensionSettingsViewModel(
            navigator: null!, // Navigation is not exercised by this settings contract test.
            db: _database);

        using var assertions = new AssertionScope();
        queuedGenders.Should().BeEquivalentTo(
            [Gender.Masculine, Gender.Neuter]);
        _userData.GetSetting(SettingsKeys.NounsFemPatterns, "missing")
            .Should().BeEmpty("practice reads must not rewrite an intentional empty selection");
        FemininePatternValues(reloaded).Should().OnlyContain(enabled => !enabled,
            "reopening settings must show every feminine pattern still disabled");
    }

    void StoreSelection(NounGenderSelection selection)
    {
        foreach (var fixture in GenderFixtures)
        {
            var enabledPatterns = selection.HasFlag(fixture.Selection)
                ? SettingsHelpers.ToCsv([fixture.Pattern])
                : string.Empty;
            _userData.SetSetting(fixture.SettingKey, enabledPatterns);
        }
    }

    void CorruptGenderSetting(GenderFixture fixture, string? value)
    {
        if (value is null)
            _connection.Execute("DELETE FROM user_settings WHERE key = ?", fixture.SettingKey);
        else
            _userData.SetSetting(fixture.SettingKey, value);
    }

    List<long> GetEligibleFormIds() =>
        new PracticeQueueBuilder(_database)
            .GetEligibleFormIds(PracticeType.Declension);

    HashSet<Gender> DecodedEligibleGenders() =>
        GetEligibleFormIds()
            .Select(formId => Declension.ParseId(formId).Gender)
            .ToHashSet();

    Dictionary<string, string> ReadPatternSettings() =>
        GenderFixtures.ToDictionary(
            fixture => fixture.SettingKey,
            fixture => _userData.GetSetting(fixture.SettingKey, "missing"));

    static HashSet<Gender> ExpectedGenders(NounGenderSelection selection) =>
        GenderFixtures
            .Where(fixture => selection.HasFlag(fixture.Selection))
            .Select(fixture => fixture.Gender)
            .ToHashSet();

    static GenderFixture FixtureFor(Gender gender) =>
        GenderFixtures.Single(fixture => fixture.Gender == gender);

    static bool[] FemininePatternValues(DeclensionSettingsViewModel settings) =>
    [
        settings.PatternFemALong,
        settings.PatternFemI,
        settings.PatternFemILong,
        settings.PatternFemU,
        settings.PatternFemAr
    ];

}
