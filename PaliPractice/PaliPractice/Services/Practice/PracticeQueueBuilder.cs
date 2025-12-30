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
record FormMasteryData(long FormId, int MasteryLevel, DateTime NextDueUtc);

/// <summary>
/// Groups forms by grammatical category for balanced selection.
/// Categories are NounPattern for nouns, (Tense, Voice) for verbs.
///
/// Note: DueForms is used only for initial grouping. During queue building,
/// scoredByCategory is the authoritative source for remaining reviews.
/// </summary>
partial record CategoryForms(string Key, List<FormMasteryData> DueForms, List<long> UntriedForms)
{
    /// <summary>Initial total forms in this category. Stale after queue building starts.</summary>
    public int TotalCount => DueForms.Count + UntriedForms.Count;
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

    // Minimum representation floor for category balancing.
    // Boosts rare patterns (e.g., Masc-ū at 0.3%) toward ~3% selection probability.
    // Note: actual probability depends on category count due to normalization.
    const double MinCategoryFloor = 0.03;

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
        var now = DateTime.UtcNow;  // Capture once for deterministic behavior within this build

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

        // 5. Group forms by category for balanced selection
        var categories = GroupFormsByCategory(type, dueForReview, untriedIds);

        System.Diagnostics.Debug.WriteLine($"[Queue] Categories: {categories.Count} " +
            $"({string.Join(", ", categories.Select(c => $"{c.Key}:{c.Value.TotalCount}"))})");

        // 6. Score due forms within each category by priority
        var scoredByCategory = new Dictionary<string, List<(FormMasteryData Form, double Priority)>>();
        foreach (var (key, cat) in categories)
        {
            scoredByCategory[key] = cat.DueForms
                .Select(f => (Form: f, Priority: CalculatePriority(f, hardCombos, type, now)))
                .OrderByDescending(x => x.Priority)
                .ToList();
        }

        // 7. Build queue with three-stage selection:
        //    Stage 1: Decide globally whether to add new vs review (enforced interval)
        //    Stage 2: Select category from those that can satisfy the decision (weighted by relevant count + 3% floor)
        //    Stage 3: Select form from that category (SRS priority for reviews, random for new)
        //
        // Anti-repetition: Track last lemma AND last combo to avoid consecutive same-word or same-grammar forms.
        // IMPORTANT: Skipped items are NOT discarded - they remain in the pool for future selection.
        int reviewsSinceLastNew = 0;
        int nextNewInterval = _random.Next(NewFormIntervalMin, NewFormIntervalMax + 1);
        int lastLemmaId = 0;
        string lastComboKey = "";

        // Helper: count total available forms across all categories
        int TotalNewAvailable() => categories.Values.Sum(c => c.UntriedForms.Count);
        int TotalReviewsAvailable() => scoredByCategory.Values.Sum(list => list.Count);

        while (queue.Count < count)
        {
            var totalNew = TotalNewAvailable();
            var totalReviews = TotalReviewsAvailable();

            if (totalNew == 0 && totalReviews == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Queue] No more forms available (new={totalNew}, reviews={totalReviews})");
                break;
            }

            // Stage 1: Decide globally - new vs review (counter-based, not modulo)
            bool wantNew = ShouldAddNew(reviewsSinceLastNew, totalNew, nextNewInterval);
            System.Diagnostics.Debug.WriteLine($"[Queue #{queue.Count + 1}] Decision: {(wantNew ? "NEW" : "REVIEW")} " +
                $"(sinceLastNew={reviewsSinceLastNew}, interval={nextNewInterval}, totalNew={totalNew}, totalReviews={totalReviews})");

            // Stage 2: Select category that can satisfy the decision
            // Weight by RELEVANT count: UntriedForms.Count for new, scoredByCategory list size for reviews
            string selectedKey;
            if (wantNew && totalNew > 0)
            {
                // Select from categories with new forms, weighted by untried count
                var withNew = categories
                    .Where(kvp => kvp.Value.UntriedForms.Count > 0)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                selectedKey = SelectCategory(withNew, c => c.UntriedForms.Count);
            }
            else if (totalReviews > 0)
            {
                // Select from categories with reviews, weighted by remaining review count
                var withReviews = categories
                    .Where(kvp => scoredByCategory[kvp.Key].Count > 0)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                selectedKey = SelectCategory(withReviews, c => scoredByCategory[c.Key].Count);
            }
            else if (totalNew > 0)
            {
                // No reviews left, fall back to new forms
                var withNew = categories
                    .Where(kvp => kvp.Value.UntriedForms.Count > 0)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                selectedKey = SelectCategory(withNew, c => c.UntriedForms.Count);
                wantNew = true;
            }
            else
            {
                break;
            }

            if (string.IsNullOrEmpty(selectedKey))
            {
                System.Diagnostics.Debug.WriteLine($"[Queue] No category selected, breaking");
                break;
            }

            var cat = categories[selectedKey];
            System.Diagnostics.Debug.WriteLine($"[Queue #{queue.Count + 1}] Category: {selectedKey} " +
                $"(due={scoredByCategory[selectedKey].Count}, new={cat.UntriedForms.Count})");

            // Stage 3: Select form from category with anti-repetition
            if (wantNew && cat.UntriedForms.Count > 0)
            {
                // Pick random new form with anti-repetition preference
                var newFormId = SelectNewForm(cat.UntriedForms, type, lastLemmaId, lastComboKey);
                cat.UntriedForms.Remove(newFormId);

                lastLemmaId = ExtractLemmaId(newFormId, type);
                lastComboKey = GetComboKey(newFormId, type);
                queue.Add(new PracticeItem(
                    newFormId,
                    type,
                    lastLemmaId,
                    PracticeItemSource.NewForm,
                    Priority: 0.5,  // Medium tier - new forms appear early but don't dominate
                    MasteryLevel: 1
                ));
                reviewsSinceLastNew = 0;
                nextNewInterval = _random.Next(NewFormIntervalMin, NewFormIntervalMax + 1);
                System.Diagnostics.Debug.WriteLine($"[Queue #{queue.Count}] → NEW form {newFormId} (lemma={lastLemmaId}, combo={lastComboKey}), " +
                    $"next new in {nextNewInterval} reviews");
            }
            else
            {
                // Pick review with anti-repetition (items NOT selected remain in list for later)
                var scoredDue = scoredByCategory[selectedKey];
                var (form, priority, selectedIdx) = SelectReview(scoredDue, type, lastLemmaId, lastComboKey);

                // Remove ONLY the selected item - skipped items stay available
                scoredDue.RemoveAt(selectedIdx);

                // Label as DifficultCombo only if this form's grammatical combo is actually in hardCombos
                var comboKey = GetComboKey(form.FormId, type);
                var source = hardCombos.ContainsKey(comboKey)
                    ? PracticeItemSource.DifficultCombo
                    : PracticeItemSource.DueForReview;

                lastLemmaId = ExtractLemmaId(form.FormId, type);
                lastComboKey = comboKey;
                queue.Add(new PracticeItem(
                    form.FormId,
                    type,
                    lastLemmaId,
                    source,
                    priority,
                    form.MasteryLevel
                ));
                reviewsSinceLastNew++;
                System.Diagnostics.Debug.WriteLine($"[Queue #{queue.Count}] → REVIEW form {form.FormId} (lemma={lastLemmaId}, combo={lastComboKey}, " +
                    $"mastery={form.MasteryLevel}, priority={priority:F2}, source={source})");
            }
        }

        LogQueueSummary(queue, type);

        // 8. Shuffle with priority bias to avoid predictable ordering
        return ShuffleWithPriorityBias(queue);
    }

    /// <summary>
    /// Decides whether to add a new form based on reviews since last new.
    /// Uses counter-based logic (not modulo) to ensure consistent new form pacing.
    /// </summary>
    bool ShouldAddNew(int reviewsSinceLastNew, int availableNew, int interval)
    {
        if (availableNew == 0) return false;
        // Add a new form when we've done [interval] reviews since the last new
        return reviewsSinceLastNew >= interval;
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
    /// - Low mastery:   0.0-0.45 (level 1 = 0.45, level 10 = 0.0)
    /// - Hard combo:    0.0-0.3 (boost if this grammatical combo is difficult)
    ///
    /// Example: A level-2 form that's 2 days overdue in a hard combo:
    ///   0.2 (overdue) + 0.4 (low mastery) + 0.2 (hard combo) = 0.8
    /// </summary>
    static double CalculatePriority(FormMasteryData form, Dictionary<string, double> hardCombos, PracticeType type, DateTime now)
    {
        double priority = 0.0;

        // Factor 1: Overdue time (more overdue = higher priority)
        // 0.1 per day overdue, capped at 0.3 (3+ days)
        var overdueDays = (now - form.NextDueUtc).TotalDays;
        priority += Math.Min(0.3, Math.Max(0, overdueDays) * 0.1);

        // Factor 2: Low mastery level (struggling = higher priority)
        // Level 1 → 0.45, Level 10 → 0.0
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
        // For variants and irregulars, check if parent base pattern is enabled
        var checkPattern = pattern.IsBase() ? pattern : pattern.ParentBase();

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

        var includeActive = enabledVoices.Contains(Voice.Active);
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

            foreach (var tense in enabledTenses)
            {
                foreach (var person in enabledPersons)
                {
                    foreach (var number in enabledNumbers)
                    {
                        // Normal (active) forms
                        if (includeActive)
                        {
                            // Skip Present 3rd singular Active - used as a question itself
                            if (tense == Tense.Present && person == Person.Third && number == Number.Singular)
                                continue;

                            if (_verbs.HasAttestedForm(lemma.LemmaId, tense, person, number, reflexive: false))
                            {
                                var formId = Conjugation.ResolveId(lemma.LemmaId, tense, person, number, Voice.Active, 0);
                                formIds.Add(formId);
                            }
                        }

                        // Reflexive forms
                        if (includeReflexive && _verbs.HasReflexive(lemma.LemmaId))
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

    /// <summary>
    /// Extracts lemmaId from a packed formId by removing the trailing grammar digits.
    /// </summary>
    static int ExtractLemmaId(long formId, PracticeType type)
    {
        // FormId is a packed integer: lemmaId in high digits, grammar slots in low digits.
        // Declension: LLLLL_CGNX (5+4 digits) → divide by 10,000
        // Conjugation: LLLLL_TPNRX (5+5 digits) → divide by 100,000
        // Where L=lemma, C=case, G=gender, N=number, X=ending, T=tense, P=person, R=reflexive
        return type switch
        {
            PracticeType.Declension => (int)(formId / 10_000),
            PracticeType.Conjugation => (int)(formId / 100_000),
            _ => 0
        };
    }

    static string GetComboKey(long formId, PracticeType type)
    {
        return type == PracticeType.Declension
            ? Declension.ComboKeyFromId((int)formId)
            : Conjugation.ComboKeyFromId(formId);
    }

    #region Category Balancing

    /// <summary>
    /// Gets the category key for a form ID.
    /// - Nouns: NounPattern name (e.g., "AMasc", "ĀFem")
    /// - Verbs: "Tense_Voice" (e.g., "Present_Active", "Future_Reflexive")
    /// </summary>
    string GetCategoryKey(long formId, PracticeType type)
    {
        if (type == PracticeType.Declension)
        {
            var lemmaId = ExtractLemmaId(formId, type);
            var lemma = _nouns.GetLemma(lemmaId);
            if (lemma == null) return "Unknown";

            var noun = (Noun)lemma.Primary;
            // Use parent base pattern for variants/irregulars to group them together
            var pattern = noun.Pattern.IsBase() ? noun.Pattern : noun.Pattern.ParentBase();
            return pattern.ToString();
        }
        else
        {
            var parsed = Conjugation.ParseId(formId);
            return $"{parsed.Tense}_{parsed.Voice}";
        }
    }

    /// <summary>
    /// Groups forms by grammatical category for balanced selection.
    /// </summary>
    Dictionary<string, CategoryForms> GroupFormsByCategory(
        PracticeType type,
        List<FormMasteryData> dueForms,
        List<long> untriedIds)
    {
        var categories = new Dictionary<string, CategoryForms>();

        // Group due forms by category
        foreach (var form in dueForms)
        {
            var key = GetCategoryKey(form.FormId, type);
            if (!categories.TryGetValue(key, out var cat))
            {
                cat = new CategoryForms(key, [], []);
                categories[key] = cat;
            }
            cat.DueForms.Add(form);
        }

        // Group untried forms by category
        foreach (var formId in untriedIds)
        {
            var key = GetCategoryKey(formId, type);
            if (!categories.TryGetValue(key, out var cat))
            {
                cat = new CategoryForms(key, [], []);
                categories[key] = cat;
            }
            cat.UntriedForms.Add(formId);
        }

        return categories;
    }

    /// <summary>
    /// Selects a category using weighted random selection with minimum floor.
    ///
    /// Each category's weight = max(relevant count, floor weight).
    /// Floor weight = 3% of total relevant forms, ensuring rare patterns appear regularly.
    ///
    /// The getWeight function extracts the relevant count for the selection stage:
    /// - For new form selection: UntriedForms.Count
    /// - For review selection: remaining reviews in scoredByCategory
    /// </summary>
    string SelectCategory(Dictionary<string, CategoryForms> categories, Func<CategoryForms, int> getWeight)
    {
        if (categories.Count == 0) return string.Empty;
        if (categories.Count == 1) return categories.Keys.First();

        var totalRelevant = categories.Values.Sum(getWeight);
        // Use ceiling + min 1 to ensure floor is never 0 for small form counts
        var floorWeight = Math.Max(1, (int)Math.Ceiling(totalRelevant * MinCategoryFloor));

        // Build weighted list with floor, tracking which got boosted
        var weights = new List<(string Key, int Weight, bool Boosted)>();
        foreach (var (key, cat) in categories)
        {
            var natural = getWeight(cat);
            if (natural == 0) continue;
            var weight = Math.Max(natural, floorWeight);
            weights.Add((key, weight, weight > natural));
        }

        if (weights.Count == 0) return string.Empty;

        // Weighted random selection
        var total = weights.Sum(w => w.Weight);
        var roll = _random.Next(total);
        var acc = 0;

        string selected = weights.Last().Key;
        foreach (var (key, weight, _) in weights)
        {
            acc += weight;
            if (roll < acc)
            {
                selected = key;
                break;
            }
        }

        LogFloorBoost(weights, floorWeight, totalRelevant, selected);

        return selected;
    }

    /// <summary>
    /// Selects a new form with anti-repetition preference.
    /// Prioritizes forms that differ in both lemma AND combo from previous.
    /// Falls back gracefully: different-both → different-either → any.
    /// </summary>
    long SelectNewForm(List<long> untried, PracticeType type, int lastLemmaId, string lastComboKey)
    {
        // First try: different lemma AND different combo
        var best = untried
            .Where(id => ExtractLemmaId(id, type) != lastLemmaId && GetComboKey(id, type) != lastComboKey)
            .ToList();
        if (best.Count > 0)
            return best[_random.Next(best.Count)];

        // Second try: different lemma OR different combo
        var good = untried
            .Where(id => ExtractLemmaId(id, type) != lastLemmaId || GetComboKey(id, type) != lastComboKey)
            .ToList();
        if (good.Count > 0)
            return good[_random.Next(good.Count)];

        // Fallback: any
        return untried[_random.Next(untried.Count)];
    }

    /// <summary>
    /// Selects a review form with anti-repetition preference, maintaining priority order.
    /// Returns (form, priority, index) - caller removes by index to preserve other items.
    ///
    /// IMPORTANT: Items not selected are NOT skipped - they remain available for later.
    /// This fixes the original bug where advancing index would discard skipped items.
    /// </summary>
    (FormMasteryData Form, double Priority, int Index) SelectReview(
        List<(FormMasteryData Form, double Priority)> scoredDue,
        PracticeType type,
        int lastLemmaId,
        string lastComboKey)
    {
        // First try: find highest-priority item with different lemma AND different combo
        for (int i = 0; i < scoredDue.Count; i++)
        {
            var (form, priority) = scoredDue[i];
            var lemmaId = ExtractLemmaId(form.FormId, type);
            var comboKey = GetComboKey(form.FormId, type);
            if (lemmaId != lastLemmaId && comboKey != lastComboKey)
                return (form, priority, i);
        }

        // Second try: different lemma OR different combo
        for (int i = 0; i < scoredDue.Count; i++)
        {
            var (form, priority) = scoredDue[i];
            var lemmaId = ExtractLemmaId(form.FormId, type);
            var comboKey = GetComboKey(form.FormId, type);
            if (lemmaId != lastLemmaId || comboKey != lastComboKey)
                return (form, priority, i);
        }

        // Fallback: just take highest priority (first item)
        return (scoredDue[0].Form, scoredDue[0].Priority, 0);
    }

    #endregion

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
    ///
    /// Post-shuffle: Swaps adjacent items with same lemma/combo to maintain variety.
    /// </summary>
    List<PracticeItem> ShuffleWithPriorityBias(List<PracticeItem> queue)
    {
        // Tier boundaries chosen to match priority score distribution:
        // - High (≥0.7): 2+ days overdue OR low mastery + hard combo
        // - Medium (0.4-0.7): typical due reviews, new forms (0.5)
        // - Low (<0.4): well-learned forms slightly past due
        var tier1 = queue.Where(i => i.Priority >= 0.7).OrderBy(_ => _random.Next()).ToList();
        var tier2 = queue.Where(i => i.Priority is >= 0.4 and < 0.7).OrderBy(_ => _random.Next()).ToList();
        var tier3 = queue.Where(i => i.Priority < 0.4).OrderBy(_ => _random.Next()).ToList();

        var result = new List<PracticeItem>();
        int i1 = 0, i2 = 0, i3 = 0;
        int index = 0;

        // Interleave: H-M-L-H-M-L... ensures variety while keeping urgent items frequent.
        // Fallback chain: if preferred tier empty, try next available.
        while (result.Count < queue.Count)
        {
            if (index % 3 == 0 && i1 < tier1.Count)
                result.Add(tier1[i1++]);
            else if (index % 3 == 1 && i2 < tier2.Count)
                result.Add(tier2[i2++]);
            else if (index % 3 == 2 && i3 < tier3.Count)
                result.Add(tier3[i3++]);
            // Fallback chain when preferred tier exhausted
            else if (i1 < tier1.Count)
                result.Add(tier1[i1++]);
            else if (i2 < tier2.Count)
                result.Add(tier2[i2++]);
            else if (i3 < tier3.Count)
                result.Add(tier3[i3++]);

            index++;
        }

        // Post-shuffle: break up adjacent same-lemma or same-combo pairs
        FixAdjacentCollisions(result);

        return result;
    }

    /// <summary>
    /// Swaps adjacent items that share lemma or grammar combo with a later non-colliding item.
    /// Best-effort: if no swap candidate exists, leaves the collision in place.
    /// </summary>
    void FixAdjacentCollisions(List<PracticeItem> items)
    {
        for (int i = 1; i < items.Count; i++)
        {
            var prev = items[i - 1];
            var curr = items[i];

            // Check for collision: same lemma OR same grammar combo
            var prevCombo = GetComboKey(prev.FormId, prev.Type);
            var currCombo = GetComboKey(curr.FormId, curr.Type);

            if (curr.LemmaId == prev.LemmaId || currCombo == prevCombo)
            {
                // Find a swap candidate further ahead that doesn't collide with prev
                for (int j = i + 1; j < items.Count; j++)
                {
                    var candidate = items[j];
                    var candidateCombo = GetComboKey(candidate.FormId, candidate.Type);

                    // Candidate must not collide with prev
                    if (candidate.LemmaId != prev.LemmaId && candidateCombo != prevCombo)
                    {
                        (items[i], items[j]) = (items[j], items[i]);
                        break;
                    }
                }
            }
        }
    }

    #region Debug Logging

    /// <summary>
    /// Logs queue summary with category distribution. No-op in release builds.
    /// </summary>
    void LogQueueSummary(List<PracticeItem> queue, PracticeType type)
    {
#if DEBUG
        var newCount = queue.Count(q => q.Source == PracticeItemSource.NewForm);
        var reviewCount = queue.Count - newCount;
        var categoryDist = queue
            .GroupBy(q => GetCategoryKey(q.FormId, type))
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}:{g.Count()}")
            .Take(8);
        System.Diagnostics.Debug.WriteLine($"[Queue] Built {queue.Count} items: {newCount} new, {reviewCount} reviews");
        System.Diagnostics.Debug.WriteLine($"[Queue] Category distribution: {string.Join(", ", categoryDist)}");
#endif
    }

    /// <summary>
    /// Logs when floor boost is applied to rare categories. No-op in release builds.
    /// </summary>
    static void LogFloorBoost(List<(string Key, int Weight, bool Boosted)> weights, int floorWeight, int totalForms, string selected)
    {
#if DEBUG
        var boosted = weights.Where(w => w.Boosted).Select(w => w.Key).ToList();
        if (boosted.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Queue] Floor boost: {boosted.Count} categories boosted " +
                $"(floor={floorWeight}, total={totalForms}), selected={selected}");
        }
#endif
    }

    #endregion
}
