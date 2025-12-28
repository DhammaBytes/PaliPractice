using PaliPractice.Models;
using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;

namespace PaliPractice.Services.Practice;

/// <summary>
/// Common data for form mastery used in queue building.
/// Extracted from type-specific entities (NounsFormMastery, VerbsFormMastery).
/// </summary>
record FormMasteryData(long FormId, int MasteryLevel, DateTime NextDueUtc)
{
    public bool IsDue => DateTime.UtcNow >= NextDueUtc;
}

/// <summary>
/// Builds a practice queue using a three-bucket SRS approach:
///
/// 1. DUE REVIEWS: Forms whose cooldown has expired (LastPracticedUtc + cooldown &lt; now).
///    Prioritized by: overdue time, low mastery level, and difficult combo patterns.
///
/// 2. NEW FORMS: Forms never practiced before, introduced gradually (1 per 4-6 reviews).
///    Randomized to provide variety and prevent pattern memorization.
///
/// 3. DIFFICULT COMBOS: Meta-bucket boost for grammatical combinations the user struggles with.
///    E.g., if user often fails "gen_masc_pl", those forms get priority boost.
///
/// The queue is shuffled with priority bias to avoid predictable ordering while
/// ensuring high-priority items appear early. Pattern: H-M-L-H-M-L where available.
/// </summary>
public class PracticeQueueBuilder : IPracticeQueueBuilder
{
    readonly UserDataRepository _userData;
    readonly NounRepository _nouns;
    readonly VerbRepository _verbs;
    readonly Random _random = new();

    // Introduce new forms gradually: 1 new form every 4-6 reviews.
    // Randomized interval prevents predictable patterns.
    const int NewFormIntervalMin = 4;
    const int NewFormIntervalMax = 6;

    public PracticeQueueBuilder(IDatabaseService db)
    {
        _userData = db.UserData;
        _nouns = db.Nouns;
        _verbs = db.Verbs;
    }

    public List<PracticeItem> BuildQueue(PracticeType type, int count)
    {
        System.Diagnostics.Debug.WriteLine($"[Queue] BuildQueue({type}, {count})");

        var queue = new List<PracticeItem>();

        // 1. Get all eligible form IDs (corpus-attested, matching settings)
        var eligibleFormIds = GetEligibleFormIds(type);
        var eligibleSet = eligibleFormIds.ToHashSet();

        System.Diagnostics.Debug.WriteLine($"[Queue] Eligible form IDs: {eligibleFormIds.Count}");

        if (eligibleFormIds.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Queue] WARNING: No eligible forms for {type}!");
            return queue;
        }

        // 2. Get practiced form IDs (type-specific)
        var practicedIds = type == PracticeType.Declension
            ? _userData.GetPracticedNounFormIds()
            : _userData.GetPracticedVerbFormIds();

        // 3. Categorize into buckets (type-specific)
        var dueForReview = GetDueForms(type, eligibleSet);

        var untriedIds = eligibleFormIds
            .Where(id => !practicedIds.Contains(id))
            .ToList();

        // 4. Get difficulty weights for prioritization (type-specific)
        var hardCombos = GetHardestCombinations(type);

        // 5. Score and sort due forms by priority
        var scoredDue = dueForReview
            .Select(f => (Form: f, Priority: CalculatePriority(f, hardCombos, type)))
            .OrderByDescending(x => x.Priority)
            .ToList();

        // 6. Build queue with review-to-new ratio
        int reviewsAdded = 0;
        int newAdded = 0;
        int reviewIndex = 0;
        int nextNewInterval = _random.Next(NewFormIntervalMin, NewFormIntervalMax + 1);

        while (queue.Count < count)
        {
            bool addNew = ShouldAddNew(reviewsAdded, newAdded, untriedIds.Count, nextNewInterval);

            if (addNew && newAdded < untriedIds.Count)
            {
                // Add a random new form
                var newIndex = _random.Next(untriedIds.Count);
                var newFormId = untriedIds[newIndex];
                untriedIds.RemoveAt(newIndex);  // Don't repeat

                queue.Add(new PracticeItem(
                    newFormId,
                    type,
                    ExtractLemmaId(newFormId, type),
                    PracticeItemSource.NewForm,
                    0.5,  // Neutral priority for new forms
                    MasteryLevel: 1  // New form starts at level 1
                ));
                newAdded++;
                nextNewInterval = _random.Next(NewFormIntervalMin, NewFormIntervalMax + 1);
            }
            else if (reviewIndex < scoredDue.Count)
            {
                // Add next highest-priority review
                var (form, priority) = scoredDue[reviewIndex];
                queue.Add(new PracticeItem(
                    form.FormId,
                    type,
                    ExtractLemmaId(form.FormId, type),
                    priority > 0.7
                        ? PracticeItemSource.DifficultCombo
                        : PracticeItemSource.DueForReview,
                    priority,
                    form.MasteryLevel
                ));
                reviewIndex++;
                reviewsAdded++;
            }
            else if (newAdded < untriedIds.Count)
            {
                // No more reviews, add new forms
                var newIndex = _random.Next(untriedIds.Count);
                var newFormId = untriedIds[newIndex];
                untriedIds.RemoveAt(newIndex);

                queue.Add(new PracticeItem(
                    newFormId,
                    type,
                    ExtractLemmaId(newFormId, type),
                    PracticeItemSource.NewForm,
                    0.5,
                    MasteryLevel: 1
                ));
                newAdded++;
            }
            else
            {
                // No more forms available
                break;
            }
        }

        // 7. Shuffle with priority bias to avoid predictable ordering
        return ShuffleWithPriorityBias(queue);
    }

    bool ShouldAddNew(int reviews, int newCount, int availableNew, int interval)
    {
        if (availableNew == 0) return false;
        if (reviews == 0) return false;  // Start with a review if possible

        // Add a new form every [interval] reviews
        return reviews > 0 && reviews % interval == 0 && newCount < reviews / interval;
    }

    /// <summary>
    /// Gets due forms for review, filtered by eligible set.
    /// Uses type-specific repository methods.
    /// </summary>
    List<FormMasteryData> GetDueForms(PracticeType type, HashSet<long> eligibleSet)
    {
        if (type == PracticeType.Declension)
        {
            return _userData.GetDueNounForms(limit: 500)
                .Where(f => eligibleSet.Contains(f.FormId))
                .Select(f => new FormMasteryData(f.FormId, f.MasteryLevel, f.NextDueUtc))
                .ToList();
        }
        else
        {
            return _userData.GetDueVerbForms(limit: 500)
                .Where(f => eligibleSet.Contains(f.FormId))
                .Select(f => new FormMasteryData(f.FormId, f.MasteryLevel, f.NextDueUtc))
                .ToList();
        }
    }

    /// <summary>
    /// Gets hardest combinations as a lookup dictionary.
    /// Uses type-specific repository methods.
    /// </summary>
    Dictionary<string, double> GetHardestCombinations(PracticeType type)
    {
        if (type == PracticeType.Declension)
        {
            return _userData.GetHardestNounCombinations(limit: 20)
                .ToDictionary(c => c.ComboKey, c => c.DifficultyScore);
        }
        else
        {
            return _userData.GetHardestVerbCombinations(limit: 20)
                .ToDictionary(c => c.ComboKey, c => c.DifficultyScore);
        }
    }

    /// <summary>
    /// Calculates a 0.0-1.0 priority score for a due form. Higher = more urgent.
    ///
    /// Factors (weighted to sum to ~1.0 max):
    /// - Overdue time:  0.0-0.3 (3+ days overdue = max)
    /// - Low mastery:   0.05-0.45 (level 1 = 0.45, level 10 = 0.05)
    /// - Hard combo:    0.0-0.3 (boost if this grammatical combo is difficult)
    ///
    /// Example: A level-2 form that's 2 days overdue in a hard combo:
    ///   0.2 (overdue) + 0.4 (low mastery) + 0.2 (hard combo) = 0.8
    /// </summary>
    double CalculatePriority(FormMasteryData form, Dictionary<string, double> hardCombos, PracticeType type)
    {
        double priority = 0.0;

        // Factor 1: Overdue time (more overdue = higher priority)
        // 0.1 per day overdue, capped at 0.3 (3+ days)
        var overdueDays = (DateTime.UtcNow - form.NextDueUtc).TotalDays;
        priority += Math.Min(0.3, Math.Max(0, overdueDays) * 0.1);

        // Factor 2: Low mastery level (struggling = higher priority)
        // Level 1 → 0.45, Level 10 → 0.05
        priority += (10 - form.MasteryLevel) * 0.05;

        // Factor 3: Combination difficulty boost
        // If user struggles with this grammatical combo (e.g., "gen_masc_pl"), boost priority
        var comboKey = GetComboKey(form.FormId, type);
        if (hardCombos.TryGetValue(comboKey, out var difficulty))
        {
            priority += difficulty * 0.3;
        }

        return Math.Min(1.0, priority);
    }

    public List<long> GetEligibleFormIds(PracticeType type)
    {
        return type switch
        {
            PracticeType.Declension => GetEligibleDeclensionFormIds(),
            PracticeType.Conjugation => GetEligibleConjugationFormIds(),
            _ => []
        };
    }

    List<long> GetEligibleDeclensionFormIds()
    {
        // Load settings with self-healing (rewrites defaults if empty/invalid)
        var enabledCases = _userData.GetEnumListOrResetDefault(SettingsKeys.NounsCases, SettingsKeys.NounsDefaultCases);
        var enabledNumbers = _userData.GetEnumListOrResetDefault(SettingsKeys.NounsNumbers, SettingsKeys.DefaultNumbers);

        // Get total noun count for range validation
        var totalNouns = _nouns.GetCount();
        var (minRank, maxRank) = _userData.GetLemmaRangeOrResetDefault(PracticeType.Declension, totalNouns);

        System.Diagnostics.Debug.WriteLine($"[Queue/Decl] cases={enabledCases.Count}, numbers={enabledNumbers.Count}, rank={minRank}-{maxRank}");

        // Load enabled patterns per gender with self-healing
        var mascEnabled = _userData.GetEnumSetOrResetDefault(SettingsKeys.NounsMascPatterns, SettingsKeys.NounsDefaultMascPatterns);
        var neutEnabled = _userData.GetEnumSetOrResetDefault(SettingsKeys.NounsNeutPatterns, SettingsKeys.NounsDefaultNeutPatterns);
        var femEnabled = _userData.GetEnumSetOrResetDefault(SettingsKeys.NounsFemPatterns, SettingsKeys.NounsDefaultFemPatterns);

        System.Diagnostics.Debug.WriteLine($"[Queue/Decl] enabledCases: {enabledCases.Count} ({string.Join(",", enabledCases)})");
        System.Diagnostics.Debug.WriteLine($"[Queue/Decl] enabledNumbers: {enabledNumbers.Count} ({string.Join(",", enabledNumbers)})");

        var formIds = new List<long>();

        // Get noun lemmas by rank
        var lemmas = _nouns.GetLemmasByRank(minRank, maxRank);
        System.Diagnostics.Debug.WriteLine($"[Queue/Decl] lemmas in rank range: {lemmas.Count}");

        foreach (var lemma in lemmas)
        {
            var noun = (Noun)lemma.Primary;

            // Skip if pattern is not enabled for this gender
            if (!IsPatternEnabled(noun.Pattern, noun.Gender, mascEnabled, neutEnabled, femEnabled))
                continue;

            foreach (var @case in enabledCases)
            {
                foreach (var number in enabledNumbers)
                {
                    // Check if this combination has corpus attestation
                    if (_nouns.HasAttestedForm(lemma.LemmaId, @case, noun.Gender, number))
                    {
                        // Use EndingId=0 for combination reference
                        var formId = Declension.ResolveId(lemma.LemmaId, @case, noun.Gender, number, 0);
                        formIds.Add(formId);
                    }
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"[Queue/Decl] Generated {formIds.Count} form IDs");
        return formIds;
    }

    /// <summary>
    /// Checks if a noun pattern is enabled based on gender and enabled pattern sets.
    /// Irregular patterns are checked against their parent regular pattern.
    /// </summary>
    static bool IsPatternEnabled(
        NounPattern pattern,
        Gender gender,
        HashSet<NounPattern> mascEnabled,
        HashSet<NounPattern> neutEnabled,
        HashSet<NounPattern> femEnabled)
    {
        // For irregulars, check if parent regular pattern is enabled
        var checkPattern = pattern.IsIrregular() ? pattern.ParentRegular() : pattern;

        // Get the appropriate enabled set for this gender
        var enabled = gender switch
        {
            Gender.Masculine => mascEnabled,
            Gender.Neuter => neutEnabled,
            Gender.Feminine => femEnabled,
            _ => []
        };

        return enabled.Contains(checkPattern);
    }

    /// <summary>
    /// Checks if a verb pattern is enabled based on enabled pattern set.
    /// Irregular patterns are checked against their parent regular pattern.
    /// </summary>
    static bool IsVerbPatternEnabled(VerbPattern pattern, HashSet<VerbPattern> enabledPatterns)
    {
        // For irregulars, check if parent regular pattern is enabled
        var checkPattern = pattern.IsIrregular() ? pattern.ParentRegular() : pattern;
        return enabledPatterns.Contains(checkPattern);
    }

    List<long> GetEligibleConjugationFormIds()
    {
        // Load settings with self-healing (rewrites defaults if empty/invalid)
        var enabledTenses = _userData.GetEnumListOrResetDefault(SettingsKeys.VerbsTenses, SettingsKeys.VerbsDefaultTenses);
        var enabledPersons = _userData.GetEnumListOrResetDefault(SettingsKeys.VerbsPersons, SettingsKeys.VerbsDefaultPersons);
        var enabledNumbers = _userData.GetEnumListOrResetDefault(SettingsKeys.VerbsNumbers, SettingsKeys.DefaultNumbers);
        var enabledVoices = _userData.GetEnumListOrResetDefault(SettingsKeys.VerbsVoices, SettingsKeys.VerbsDefaultVoices);
        var enabledPatterns = _userData.GetEnumSetOrResetDefault(SettingsKeys.VerbsPatterns, SettingsKeys.VerbsDefaultPatterns);

        // Get total verb count for range validation
        var totalVerbs = _verbs.GetCount();
        var (minRank, maxRank) = _userData.GetLemmaRangeOrResetDefault(PracticeType.Conjugation, totalVerbs);

        System.Diagnostics.Debug.WriteLine($"[Queue/Conj] tenses={enabledTenses.Count}, persons={enabledPersons.Count}, rank={minRank}-{maxRank}");
        System.Diagnostics.Debug.WriteLine($"[Queue/Conj] enabledNumbers: {enabledNumbers.Count}, voices: {enabledVoices.Count}");

        var includeActive = enabledVoices.Contains(Voice.Normal);
        var includeReflexive = enabledVoices.Contains(Voice.Reflexive);

        var formIds = new List<long>();

        // Get verb lemmas by rank
        var lemmas = _verbs.GetLemmasByRank(minRank, maxRank);

        foreach (var lemma in lemmas)
        {
            var verb = (Verb)lemma.Primary;

            // Skip if pattern is not enabled
            if (!IsVerbPatternEnabled(verb.Pattern, enabledPatterns))
                continue;

            var hasReflexive = _verbs.HasReflexive(lemma.LemmaId);

            foreach (var tense in enabledTenses)
            {
                foreach (var person in enabledPersons)
                {
                    foreach (var number in enabledNumbers)
                    {
                        // Normal (active) forms
                        if (includeActive)
                        {
                            if (_verbs.HasAttestedForm(lemma.LemmaId, tense, person, number, reflexive: false))
                            {
                                var formId = Conjugation.ResolveId(lemma.LemmaId, tense, person, number, Voice.Normal, 0);
                                formIds.Add(formId);
                            }
                        }

                        // Reflexive forms
                        if (includeReflexive && hasReflexive)
                        {
                            if (_verbs.HasAttestedForm(lemma.LemmaId, tense, person, number, reflexive: true))
                            {
                                var formId = Conjugation.ResolveId(lemma.LemmaId, tense, person, number, Voice.Reflexive, 0);
                                formIds.Add(formId);
                            }
                        }
                    }
                }
            }
        }

        return formIds;
    }

    static int ExtractLemmaId(long formId, PracticeType type)
    {
        // FormId format:
        // Declension: lemmaId(5) + case(1) + gender(1) + number(1) + endingId(1) = 9 digits
        // Conjugation: lemmaId(5) + tense(1) + person(1) + number(1) + reflexive(1) + endingId(1) = 10 digits

        return type switch
        {
            PracticeType.Declension => (int)(formId / 10_000),  // Remove last 4 digits
            PracticeType.Conjugation => (int)(formId / 100_000),  // Remove last 5 digits
            _ => 0
        };
    }

    static string GetComboKey(long formId, PracticeType type)
    {
        return type == PracticeType.Declension
            ? Declension.ComboKeyFromId((int)formId)
            : Conjugation.ComboKeyFromId(formId);
    }

    /// <summary>
    /// Shuffles the queue while maintaining priority ordering.
    ///
    /// Problem: Pure priority ordering is predictable and boring.
    /// Solution: Divide into tiers, shuffle within each tier, then interleave.
    ///
    /// Tiers (based on priority score):
    /// - High (0.7-1.0): Overdue/struggling forms that need immediate attention
    /// - Medium (0.4-0.7): Regular reviews
    /// - Low (0.0-0.4): Well-learned forms or new introductions
    ///
    /// Interleave pattern: H-M-L-H-M-L...
    /// This ensures high-priority items dominate early positions while
    /// mixing in easier items for psychological relief.
    /// Falls back to next available tier when one is exhausted.
    /// </summary>
    List<PracticeItem> ShuffleWithPriorityBias(List<PracticeItem> queue)
    {
        var tier1 = queue.Where(i => i.Priority >= 0.7).OrderBy(_ => _random.Next()).ToList();
        var tier2 = queue.Where(i => i.Priority is >= 0.4 and < 0.7).OrderBy(_ => _random.Next()).ToList();
        var tier3 = queue.Where(i => i.Priority < 0.4).OrderBy(_ => _random.Next()).ToList();

        var result = new List<PracticeItem>();
        int i1 = 0, i2 = 0, i3 = 0;
        int index = 0;

        while (result.Count < queue.Count)
        {
            // Interleave pattern: position 0 → tier1, position 1 → tier2, position 2 → tier3
            // Falls back to next available tier when preferred tier is exhausted
            if (index % 3 == 2 && i3 < tier3.Count)
                result.Add(tier3[i3++]);
            else if (index % 3 == 1 && i2 < tier2.Count)
                result.Add(tier2[i2++]);
            else if (i1 < tier1.Count)
                result.Add(tier1[i1++]);
            else if (i2 < tier2.Count)
                result.Add(tier2[i2++]);
            else if (i3 < tier3.Count)
                result.Add(tier3[i3++]);

            index++;
        }

        return result;
    }
}
