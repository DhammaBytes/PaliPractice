using PaliPractice.Models.Inflection;
using PaliPractice.Services.UserData;
using PaliPractice.Services.UserData.Entities;

namespace PaliPractice.Services.Practice;

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
    readonly IUserDataService _userData;
    readonly IDatabaseService _trainingDb;
    readonly Random _random = new();

    // Introduce new forms gradually: 1 new form every 4-6 reviews.
    // Randomized interval prevents predictable patterns.
    const int NewFormIntervalMin = 4;
    const int NewFormIntervalMax = 6;

    public PracticeQueueBuilder(IUserDataService userData, IDatabaseService trainingDb)
    {
        _userData = userData;
        _trainingDb = trainingDb;
    }

    public List<PracticeItem> BuildQueue(PracticeType type, int count)
    {
        var queue = new List<PracticeItem>();

        // 1. Get all eligible form IDs (corpus-attested, matching settings)
        var eligibleFormIds = GetEligibleFormIds(type);
        var eligibleSet = eligibleFormIds.ToHashSet();

        if (eligibleFormIds.Count == 0)
            return queue;

        // 2. Get practiced form IDs
        var practicedIds = _userData.GetPracticedFormIds(type);

        // 3. Categorize into buckets
        var dueForReview = _userData.GetDueForms(type, limit: 500)
            .Where(f => eligibleSet.Contains(f.FormId))
            .ToList();

        var untriedIds = eligibleFormIds
            .Where(id => !practicedIds.Contains(id))
            .ToList();

        // 4. Get difficulty weights for prioritization
        var hardCombos = _userData.GetHardestCombinations(type, limit: 20)
            .ToDictionary(c => c.ComboKey, c => c.DifficultyScore);

        // 5. Score and sort due forms by priority
        var scoredDue = dueForReview
            .Select(f => new
            {
                Form = f,
                Priority = CalculatePriority(f, hardCombos, type)
            })
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
                var item = scoredDue[reviewIndex];
                queue.Add(new PracticeItem(
                    item.Form.FormId,
                    type,
                    ExtractLemmaId(item.Form.FormId, type),
                    item.Priority > 0.7
                        ? PracticeItemSource.DifficultCombo
                        : PracticeItemSource.DueForReview,
                    item.Priority,
                    item.Form.MasteryLevel
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
    double CalculatePriority(FormMastery form, Dictionary<string, double> hardCombos, PracticeType type)
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
        // Load settings
        var casesStr = _userData.GetSetting(SettingsKeys.DeclensionCases, SettingsKeys.DefaultDeclensionCases);
        var gendersStr = _userData.GetSetting(SettingsKeys.DeclensionGenders, SettingsKeys.DefaultDeclensionGenders);
        var numbersStr = _userData.GetSetting(SettingsKeys.DeclensionNumbers, SettingsKeys.DefaultNumbers);
        var minRank = _userData.GetSetting(SettingsKeys.DeclensionLemmaMin, SettingsKeys.DefaultLemmaMin);
        var maxRank = _userData.GetSetting(SettingsKeys.DeclensionLemmaMax, SettingsKeys.DefaultLemmaMax);

        var enabledCases = ParseEnumList<Case>(casesStr);
        var enabledGenders = ParseEnumList<Gender>(gendersStr);
        var enabledNumbers = ParseEnumList<Number>(numbersStr);

        var formIds = new List<long>();

        // Get nouns by rank
        var nouns = _trainingDb.GetNounsByRank(minRank, maxRank);

        foreach (var noun in nouns)
        {
            // Skip if noun's gender is not enabled
            if (!enabledGenders.Contains(noun.Gender))
                continue;

            foreach (var @case in enabledCases)
            {
                foreach (var number in enabledNumbers)
                {
                    // Check if this combination has corpus attestation
                    if (_trainingDb.HasAttestedNounForm(noun.LemmaId, @case, noun.Gender, number))
                    {
                        // Use EndingId=0 for combination reference
                        var formId = Declension.ResolveId(noun.LemmaId, @case, noun.Gender, number, 0);
                        formIds.Add(formId);
                    }
                }
            }
        }

        return formIds;
    }

    List<long> GetEligibleConjugationFormIds()
    {
        // Load settings
        var tensesStr = _userData.GetSetting(SettingsKeys.ConjugationTenses, SettingsKeys.DefaultConjugationTenses);
        var personsStr = _userData.GetSetting(SettingsKeys.ConjugationPersons, SettingsKeys.DefaultConjugationPersons);
        var numbersStr = _userData.GetSetting(SettingsKeys.ConjugationNumbers, SettingsKeys.DefaultNumbers);
        var reflexiveSetting = _userData.GetSetting(SettingsKeys.ConjugationReflexive, SettingsKeys.DefaultReflexive);
        var minRank = _userData.GetSetting(SettingsKeys.ConjugationLemmaMin, SettingsKeys.DefaultLemmaMin);
        var maxRank = _userData.GetSetting(SettingsKeys.ConjugationLemmaMax, SettingsKeys.DefaultLemmaMax);

        var enabledTenses = ParseEnumList<Tense>(tensesStr);
        var enabledPersons = ParseEnumList<Person>(personsStr);
        var enabledNumbers = ParseEnumList<Number>(numbersStr);
        var includeActive = reflexiveSetting is "both" or "active";
        var includeReflexive = reflexiveSetting is "both" or "reflexive";

        var formIds = new List<long>();

        // Get verbs by rank
        var verbs = _trainingDb.GetVerbsByRank(minRank, maxRank);

        foreach (var verb in verbs)
        {
            var hasReflexive = _trainingDb.VerbHasReflexive(verb.LemmaId);

            foreach (var tense in enabledTenses)
            {
                foreach (var person in enabledPersons)
                {
                    foreach (var number in enabledNumbers)
                    {
                        // Active forms
                        if (includeActive)
                        {
                            if (_trainingDb.HasAttestedVerbForm(verb.LemmaId, tense, person, number, reflexive: false))
                            {
                                var formId = Conjugation.ResolveId(verb.LemmaId, tense, person, number, reflexive: false, 0);
                                formIds.Add(formId);
                            }
                        }

                        // Reflexive forms
                        if (includeReflexive && hasReflexive)
                        {
                            if (_trainingDb.HasAttestedVerbForm(verb.LemmaId, tense, person, number, reflexive: true))
                            {
                                var formId = Conjugation.ResolveId(verb.LemmaId, tense, person, number, reflexive: true, 0);
                                formIds.Add(formId);
                            }
                        }
                    }
                }
            }
        }

        return formIds;
    }

    static List<T> ParseEnumList<T>(string csv) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        return csv.Split(',')
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out _))
            .Select(s => (T)Enum.ToObject(typeof(T), int.Parse(s)))
            .ToList();
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
        var tier2 = queue.Where(i => i.Priority >= 0.4 && i.Priority < 0.7).OrderBy(_ => _random.Next()).ToList();
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
