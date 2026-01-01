using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Repositories;
using PaliPractice.Services.UserData;

namespace PaliPractice.Services.Practice;

/// <summary>
/// Common data for form mastery used in queue building.
/// Extracted from type-specific entities (NounsFormMastery, VerbsFormMastery).
/// </summary>
record FormMasteryData(long FormId, int MasteryLevel, DateTime NextDueUtc);

/// <summary>
/// Builds a practice queue using a prebuilt slot-based approach:
///
/// 1. SLOT PLAN: Pre-determine new vs review pattern (1 new every 4-6 reviews).
/// 2. LEVEL BUCKETS: Rotate through mastery levels (1-2, 3-4, 5-6, 7-8, 9-10)
///    to mix difficulty instead of always showing hardest items first.
/// 3. SPACING CONSTRAINTS: Enforce lemma, combo, and category gaps with graceful degradation.
///
/// The queue is deterministically built for reproducibility (seeded by date + type).
/// No post-shuffle needed - spacing is handled during selection.
///
/// SPACING AXES (in priority order for fallback):
/// - Lemma: Same word shouldn't repeat within N items (prevents "deva, deva, deva...")
/// - Combo: Same grammatical combination shouldn't repeat (prevents "nom_sg, nom_sg...")
/// - Category: Same inflection pattern shouldn't dominate (prevents "all -a masc" or "all Class I verbs")
///
/// FALLBACK CHAIN: When ideal spacing can't be achieved, constraints are relaxed in order:
/// all three → lemma+combo → combo only → lemma only → any
/// This ensures small pools (e.g., 2 lemmas, 1 combo) still produce valid queues.
/// </summary>
public class PracticeQueueBuilder : IPracticeQueueBuilder
{
    readonly UserDataRepository _userData;
    readonly NounRepository _nouns;
    readonly VerbRepository _verbs;
    Random _random = new();  // Re-seeded per build for determinism

    // Introduce new forms gradually: 1 new form every 4-6 reviews.
    // Variable interval prevents predictable patterns (vs fixed "every 5th").
    const int NewFormIntervalMin = 4;
    const int NewFormIntervalMax = 6;

    // Ideal spacing gaps (scaled down when pool is small via EffectiveGap).
    // These values assume a reasonably diverse pool; with 2 lemmas, gap becomes 1 (ABAB pattern).
    const int IdealLemmaGap = 8;   // Don't show same word within 8 items
    const int IdealComboGap = 6;   // Don't show same case/number or tense/person within 6 items
    const int IdealCategoryGap = 3; // Don't show same inflection pattern within 3 items

    // Level buckets for round-robin difficulty mixing.
    // Instead of always showing struggling items first (which is discouraging),
    // we rotate through buckets to interleave easy wins with challenging items.
    static readonly (int Min, int Max)[] LevelBuckets =
    [
        (1, 2),   // Struggling - items user finds difficult
        (3, 4),   // Learning - includes level 4, the default for new items
        (5, 6),   // Developing - gaining confidence
        (7, 8),   // Strong - approaching mastery
        (9, 10),  // Practiced - near retirement (level 11 = retired forever)
    ];

    public PracticeQueueBuilder(IDatabaseService db)
    {
        _userData = db.UserData;
        _nouns = db.Nouns;
        _verbs = db.Verbs;
    }

    public List<PracticeItem> BuildQueue(PracticeType type, int count)
    {
        System.Diagnostics.Debug.WriteLine($"[Queue] BuildQueue({type}, {count})");

        // Deterministic seeding: same day + type = same queue order.
        // This ensures reproducibility for testing and prevents queue "churn" during a session.
        // We use a custom hash because HashCode.Combine is not stable across processes/restarts.
        // The 397 multiplier is a common prime that provides good distribution.
        var today = DateTime.UtcNow.Date;
        var daysSinceEpoch = (int)(today - DateTime.UnixEpoch).TotalDays;
        var seed = unchecked(daysSinceEpoch * 397 ^ (int)type);
        _random = new Random(seed);
        System.Diagnostics.Debug.WriteLine($"[Queue] Seeded with {seed} (date={DateTime.UtcNow.Date:yyyy-MM-dd}, type={type})");

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

        // 2. Get practiced form IDs and categorize
        var practicedIds = type == PracticeType.Declension
            ? _userData.GetPracticedNounFormIds()
            : _userData.GetPracticedVerbFormIds();

        var dueForReview = GetDueForms(type, eligibleSet);
        var untriedIds = eligibleFormIds
            .Where(id => !practicedIds.Contains(id))
            .ToList();

        // Shuffle untried for variety (deterministic via seeded random)
        ShuffleList(untriedIds);

        System.Diagnostics.Debug.WriteLine($"[Queue] Due: {dueForReview.Count}, Untried: {untriedIds.Count}");

        // 3. Build slot plan: determines new vs review for each position
        var slotPlan = BuildSlotPlan(count, untriedIds.Count, dueForReview.Count);
        var newSlotCount = slotPlan.Count(isNew => isNew);
        var reviewSlotCount = slotPlan.Count - newSlotCount;
        System.Diagnostics.Debug.WriteLine($"[Queue] Slot plan: {newSlotCount} new, {reviewSlotCount} review");

        // 4. Group due forms by level bucket, sorted by overdue time within each bucket.
        // Sorting by NextDueUtc ascending means most overdue items come first within their bucket.
        // This preserves urgency: if you have 3 items in bucket [1-2], the one due 5 days ago
        // gets picked before the one due yesterday, even though we rotate through buckets.
        var levelBuckets = GroupByLevelBucket(dueForReview);
        foreach (var bucket in levelBuckets)
            bucket.Sort((a, b) => a.NextDueUtc.CompareTo(b.NextDueUtc));

        LogLevelBuckets(levelBuckets);

        // 5. Calculate effective gaps based on pool diversity.
        // We use union of due + untried (not sum) to get true unique counts.
        // Category cache is built here because GetCategoryKey requires GetLemma() DB lookup.
        // Cache is keyed by lemmaId since all forms of a lemma share the same pattern.
        var categoryCache = new Dictionary<int, string>();
        var uniqueLemmas = dueForReview.Select(f => ExtractLemmaId(f.FormId, type))
            .Concat(untriedIds.Select(id => ExtractLemmaId(id, type)))
            .Distinct()
            .Count();
        var uniqueCombos = dueForReview.Select(f => GetComboKey(f.FormId, type))
            .Concat(untriedIds.Select(id => GetComboKey(id, type)))
            .Distinct()
            .Count();
        var uniqueCategories = dueForReview.Select(f => GetCachedCategoryKey(ExtractLemmaId(f.FormId, type), type, categoryCache))
            .Concat(untriedIds.Select(id => GetCachedCategoryKey(ExtractLemmaId(id, type), type, categoryCache)))
            .Distinct()
            .Count();

        var lemmaGap = EffectiveGap(IdealLemmaGap, uniqueLemmas);
        var comboGap = EffectiveGap(IdealComboGap, uniqueCombos);
        var categoryGap = EffectiveGap(IdealCategoryGap, uniqueCategories);
        System.Diagnostics.Debug.WriteLine($"[Queue] Gaps: lemma={lemmaGap}, combo={comboGap}, category={categoryGap}");

        // 6. Track last-seen positions for spacing constraints
        var lastLemmaPos = new Dictionary<int, int>();
        var lastComboPos = new Dictionary<string, int>();
        var lastCategoryPos = new Dictionary<string, int>();

        // 7. Fill slots according to plan
        int newIdx = 0;
        int bucketRound = 0;  // Which level bucket to try next
        var bucketIndices = new int[LevelBuckets.Length];  // Current index in each bucket

        for (int pos = 0; pos < slotPlan.Count; pos++)
        {
            bool isNewSlot = slotPlan[pos];

            PracticeItem? item = null;

            if (isNewSlot && newIdx < untriedIds.Count)
            {
                // Fill new slot
                item = TakeNewForm(untriedIds, ref newIdx, type, pos, lastLemmaPos, lastComboPos, lastCategoryPos, categoryCache, lemmaGap, comboGap, categoryGap);
            }
            else if (!isNewSlot || newIdx >= untriedIds.Count)
            {
                // Fill review slot (or fallback if no new forms left)
                item = TakeReviewForm(levelBuckets, bucketIndices, ref bucketRound, type, pos, lastLemmaPos, lastComboPos, lastCategoryPos, categoryCache, lemmaGap, comboGap, categoryGap);
            }

            if (item == null)
            {
                // Try the other type if primary failed
                if (isNewSlot)
                    item = TakeReviewForm(levelBuckets, bucketIndices, ref bucketRound, type, pos, lastLemmaPos, lastComboPos, lastCategoryPos, categoryCache, lemmaGap, comboGap, categoryGap);
                else if (newIdx < untriedIds.Count)
                    item = TakeNewForm(untriedIds, ref newIdx, type, pos, lastLemmaPos, lastComboPos, lastCategoryPos, categoryCache, lemmaGap, comboGap, categoryGap);
            }

            if (item == null)
            {
                System.Diagnostics.Debug.WriteLine($"[Queue] No more forms at position {pos}");
                break;
            }

            queue.Add(item);

            // Update tracking for all three spacing axes (use cached category)
            lastLemmaPos[item.LemmaId] = pos;
            lastComboPos[GetComboKey(item.FormId, type)] = pos;
            lastCategoryPos[GetCachedCategoryKey(item.LemmaId, type, categoryCache)] = pos;
        }

        LogQueueSummary(queue, type);
        return queue;
    }

    #region Slot Planning

    /// <summary>
    /// Builds a deterministic slot plan: true = new form, false = review.
    /// Places new forms every 4-6 reviews (variable but deterministic via seeded random).
    /// </summary>
    List<bool> BuildSlotPlan(int count, int totalNew, int totalReviews)
    {
        var slots = new List<bool>(count);
        if (totalNew == 0 && totalReviews == 0)
            return slots;

        int newPlaced = 0;
        int reviewPlaced = 0;
        int reviewsSinceNew = 0;
        int nextInterval = _random.Next(NewFormIntervalMin, NewFormIntervalMax + 1);

        for (int i = 0; i < count; i++)
        {
            bool placeNew = false;

            if (totalReviews == 0 || reviewPlaced >= totalReviews)
            {
                // No reviews left, must use new
                placeNew = newPlaced < totalNew;
            }
            else if (totalNew == 0 || newPlaced >= totalNew)
            {
                // No new left, must use review
                placeNew = false;
            }
            else
            {
                // Both available: use interval logic
                placeNew = reviewsSinceNew >= nextInterval;
            }

            slots.Add(placeNew);

            if (placeNew)
            {
                newPlaced++;
                reviewsSinceNew = 0;
                nextInterval = _random.Next(NewFormIntervalMin, NewFormIntervalMax + 1);
            }
            else
            {
                reviewPlaced++;
                reviewsSinceNew++;
            }

            // Stop if we've exhausted both pools
            if (newPlaced >= totalNew && reviewPlaced >= totalReviews)
                break;
        }

        return slots;
    }

    #endregion

    #region Level Bucket Management

    /// <summary>
    /// Groups due forms into 5 level buckets for round-robin selection.
    /// </summary>
    static List<List<FormMasteryData>> GroupByLevelBucket(List<FormMasteryData> dueForms)
    {
        var buckets = new List<List<FormMasteryData>>(LevelBuckets.Length);
        for (int i = 0; i < LevelBuckets.Length; i++)
            buckets.Add([]);

        foreach (var form in dueForms)
        {
            for (int i = 0; i < LevelBuckets.Length; i++)
            {
                var (min, max) = LevelBuckets[i];
                if (form.MasteryLevel >= min && form.MasteryLevel <= max)
                {
                    buckets[i].Add(form);
                    break;
                }
            }
        }

        return buckets;
    }

    /// <summary>
    /// Takes next review form using round-robin through level buckets.
    ///
    /// ROUND-ROBIN RATIONALE: Instead of draining all struggling items (L1-2) first,
    /// we rotate through buckets: L1-2 → L3-4 → L5-6 → L7-8 → L9-10 → L1-2...
    /// This interleaves difficulty levels, providing "easy wins" between challenges.
    ///
    /// URGENCY PRESERVATION: Within each bucket, items are sorted by NextDueUtc (most
    /// overdue first). So we get diversity across levels while still prioritizing
    /// the most urgent items within each level group.
    ///
    /// SWAP TECHNIQUE: When FindFormWithConstraints picks an item at index N > startIdx
    /// (skipping items that violate spacing), we swap it to startIdx and advance.
    /// This avoids O(n) removal while maintaining bucket order for remaining items.
    /// </summary>
    PracticeItem? TakeReviewForm(
        List<List<FormMasteryData>> buckets,
        int[] bucketIndices,
        ref int bucketRound,
        PracticeType type,
        int position,
        Dictionary<int, int> lastLemmaPos,
        Dictionary<string, int> lastComboPos,
        Dictionary<string, int> lastCategoryPos,
        Dictionary<int, string> categoryCache,
        int lemmaGap,
        int comboGap,
        int categoryGap)
    {
        // Try each bucket in round-robin order, starting from bucketRound
        for (int attempt = 0; attempt < LevelBuckets.Length; attempt++)
        {
            int bucketIdx = (bucketRound + attempt) % LevelBuckets.Length;
            var bucket = buckets[bucketIdx];
            int startIdx = bucketIndices[bucketIdx];

            if (startIdx >= bucket.Count)
                continue;  // Bucket exhausted, try next

            // Try to find a form satisfying spacing constraints
            var (form, foundIdx) = FindFormWithConstraints(
                bucket, startIdx, type, position, lastLemmaPos, lastComboPos, lastCategoryPos, categoryCache, lemmaGap, comboGap, categoryGap);

            if (form != null)
            {
                // Swap selected item to the "consumed" position and advance the index.
                // This marks the item as used without removing it from the list.
                if (foundIdx != startIdx)
                    (bucket[startIdx], bucket[foundIdx]) = (bucket[foundIdx], bucket[startIdx]);
                bucketIndices[bucketIdx]++;

                // Advance round-robin so next call starts with the following bucket
                bucketRound = (bucketIdx + 1) % LevelBuckets.Length;

                return new PracticeItem(
                    form.FormId,
                    type,
                    ExtractLemmaId(form.FormId, type),
                    PracticeItemSource.DueForReview,
                    Priority: 0.5,  // Not used for ordering; bucket rotation handles difficulty mixing
                    form.MasteryLevel
                );
            }
        }

        return null;  // All buckets exhausted
    }

    /// <summary>
    /// Finds a form satisfying spacing constraints using a 5-pass fallback chain.
    ///
    /// The fallback order is designed for graceful degradation:
    /// - Category is dropped first (least jarring to repeat a pattern group)
    /// - Combo-only before lemma-only handles beginner settings with few lemmas
    ///   (e.g., practicing just 2 words: combo diversity matters more than lemma diversity)
    /// - Final pass accepts anything to ensure we always return a form if available
    ///
    /// WHY THIS ORDER:
    /// Consider a beginner practicing only "deva" and "dhamma" (2 lemmas, 16 combos).
    /// - lemmaGap becomes 1 (ABAB pattern) via EffectiveGap
    /// - comboGap stays at 6 (enough combos)
    /// If we prioritized lemma-only over combo-only, we'd get "nom_sg, nom_sg, nom_sg..."
    /// By checking combo-only first, we maintain grammatical variety even when lemma pool is tiny.
    /// </summary>
    (FormMasteryData? Form, int Index) FindFormWithConstraints(
        List<FormMasteryData> forms,
        int startIdx,
        PracticeType type,
        int position,
        Dictionary<int, int> lastLemmaPos,
        Dictionary<string, int> lastComboPos,
        Dictionary<string, int> lastCategoryPos,
        Dictionary<int, string> categoryCache,
        int lemmaGap,
        int comboGap,
        int categoryGap)
    {
        // Scan up to 100 items to find one satisfying constraints.
        // 100 is generous enough to find good candidates without being a performance issue.
        // In practice, pass 1 or 2 usually succeeds within the first few items.
        int remaining = forms.Count - startIdx;
        int scanLimit = Math.Min(100, remaining);
        int endIdx = startIdx + scanLimit;

        // Gap check helpers: return true if constraint is satisfied.
        // "Not in dictionary" means never seen → always OK.
        // "In dictionary" means check if enough positions have passed since last use.
        bool LemmaOk(int lemmaId) =>
            !lastLemmaPos.TryGetValue(lemmaId, out var lp) || position - lp >= lemmaGap;
        bool ComboOk(string comboKey) =>
            !lastComboPos.TryGetValue(comboKey, out var cp) || position - cp >= comboGap;
        bool CategoryOk(string categoryKey) =>
            !lastCategoryPos.TryGetValue(categoryKey, out var cat) || position - cat >= categoryGap;

        // Pass 1: Ideal case - all three constraints satisfied
        for (int i = startIdx; i < endIdx; i++)
        {
            var form = forms[i];
            var lemmaId = ExtractLemmaId(form.FormId, type);
            var comboKey = GetComboKey(form.FormId, type);
            var categoryKey = GetCachedCategoryKey(lemmaId, type, categoryCache);
            if (LemmaOk(lemmaId) && ComboOk(comboKey) && CategoryOk(categoryKey))
                return (form, i);
        }

        // Pass 2: Drop category (same pattern group is less jarring than same word/combo)
        for (int i = startIdx; i < endIdx; i++)
        {
            var form = forms[i];
            var lemmaId = ExtractLemmaId(form.FormId, type);
            var comboKey = GetComboKey(form.FormId, type);
            if (LemmaOk(lemmaId) && ComboOk(comboKey))
                return (form, i);
        }

        // Pass 3: Combo only - important for small lemma pools (e.g., beginner with 2 words)
        // When lemmaGap is 0/1, combo diversity becomes the primary differentiation axis
        for (int i = startIdx; i < endIdx; i++)
        {
            var form = forms[i];
            var comboKey = GetComboKey(form.FormId, type);
            if (ComboOk(comboKey))
                return (form, i);
        }

        // Pass 4: Lemma only - fallback when even combo diversity can't be achieved
        for (int i = startIdx; i < endIdx; i++)
        {
            var form = forms[i];
            var lemmaId = ExtractLemmaId(form.FormId, type);
            if (LemmaOk(lemmaId))
                return (form, i);
        }

        // Pass 5: Any
        if (startIdx < forms.Count)
            return (forms[startIdx], startIdx);

        return (null, -1);
    }

    #endregion

    #region New Form Selection

    /// <summary>
    /// Takes next new form with spacing constraints.
    /// Uses same fallback chain as FindFormWithConstraints.
    /// </summary>
    PracticeItem? TakeNewForm(
        List<long> untried,
        ref int idx,
        PracticeType type,
        int position,
        Dictionary<int, int> lastLemmaPos,
        Dictionary<string, int> lastComboPos,
        Dictionary<string, int> lastCategoryPos,
        Dictionary<int, string> categoryCache,
        int lemmaGap,
        int comboGap,
        int categoryGap)
    {
        if (idx >= untried.Count)
            return null;

        // Scan up to 100 items to find one satisfying constraints
        int remaining = untried.Count - idx;
        int scanLimit = Math.Min(100, remaining);
        int endIdx = idx + scanLimit;

        bool LemmaOk(int lemmaId) =>
            !lastLemmaPos.TryGetValue(lemmaId, out var lp) || position - lp >= lemmaGap;
        bool ComboOk(string comboKey) =>
            !lastComboPos.TryGetValue(comboKey, out var cp) || position - cp >= comboGap;
        bool CategoryOk(string categoryKey) =>
            !lastCategoryPos.TryGetValue(categoryKey, out var cat) || position - cat >= categoryGap;

        // Pass 1: All three constraints
        for (int i = idx; i < endIdx; i++)
        {
            var formId = untried[i];
            var lemmaId = ExtractLemmaId(formId, type);
            var comboKey = GetComboKey(formId, type);
            var categoryKey = GetCachedCategoryKey(lemmaId, type, categoryCache);
            if (LemmaOk(lemmaId) && ComboOk(comboKey) && CategoryOk(categoryKey))
            {
                if (i != idx)
                    (untried[idx], untried[i]) = (untried[i], untried[idx]);
                idx++;
                return new PracticeItem(formId, type, lemmaId, PracticeItemSource.NewForm, 0.5, MasteryLevel: 1);
            }
        }

        // Pass 2: Lemma + combo (drop category)
        for (int i = idx; i < endIdx; i++)
        {
            var formId = untried[i];
            var lemmaId = ExtractLemmaId(formId, type);
            var comboKey = GetComboKey(formId, type);
            if (LemmaOk(lemmaId) && ComboOk(comboKey))
            {
                if (i != idx)
                    (untried[idx], untried[i]) = (untried[i], untried[idx]);
                idx++;
                return new PracticeItem(formId, type, lemmaId, PracticeItemSource.NewForm, 0.5, MasteryLevel: 1);
            }
        }

        // Pass 3: Combo only (for small lemma pools)
        for (int i = idx; i < endIdx; i++)
        {
            var formId = untried[i];
            var lemmaId = ExtractLemmaId(formId, type);
            var comboKey = GetComboKey(formId, type);
            if (ComboOk(comboKey))
            {
                if (i != idx)
                    (untried[idx], untried[i]) = (untried[i], untried[idx]);
                idx++;
                return new PracticeItem(formId, type, lemmaId, PracticeItemSource.NewForm, 0.5, MasteryLevel: 1);
            }
        }

        // Pass 4: Lemma only
        for (int i = idx; i < endIdx; i++)
        {
            var formId = untried[i];
            var lemmaId = ExtractLemmaId(formId, type);
            if (LemmaOk(lemmaId))
            {
                if (i != idx)
                    (untried[idx], untried[i]) = (untried[i], untried[idx]);
                idx++;
                return new PracticeItem(formId, type, lemmaId, PracticeItemSource.NewForm, 0.5, MasteryLevel: 1);
            }
        }

        // Pass 5: Any
        if (idx < untried.Count)
        {
            var formId = untried[idx];
            var lemmaId = ExtractLemmaId(formId, type);
            idx++;
            return new PracticeItem(formId, type, lemmaId, PracticeItemSource.NewForm, 0.5, MasteryLevel: 1);
        }

        return null;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Calculates effective gap scaled to pool size.
    ///
    /// You can't space items farther apart than the pool allows. Examples:
    /// - 1 unique item → gap=0 (can't avoid repeating the only item)
    /// - 2 unique items → gap=1 achieves ABAB pattern (best possible)
    /// - 3 unique items → gap=2 achieves ABCABC pattern
    /// - 10+ unique items → can use full idealGap
    ///
    /// The formula uniqueCount-1 represents the maximum gap achievable:
    /// with N items, you can have at most N-1 different items between repetitions.
    /// </summary>
    static int EffectiveGap(int idealGap, int uniqueCount)
    {
        if (uniqueCount <= 1) return 0;  // No spacing possible with 0 or 1 items
        return Math.Min(idealGap, uniqueCount - 1);
    }

    /// <summary>
    /// Fisher-Yates shuffle using instance random (deterministic when seeded).
    /// </summary>
    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    #endregion

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
    /// Uses the GrammarDivisor constants defined in Declension/Conjugation classes.
    /// </summary>
    static int ExtractLemmaId(long formId, PracticeType type)
    {
        // FormId is a packed integer: lemmaId in high digits, grammar slots in low digits.
        // Declension: LLLLL_CGNX (5+4 digits) → divide by GrammarDivisor (10,000)
        // Conjugation: LLLLL_TPNRX (5+5 digits) → divide by GrammarDivisor (100,000)
        return type switch
        {
            PracticeType.Declension => (int)(formId / Declension.LemmaDivisor),
            PracticeType.Conjugation => (int)(formId / Conjugation.LemmaDivisor),
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
    /// Gets the category key for a form ID with memoization.
    ///
    /// WHY CACHE: For nouns, GetCategoryKey calls _nouns.GetLemma() which does a DB lookup.
    /// During queue building, we scan up to 100 forms per slot across 5 passes, so the same
    /// lemmaId may be looked up dozens of times. Caching by lemmaId eliminates this overhead.
    ///
    /// WHY BY LEMMA: All forms of a noun share the same pattern (category), so we cache by
    /// lemmaId rather than formId. This also means the cache is smaller and has better hit rates.
    ///
    /// Both nouns and verbs require GetLemma() DB lookups to retrieve their pattern.
    /// </summary>
    string GetCachedCategoryKey(int lemmaId, PracticeType type, Dictionary<int, string> cache)
    {
        if (cache.TryGetValue(lemmaId, out var cached))
            return cached;

        var key = GetCategoryKey(lemmaId, type);
        cache[lemmaId] = key;
        return key;
    }

    /// <summary>
    /// Gets the category key for a lemma (uncached - prefer GetCachedCategoryKey).
    /// Category = inflection pattern, which is inherent to the lemma and determines endings.
    /// - Nouns: NounPattern (e.g., "AMasc", "ĀFem")
    /// - Verbs: VerbPattern (e.g., "Class1", "Irregular")
    /// </summary>
    string GetCategoryKey(int lemmaId, PracticeType type)
    {
        if (type == PracticeType.Declension)
        {
            var lemma = _nouns.GetLemma(lemmaId);
            if (lemma == null)
                throw new InvalidOperationException($"Noun lemma {lemmaId} not found in database");

            var noun = (Noun)lemma.Primary;
            // Use parent base pattern for variants/irregulars to group them together.
            // E.g., "a_masc_irregular" → "AMasc" so all -a masculine nouns cluster.
            var pattern = noun.Pattern.IsBase() ? noun.Pattern : noun.Pattern.ParentBase();
            return pattern.ToString();
        }
        else
        {
            var lemma = _verbs.GetLemma(lemmaId);
            if (lemma == null)
                throw new InvalidOperationException($"Verb lemma {lemmaId} not found in database");

            var verb = (Verb)lemma.Primary;
            // Use parent regular pattern for irregulars to group them together.
            var pattern = verb.Pattern.IsIrregular() ? verb.Pattern.ParentRegular() : verb.Pattern;
            return pattern.ToString();
        }
    }

    #region Debug Logging

    /// <summary>
    /// Logs level bucket distribution. No-op in release builds.
    /// </summary>
    static void LogLevelBuckets(List<List<FormMasteryData>> buckets)
    {
#if DEBUG
        var counts = buckets.Select((b, i) =>
        {
            var (min, max) = LevelBuckets[i];
            return $"L{min}-{max}:{b.Count}";
        });
        System.Diagnostics.Debug.WriteLine($"[Queue] Level buckets: {string.Join(", ", counts)}");
#endif
    }

    /// <summary>
    /// Logs queue summary. No-op in release builds.
    /// </summary>
    void LogQueueSummary(List<PracticeItem> queue, PracticeType type)
    {
#if DEBUG
        var newCount = queue.Count(q => q.Source == PracticeItemSource.NewForm);
        var reviewCount = queue.Count - newCount;

        // Group by mastery level
        var levelDist = queue
            .Where(q => q.Source != PracticeItemSource.NewForm)
            .GroupBy(q => q.MasteryLevel)
            .OrderBy(g => g.Key)
            .Select(g => $"L{g.Key}:{g.Count()}");

        System.Diagnostics.Debug.WriteLine($"[Queue] Built {queue.Count} items: {newCount} new, {reviewCount} reviews");
        if (reviewCount > 0)
            System.Diagnostics.Debug.WriteLine($"[Queue] Level distribution: {string.Join(", ", levelDist)}");
#endif
    }

    #endregion
}
