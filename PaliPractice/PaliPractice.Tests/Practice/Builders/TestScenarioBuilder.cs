using PaliPractice.Models.Inflection;
using PaliPractice.Services.UserData;
using PaliPractice.Tests.Practice.Fakes;

namespace PaliPractice.Tests.Practice.Builders;

/// <summary>
/// Fluent builder for creating test scenarios for PracticeQueueBuilder tests.
/// </summary>
public class TestScenarioBuilder
{
    readonly FakeNounRepository _nouns = new();
    readonly FakeVerbRepository _verbs = new();
    readonly FakeUserDataRepository _userData = new();

    int _nounLemmaCounter = 10001;  // Start at noun lemma ID range
    int _verbLemmaCounter = 70001;  // Start at verb lemma ID range

    // Track configuration for generating valid due forms
    readonly List<int> _addedNounLemmaIds = [];
    readonly List<(Gender gender, int lemmaId)> _addedNounGenders = [];
    Case[] _configuredCases = [];
    Number[] _configuredNumbers = [];

    // === Configuration ===

    /// <summary>
    /// Sets the lemma range for nouns.
    /// </summary>
    public TestScenarioBuilder WithNounRange(int min, int max)
    {
        _userData.SetSetting(SettingsKeys.NounsLemmaMin, min);
        _userData.SetSetting(SettingsKeys.NounsLemmaMax, max);
        return this;
    }

    /// <summary>
    /// Sets the lemma range for verbs.
    /// </summary>
    public TestScenarioBuilder WithVerbRange(int min, int max)
    {
        _userData.SetSetting(SettingsKeys.VerbsLemmaMin, min);
        _userData.SetSetting(SettingsKeys.VerbsLemmaMax, max);
        return this;
    }

    /// <summary>
    /// Sets which cases are enabled for practice.
    /// </summary>
    public TestScenarioBuilder WithCases(params Case[] cases)
    {
        _configuredCases = cases;
        _userData.SetSetting(SettingsKeys.NounsCases, SettingsHelpers.ToCsv(cases));
        return this;
    }

    /// <summary>
    /// Sets which tenses are enabled for practice.
    /// </summary>
    public TestScenarioBuilder WithTenses(params Tense[] tenses)
    {
        _userData.SetSetting(SettingsKeys.VerbsTenses, SettingsHelpers.ToCsv(tenses));
        return this;
    }

    /// <summary>
    /// Sets which persons are enabled for practice.
    /// </summary>
    public TestScenarioBuilder WithPersons(params Person[] persons)
    {
        _userData.SetSetting(SettingsKeys.VerbsPersons, SettingsHelpers.ToCsv(persons));
        return this;
    }

    /// <summary>
    /// Sets which numbers are enabled for practice.
    /// </summary>
    public TestScenarioBuilder WithNumbers(params Number[] numbers)
    {
        _configuredNumbers = numbers;
        _userData.SetSetting(SettingsKeys.NounsNumbers, SettingsHelpers.ToCsv(numbers));
        _userData.SetSetting(SettingsKeys.VerbsNumbers, SettingsHelpers.ToCsv(numbers));
        return this;
    }

    /// <summary>
    /// Sets which voices are enabled for practice.
    /// </summary>
    public TestScenarioBuilder WithVoices(params Voice[] voices)
    {
        _userData.SetSetting(SettingsKeys.VerbsVoices, SettingsHelpers.ToCsv(voices));
        return this;
    }

    /// <summary>
    /// Sets which masculine noun patterns are enabled.
    /// </summary>
    public TestScenarioBuilder WithMascPatterns(params NounPattern[] patterns)
    {
        _userData.SetSetting(SettingsKeys.NounsMascPatterns, SettingsHelpers.ToCsv(patterns));
        return this;
    }

    /// <summary>
    /// Sets which feminine noun patterns are enabled.
    /// </summary>
    public TestScenarioBuilder WithFemPatterns(params NounPattern[] patterns)
    {
        _userData.SetSetting(SettingsKeys.NounsFemPatterns, SettingsHelpers.ToCsv(patterns));
        return this;
    }

    /// <summary>
    /// Sets which neuter noun patterns are enabled.
    /// </summary>
    public TestScenarioBuilder WithNeutPatterns(params NounPattern[] patterns)
    {
        _userData.SetSetting(SettingsKeys.NounsNeutPatterns, SettingsHelpers.ToCsv(patterns));
        return this;
    }

    /// <summary>
    /// Sets which verb patterns are enabled.
    /// </summary>
    public TestScenarioBuilder WithVerbPatterns(params VerbPattern[] patterns)
    {
        _userData.SetSetting(SettingsKeys.VerbsPatterns, SettingsHelpers.ToCsv(patterns));
        return this;
    }

    // === Lemma Pool ===

    /// <summary>
    /// Adds N nouns with the specified pattern and gender, with all forms attested.
    /// </summary>
    public TestScenarioBuilder AddNouns(int count, NounPattern pattern, Gender gender)
    {
        for (int i = 0; i < count; i++)
        {
            var lemmaId = _nounLemmaCounter++;
            var lemma = FakeLemma.CreateNoun(
                lemmaId,
                $"noun_{lemmaId}",
                gender,
                pattern,
                ebtCount: 1000 - i  // Decreasing EBT count for rank ordering
            );
            _nouns.AddLemma(lemma);
            _nouns.AddAllAttestedForms(lemmaId, gender);

            // Track for generating valid due forms
            _addedNounLemmaIds.Add(lemmaId);
            _addedNounGenders.Add((gender, lemmaId));
        }
        return this;
    }

    /// <summary>
    /// Adds N verbs with the specified pattern, with all forms attested.
    /// </summary>
    public TestScenarioBuilder AddVerbs(int count, VerbPattern pattern, bool includeReflexive = false)
    {
        for (int i = 0; i < count; i++)
        {
            var lemmaId = _verbLemmaCounter++;
            var lemma = FakeLemma.CreateVerb(
                lemmaId,
                $"verb_{lemmaId}",
                pattern,
                ebtCount: 1000 - i
            );
            _verbs.AddLemma(lemma);

            if (!includeReflexive)
                _verbs.MarkAsNonReflexive(lemmaId);

            _verbs.AddAllAttestedForms(lemmaId, includeReflexive);
        }
        return this;
    }

    // === User Progress ===

    /// <summary>
    /// Adds due noun forms at the specified mastery level.
    /// </summary>
    public TestScenarioBuilder WithDueNounForms(params (long formId, int masteryLevel)[] forms)
    {
        foreach (var (formId, level) in forms)
        {
            _userData.AddDueNounForm(formId, level);
        }
        return this;
    }

    /// <summary>
    /// Adds due verb forms at the specified mastery level.
    /// </summary>
    public TestScenarioBuilder WithDueVerbForms(params (long formId, int masteryLevel)[] forms)
    {
        foreach (var (formId, level) in forms)
        {
            _userData.AddDueVerbForm(formId, level);
        }
        return this;
    }

    /// <summary>
    /// Generates due noun forms distributed across all 5 mastery level buckets.
    /// Uses actually configured lemmas, cases, and numbers to ensure forms are eligible.
    ///
    /// Buckets: [1-2], [3-4], [5-6], [7-8], [9-10]
    /// Total forms added = formsPerBucket * 5
    /// </summary>
    /// <param name="formsPerBucket">Number of forms to add per bucket (distributed across levels within bucket)</param>
    public TestScenarioBuilder WithDueNounFormsAcrossBuckets(int formsPerBucket)
    {
        if (_addedNounLemmaIds.Count == 0)
            throw new InvalidOperationException("Must add nouns before calling WithDueNounFormsAcrossBuckets");

        // Filter out Case.None which is never eligible
        var validCases = _configuredCases.Length > 0
            ? _configuredCases.Where(c => c != Case.None).ToArray()
            : [Case.Nominative, Case.Accusative];  // Fallback

        var validNumbers = _configuredNumbers.Length > 0
            ? _configuredNumbers.Where(n => n != Number.None).ToArray()
            : [Number.Singular];  // Fallback

        // Build list of all valid form IDs from actual configuration
        var eligibleForms = new List<long>();
        foreach (var (gender, lemmaId) in _addedNounGenders)
        {
            foreach (var caseValue in validCases)
            {
                foreach (var number in validNumbers)
                {
                    var formId = Declension.ResolveId(lemmaId, caseValue, gender, number, 0);
                    eligibleForms.Add(formId);
                }
            }
        }

        // 5 buckets with 2 levels each
        (int Min, int Max)[] buckets = [(1, 2), (3, 4), (5, 6), (7, 8), (9, 10)];
        int formIndex = 0;

        foreach (var (minLevel, maxLevel) in buckets)
        {
            // Distribute formsPerBucket exactly across the 2 levels in this bucket
            // For odd values: first level gets ceiling, second gets floor
            var formsForMinLevel = (formsPerBucket + 1) / 2;  // Ceiling division
            var formsForMaxLevel = formsPerBucket / 2;        // Floor division

            for (int i = 0; i < formsForMinLevel && formIndex < eligibleForms.Count; i++)
            {
                var formId = eligibleForms[formIndex % eligibleForms.Count];
                _userData.AddDueNounForm(formId, minLevel);
                formIndex++;
            }

            for (int i = 0; i < formsForMaxLevel && formIndex < eligibleForms.Count; i++)
            {
                var formId = eligibleForms[formIndex % eligibleForms.Count];
                _userData.AddDueNounForm(formId, maxLevel);
                formIndex++;
            }
        }

        return this;
    }

    // === Build ===

    /// <summary>
    /// Builds the test scenario and returns the fake database service.
    /// </summary>
    public FakeDatabaseService Build()
    {
        return new FakeDatabaseService(_nouns, _verbs, _userData);
    }

    // === Pre-built Scenarios ===

    /// <summary>
    /// Creates a minimal scenario: 2 nouns, 2 cases, singular only.
    /// Results in 4 eligible forms.
    /// </summary>
    public static TestScenarioBuilder Minimal() =>
        new TestScenarioBuilder()
            .WithNounRange(1, 2)
            .WithCases(Case.Nominative, Case.Accusative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(2, NounPattern.AMasc, Gender.Masculine);

    /// <summary>
    /// Creates a small scenario: 5 nouns, all cases, singular only.
    /// Results in 40 eligible forms.
    /// </summary>
    public static TestScenarioBuilder Small() =>
        new TestScenarioBuilder()
            .WithNounRange(1, 5)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental,
                       Case.Dative, Case.Ablative, Case.Genitive,
                       Case.Locative, Case.Vocative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(5, NounPattern.AMasc, Gender.Masculine);

    /// <summary>
    /// Creates a medium scenario: 20 nouns, all cases, both numbers.
    /// Results in 320 eligible forms.
    /// </summary>
    public static TestScenarioBuilder Medium() =>
        new TestScenarioBuilder()
            .WithNounRange(1, 20)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental,
                       Case.Dative, Case.Ablative, Case.Genitive,
                       Case.Locative, Case.Vocative)
            .WithNumbers(Number.Singular, Number.Plural)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(20, NounPattern.AMasc, Gender.Masculine);

    /// <summary>
    /// Creates a scenario for testing verbs.
    /// </summary>
    public static TestScenarioBuilder VerbSmall() =>
        new TestScenarioBuilder()
            .WithVerbRange(1, 5)
            .WithTenses(Tense.Present)
            .WithPersons(Person.First, Person.Second, Person.Third)
            .WithNumbers(Number.Singular, Number.Plural)
            .WithVoices(Voice.Active)
            .WithVerbPatterns(VerbPattern.Ati)
            .AddVerbs(5, VerbPattern.Ati);
}
