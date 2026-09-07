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
/// Contract tests for persisted verb filters consumed by practice.
/// Uses the production user-data repository with a deterministic in-memory
/// grammar corpus so storage, recovery, and filtering are tested together.
/// </summary>
[TestFixture]
public class ConjugationFilterContractTests
{
    public enum FilterAxis
    {
        Patterns,
        Tenses,
        Persons,
        Numbers,
        Voices
    }

    public enum InvalidSettingState
    {
        Empty,
        Missing,
        Malformed,
        Whitespace,
        None,
        Undefined
    }

    public sealed record VerbFilterCase(
        string Name,
        IReadOnlyList<VerbPattern> Patterns,
        IReadOnlyList<Tense> Tenses,
        IReadOnlyList<Person> Persons,
        IReadOnlyList<Number> Numbers,
        IReadOnlyList<Voice> Voices)
    {
        public override string ToString() => Name;
    }

    readonly record struct VerbFixture(int LemmaId, VerbPattern Pattern);

    static readonly VerbPattern[] AllPatterns =
    [
        VerbPattern.Ati,
        VerbPattern.Āti,
        VerbPattern.Eti,
        VerbPattern.Oti
    ];

    static readonly Tense[] AllTenses =
    [
        Tense.Present,
        Tense.Imperative,
        Tense.Optative,
        Tense.Future
    ];

    static readonly Person[] AllPersons =
    [
        Person.First,
        Person.Second,
        Person.Third
    ];

    static readonly Number[] AllNumbers =
    [
        Number.Singular,
        Number.Plural
    ];

    static readonly Voice[] AllVoices =
    [
        Voice.Active,
        Voice.Reflexive
    ];

    static readonly VerbFixture[] VerbFixtures = AllPatterns
        .Select((pattern, index) => new VerbFixture(70001 + index, pattern))
        .ToArray();

    static readonly string[] FilterSettingKeys =
    [
        SettingsKeys.VerbsPatterns,
        SettingsKeys.VerbsTenses,
        SettingsKeys.VerbsPersons,
        SettingsKeys.VerbsNumbers,
        SettingsKeys.VerbsVoices
    ];

    static IEnumerable<TestCaseData> GeneratedValidFilterCases()
    {
        // Exhaust every atomic combination except the four invalid citation-only selections.
        foreach (var pattern in AllPatterns)
        foreach (var tense in AllTenses)
        foreach (var person in AllPersons)
        foreach (var number in AllNumbers)
        foreach (var voice in AllVoices)
        {
            if (tense == Tense.Present && person == Person.Third &&
                number == Number.Singular && voice == Voice.Active)
                continue; // Recovery for this combination is tested separately.

            yield return new TestCaseData(Filters(
                $"Atomic_{pattern}_{tense}_{person}_{number}_{voice}",
                patterns: [pattern],
                tenses: [tense],
                persons: [person],
                numbers: [number],
                voices: [voice]));
        }

        // Exhaust every non-empty subset on each axis while all other axes are enabled.
        foreach (var patterns in NonEmptySubsets(AllPatterns).Where(values => values.Length < AllPatterns.Length))
            yield return new TestCaseData(Filters($"Patterns_{Label(patterns)}", patterns: patterns));

        foreach (var tenses in NonEmptySubsets(AllTenses).Where(values => values.Length < AllTenses.Length))
            yield return new TestCaseData(Filters($"Tenses_{Label(tenses)}", tenses: tenses));

        foreach (var persons in NonEmptySubsets(AllPersons).Where(values => values.Length < AllPersons.Length))
            yield return new TestCaseData(Filters($"Persons_{Label(persons)}", persons: persons));

        foreach (var numbers in NonEmptySubsets(AllNumbers).Where(values => values.Length < AllNumbers.Length))
            yield return new TestCaseData(Filters($"Numbers_{Label(numbers)}", numbers: numbers));

        foreach (var voices in NonEmptySubsets(AllVoices).Where(values => values.Length < AllVoices.Length))
            yield return new TestCaseData(Filters($"Voices_{Label(voices)}", voices: voices));

        yield return new TestCaseData(Filters("AllFilters"));
    }

    static IEnumerable<TestCaseData> EveryInvalidSettingState()
    {
        foreach (var axis in Enum.GetValues<FilterAxis>())
        foreach (var state in Enum.GetValues<InvalidSettingState>())
            yield return new TestCaseData(axis, state);
    }

    static IEnumerable<TestCaseData> EveryCitationOnlySelection()
    {
        foreach (var pattern in AllPatterns)
        foreach (var openSettingsFirst in new[] { false, true })
            yield return new TestCaseData(pattern, openSettingsFirst);
    }

    SQLiteConnection _connection = null!;
    UserDataRepository _userData = null!;
    IDatabaseService _database = null!;

    [SetUp]
    public void SetUp()
    {
        _connection = new SQLiteConnection(":memory:");
        _connection.CreateTable<UserSetting>();

        _userData = new UserDataRepository(_connection);
        _userData.InitializeDefaultsIfNeeded();
        _userData.SetSetting(SettingsKeys.VerbsLemmaMin, 1);
        _userData.SetSetting(SettingsKeys.VerbsLemmaMax, VerbFixtures.Length);

        var verbs = new FakeVerbRepository();
        for (var index = 0; index < VerbFixtures.Length; index++)
        {
            var fixture = VerbFixtures[index];
            verbs.AddLemma(FakeLemma.CreateVerb(
                fixture.LemmaId,
                $"verb_{fixture.Pattern}",
                fixture.Pattern,
                ebtCount: VerbFixtures.Length - index));
            verbs.AddAllAttestedForms(fixture.LemmaId, includeReflexive: true);
            // Include citation forms in the corpus so the queue must exclude them itself.
            verbs.AddAttestedForm(fixture.LemmaId, Tense.Present, Person.Third, Number.Singular, reflexive: false);
        }

        _database = new RepositoryBackedTestDatabaseService(
            new FakeNounRepository(),
            verbs,
            _userData);
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    [TestCaseSource(nameof(GeneratedValidFilterCases))]
    public void EligibleForms_ExactlyMatchGeneratedValidFilterCombinations(
        VerbFilterCase filters)
    {
        StoreFilters(filters);
        var settingsBefore = ReadFilterSettings();
        var expectedFormIds = ExpectedFormIds(filters);

        var firstRead = GetEligibleFormIds();
        var secondRead = GetEligibleFormIds();

        using var assertions = new AssertionScope();
        firstRead.Should().BeEquivalentTo(expectedFormIds,
            "every configured verb filter axis must participate in eligibility");
        secondRead.Should().Equal(firstRead,
            "repeated eligibility reads must be deterministic");
        ReadFilterSettings().Should().BeEquivalentTo(settingsBefore,
            "valid verb settings are persistent state and eligibility is a read operation");
    }

    [TestCaseSource(nameof(EveryInvalidSettingState))]
    public void InvalidRequiredFilter_RecoversItsDefaultAndExpectedEligibility(
        FilterAxis axis,
        InvalidSettingState state)
    {
        var baselineFilters = Filters(
            "RecoveryBaseline",
            voices: axis == FilterAxis.Voices ? AllVoices : [Voice.Reflexive]);
        StoreFilters(baselineFilters);
        CorruptSetting(axis, state);

        var expectedFilters = FiltersWithDefault(axis, baselineFilters);
        var actualFormIds = GetEligibleFormIds();

        using var assertions = new AssertionScope();
        actualFormIds.Should().BeEquivalentTo(ExpectedFormIds(expectedFilters),
            "required verb filters must recover consistently from empty, missing, or malformed storage");
        _userData.GetSetting(SettingKey(axis), "missing").Should().Be(DefaultCsv(axis),
            "self-healing should persist the canonical default for the invalid filter");
    }

    [TestCaseSource(nameof(EveryInvalidSettingState))]
    public void InvalidRequiredFilter_SettingsLoadAndSavePreserveRecovery(
        FilterAxis axis,
        InvalidSettingState state)
    {
        var baseline = Filters("SettingsRecovery",
            patterns: [VerbPattern.Āti, VerbPattern.Oti],
            tenses: [Tense.Imperative, Tense.Future],
            persons: [Person.First, Person.Third],
            numbers: [Number.Singular],
            voices: [Voice.Reflexive]);
        StoreFilters(baseline);
        var expectedSettings = ReadFilterSettings();
        expectedSettings[SettingKey(axis)] = DefaultCsv(axis);
        var expectedFilters = FiltersWithDefault(axis, baseline);
        CorruptSetting(axis, state);

        var settings = new ConjugationSettingsViewModel(null!, _database);

        using var assertions = new AssertionScope();
        AssertSettingsMatch(settings, expectedFilters);
        ReadFilterSettings().Should().BeEquivalentTo(expectedSettings,
            "invalid filters must be repaired before the controls are shown");
        settings.DailyGoal += 1;
        ReadFilterSettings().Should().BeEquivalentTo(expectedSettings,
            "an unrelated edit must preserve the recovered defaults and other filters");
        GetEligibleFormIds().Should().BeEquivalentTo(ExpectedFormIds(expectedFilters));
        AssertSettingsMatch(new ConjugationSettingsViewModel(null!, _database), expectedFilters);
    }

    [TestCaseSource(nameof(GeneratedValidFilterCases))]
    public void ValidFilters_SettingsLoadAndSavePreserveSelection(VerbFilterCase filters)
    {
        StoreFilters(filters);
        var settingsBefore = ReadFilterSettings();

        var settings = new ConjugationSettingsViewModel(null!, _database);

        using var assertions = new AssertionScope();
        AssertSettingsMatch(settings, filters);
        ReadFilterSettings().Should().BeEquivalentTo(settingsBefore);
        settings.DailyGoal += 1;
        ReadFilterSettings().Should().BeEquivalentTo(settingsBefore);
        GetEligibleFormIds().Should().BeEquivalentTo(ExpectedFormIds(filters));
    }

    [TestCaseSource(nameof(EveryCitationOnlySelection))]
    public void CitationOnlySelection_RecoversPersonsBeforeSettingsOrPractice(
        VerbPattern pattern,
        bool openSettingsFirst)
    {
        var filters = Filters("CitationOnly", patterns: [pattern],
            tenses: [Tense.Present], persons: [Person.Third],
            numbers: [Number.Singular], voices: [Voice.Active]);
        StoreFilters(filters);
        var expectedFilters = FiltersWithDefault(FilterAxis.Persons, filters);
        var expectedSettings = ReadFilterSettings();
        expectedSettings[SettingsKeys.VerbsPersons] = DefaultCsv(FilterAxis.Persons);

        using var assertions = new AssertionScope();
        if (openSettingsFirst)
        {
            var settings = new ConjugationSettingsViewModel(null!, _database);
            AssertSettingsMatch(settings, expectedFilters);
            ReadFilterSettings().Should().BeEquivalentTo(expectedSettings);
            settings.DailyGoal += 1;
        }

        var eligible = GetEligibleFormIds();
        eligible.Should().BeEquivalentTo(ExpectedFormIds(expectedFilters));
        eligible.Should().HaveCount(2, "only first and second person remain practiceable");
        ReadFilterSettings().Should().BeEquivalentTo(expectedSettings);
        GetEligibleFormIds().Should().Equal(eligible);
        AssertSettingsMatch(new ConjugationSettingsViewModel(null!, _database), expectedFilters);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CitationGrammar_WithReflexiveVoice_PreservesSelection(bool includeActive)
    {
        var filters = Filters("ReflexiveCitationGrammar",
            tenses: [Tense.Present], persons: [Person.Third], numbers: [Number.Singular],
            voices: includeActive ? AllVoices : [Voice.Reflexive]);
        StoreFilters(filters);
        var settingsBefore = ReadFilterSettings();

        var settings = new ConjugationSettingsViewModel(null!, _database);
        settings.DailyGoal += 1;

        using var assertions = new AssertionScope();
        AssertSettingsMatch(settings, filters);
        GetEligibleFormIds().Should().BeEquivalentTo(ExpectedFormIds(filters));
        ReadFilterSettings().Should().BeEquivalentTo(settingsBefore);
    }

    [Test]
    public void AllPersonsDisabled_KeepsPersonControlsEnabled()
    {
        var settings = new ConjugationSettingsViewModel(null!, _database);
        settings.FirstPerson = false;
        settings.SecondPerson = false;
        settings.ThirdPerson = false;

        using var assertions = new AssertionScope();
        settings.CanDisableFirstPerson.Should().BeTrue();
        settings.CanDisableSecondPerson.Should().BeTrue();
        settings.CanDisableThirdPerson.Should().BeTrue();
    }

    [Test]
    public void SettingsRoundTrip_PreservesNontrivialVerbFilterCombinationAfterPracticeRead()
    {
        var settings = new ConjugationSettingsViewModel(
            navigator: null!, // Navigation is not exercised by this settings contract test.
            db: _database);

        settings.PatternAti = false;
        settings.PatternEti = false;

        settings.Imperative = true;

        settings.FirstPerson = false;
        settings.SecondPerson = false;
        settings.NumberIndex = 1;
        settings.VoiceIndex = 0;

        var expectedFilters = Filters(
            "RoundTrip",
            patterns: [VerbPattern.Āti, VerbPattern.Oti],
            tenses: [Tense.Present, Tense.Imperative],
            persons: [Person.Third],
            numbers: [Number.Singular],
            voices: [Voice.Active, Voice.Reflexive]);
        var settingsBefore = ReadFilterSettings();

        var actualFormIds = GetEligibleFormIds();
        var reloaded = new ConjugationSettingsViewModel(
            navigator: null!, // Navigation is not exercised by this settings contract test.
            db: _database);

        using var assertions = new AssertionScope();
        actualFormIds.Should().BeEquivalentTo(ExpectedFormIds(expectedFilters));
        ReadFilterSettings().Should().BeEquivalentTo(settingsBefore,
            "practice reads must preserve the complete verb filter selection");
        new
        {
            reloaded.PatternAti,
            reloaded.PatternAtiLong,
            reloaded.PatternEti,
            reloaded.PatternOti,
            reloaded.Present,
            reloaded.Imperative,
            reloaded.Optative,
            reloaded.Future,
            reloaded.FirstPerson,
            reloaded.SecondPerson,
            reloaded.ThirdPerson,
            reloaded.NumberIndex,
            reloaded.VoiceIndex
        }.Should().BeEquivalentTo(new
        {
            PatternAti = false,
            PatternAtiLong = true,
            PatternEti = false,
            PatternOti = true,
            Present = true,
            Imperative = true,
            Optative = false,
            Future = false,
            FirstPerson = false,
            SecondPerson = false,
            ThirdPerson = true,
            NumberIndex = 1,
            VoiceIndex = 0
        });
    }

    void StoreFilters(VerbFilterCase filters)
    {
        _userData.SetSetting(SettingsKeys.VerbsPatterns, SettingsHelpers.ToCsv(filters.Patterns));
        _userData.SetSetting(SettingsKeys.VerbsTenses, SettingsHelpers.ToCsv(filters.Tenses));
        _userData.SetSetting(SettingsKeys.VerbsPersons, SettingsHelpers.ToCsv(filters.Persons));
        _userData.SetSetting(SettingsKeys.VerbsNumbers, SettingsHelpers.ToCsv(filters.Numbers));
        _userData.SetSetting(SettingsKeys.VerbsVoices, SettingsHelpers.ToCsv(filters.Voices));
    }

    List<long> GetEligibleFormIds() =>
        new PracticeQueueBuilder(_database)
            .GetEligibleFormIds(PracticeType.Conjugation);

    Dictionary<string, string> ReadFilterSettings() =>
        FilterSettingKeys.ToDictionary(
            key => key,
            key => _userData.GetSetting(key, "missing"));

    static List<long> ExpectedFormIds(VerbFilterCase filters)
    {
        var formIds = new List<long>();

        foreach (var fixture in VerbFixtures)
        {
            if (!filters.Patterns.Contains(fixture.Pattern))
                continue;

            foreach (var tense in filters.Tenses)
            foreach (var person in filters.Persons)
            foreach (var number in filters.Numbers)
            foreach (var voice in filters.Voices)
            {
                if (voice == Voice.Active &&
                    tense == Tense.Present &&
                    person == Person.Third &&
                    number == Number.Singular)
                {
                    continue;
                }

                formIds.Add(Conjugation.ResolveId(
                    fixture.LemmaId,
                    tense,
                    person,
                    number,
                    voice,
                    endingId: 0));
            }
        }

        return formIds;
    }

    void CorruptSetting(FilterAxis axis, InvalidSettingState state)
    {
        var key = SettingKey(axis);
        if (state == InvalidSettingState.Missing)
        {
            _connection.Execute("DELETE FROM user_settings WHERE key = ?", key);
            return;
        }

        _userData.SetSetting(key, state switch
        {
            InvalidSettingState.Empty => string.Empty,
            InvalidSettingState.Malformed => "not-an-enum-list",
            InvalidSettingState.Whitespace => "   ",
            InvalidSettingState.None => "0",
            InvalidSettingState.Undefined => "999999",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        });
    }

    static void AssertSettingsMatch(ConjugationSettingsViewModel settings, VerbFilterCase expected)
    {
        Selected((settings.PatternAti, VerbPattern.Ati), (settings.PatternAtiLong, VerbPattern.Āti),
            (settings.PatternEti, VerbPattern.Eti), (settings.PatternOti, VerbPattern.Oti))
            .Should().BeEquivalentTo(expected.Patterns);
        Selected((settings.Present, Tense.Present), (settings.Imperative, Tense.Imperative),
            (settings.Optative, Tense.Optative), (settings.Future, Tense.Future))
            .Should().BeEquivalentTo(expected.Tenses);
        Selected((settings.FirstPerson, Person.First), (settings.SecondPerson, Person.Second),
            (settings.ThirdPerson, Person.Third)).Should().BeEquivalentTo(expected.Persons);
        Selected((settings.IncludeSingular, Number.Singular), (settings.IncludePlural, Number.Plural))
            .Should().BeEquivalentTo(expected.Numbers);
        Selected((settings.IncludeNormal, Voice.Active), (settings.IncludeReflexive, Voice.Reflexive))
            .Should().BeEquivalentTo(expected.Voices);
    }

    static IEnumerable<T> Selected<T>(params (bool Enabled, T Value)[] options) =>
        options.Where(option => option.Enabled).Select(option => option.Value);

    static VerbFilterCase FiltersWithDefault(
        FilterAxis axis,
        VerbFilterCase baseline) => Filters(
        $"Default_{axis}",
        patterns: axis == FilterAxis.Patterns ? SettingsKeys.VerbsDefaultPatterns : baseline.Patterns,
        tenses: axis == FilterAxis.Tenses ? SettingsKeys.VerbsDefaultTenses : baseline.Tenses,
        persons: axis == FilterAxis.Persons ? SettingsKeys.VerbsDefaultPersons : baseline.Persons,
        numbers: axis == FilterAxis.Numbers ? SettingsKeys.DefaultNumbers : baseline.Numbers,
        voices: axis == FilterAxis.Voices ? SettingsKeys.VerbsDefaultVoices : baseline.Voices);

    static string SettingKey(FilterAxis axis) => axis switch
    {
        FilterAxis.Patterns => SettingsKeys.VerbsPatterns,
        FilterAxis.Tenses => SettingsKeys.VerbsTenses,
        FilterAxis.Persons => SettingsKeys.VerbsPersons,
        FilterAxis.Numbers => SettingsKeys.VerbsNumbers,
        FilterAxis.Voices => SettingsKeys.VerbsVoices,
        _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
    };

    static string DefaultCsv(FilterAxis axis) => axis switch
    {
        FilterAxis.Patterns => SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultPatterns),
        FilterAxis.Tenses => SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultTenses),
        FilterAxis.Persons => SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultPersons),
        FilterAxis.Numbers => SettingsHelpers.ToCsv(SettingsKeys.DefaultNumbers),
        FilterAxis.Voices => SettingsHelpers.ToCsv(SettingsKeys.VerbsDefaultVoices),
        _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
    };

    static VerbFilterCase Filters(
        string name,
        IReadOnlyList<VerbPattern>? patterns = null,
        IReadOnlyList<Tense>? tenses = null,
        IReadOnlyList<Person>? persons = null,
        IReadOnlyList<Number>? numbers = null,
        IReadOnlyList<Voice>? voices = null) =>
        new(
            name,
            patterns ?? AllPatterns,
            tenses ?? AllTenses,
            persons ?? AllPersons,
            numbers ?? AllNumbers,
            voices ?? AllVoices);

    static IEnumerable<T[]> NonEmptySubsets<T>(IReadOnlyList<T> values)
    {
        var subsetCount = 1 << values.Count;
        for (var mask = 1; mask < subsetCount; mask++)
        {
            yield return Enumerable.Range(0, values.Count)
                .Where(index => (mask & (1 << index)) != 0)
                .Select(index => values[index])
                .ToArray();
        }
    }

    static string Label<T>(IEnumerable<T> values) =>
        string.Join("+", values);
}
