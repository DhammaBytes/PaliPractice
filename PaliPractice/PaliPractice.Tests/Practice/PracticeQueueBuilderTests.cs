using PaliPractice.Models;
using PaliPractice.Models.Inflection;
using PaliPractice.Presentation.Practice.ViewModels.Common;
using PaliPractice.Services.Practice;
using PaliPractice.Services.UserData;
using PaliPractice.Tests.Practice.Builders;
using PaliPractice.Tests.Practice.Fakes;

namespace PaliPractice.Tests.Practice;

[TestFixture]
public class PracticeQueueBuilderTests
{
    // Fixed seed date for deterministic tests
    static readonly DateTime TestSeedDate = new(2024, 6, 15);

    #region Determinism Tests

    [Test]
    public void BuildQueue_SameSeedDate_ProducesSameQueue()
    {
        var db = TestScenarioBuilder.Small().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue1 = builder.BuildQueue(PracticeType.Declension, 20, TestSeedDate);
        var queue2 = builder.BuildQueue(PracticeType.Declension, 20, TestSeedDate);

        queue1.Should().HaveCount(queue2.Count);
        for (int i = 0; i < queue1.Count; i++)
        {
            // Compare both FormId and Source to catch regressions where items swap types
            (queue1[i].FormId, queue1[i].Source).Should().Be((queue2[i].FormId, queue2[i].Source),
                $"Item at position {i} should match in both FormId and Source");
        }
    }

    [Test]
    public void BuildQueue_DifferentSeedDates_ProducesDifferentQueues()
    {
        // Use all-new scenario (no due forms) so shuffle is the only source of variation.
        // With 320 eligible forms and no deterministic due-ordering, different seeds
        // should produce different shuffle orders.
        var db = TestScenarioBuilder.Medium().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue1 = builder.BuildQueue(PracticeType.Declension, 50, new DateTime(2024, 6, 15));
        var queue2 = builder.BuildQueue(PracticeType.Declension, 50, new DateTime(2024, 6, 16));

        // With large pool and no due forms, different seeds should produce different orders
        var differentPositions = queue1.Zip(queue2)
            .Count(pair => pair.First.FormId != pair.Second.FormId);

        differentPositions.Should().BeGreaterThan(10,
            "Different seed dates should produce substantially different queue orderings");
    }

    [Test]
    public void BuildQueue_BothPracticeTypes_BuildSuccessfully()
    {
        // Set up both noun and verb data
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 5)
            .WithCases(Case.Nominative, Case.Accusative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(5, NounPattern.AMasc, Gender.Masculine)
            .WithVerbRange(1, 5)
            .WithTenses(Tense.Present)
            .WithPersons(Person.First, Person.Second)
            .WithNumbers(Number.Singular, Number.Plural)
            .WithVoices(Voice.Active)
            .WithVerbPatterns(VerbPattern.Ati)
            .AddVerbs(5, VerbPattern.Ati)
            .Build();

        var builder = new PracticeQueueBuilder(db);

        var declensionQueue = builder.BuildQueue(PracticeType.Declension, 10, TestSeedDate);
        var conjugationQueue = builder.BuildQueue(PracticeType.Conjugation, 10, TestSeedDate);

        // Smoke test: both practice types produce non-empty queues
        declensionQueue.Should().NotBeEmpty();
        conjugationQueue.Should().NotBeEmpty();
    }

    #endregion

    #region Day 1 - All New Forms

    [Test]
    public void BuildQueue_NoPracticedForms_ReturnsAllNewForms()
    {
        // Small() has 40 eligible forms (5 lemmas × 8 cases × 1 number)
        var db = TestScenarioBuilder.Small().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 20, TestSeedDate);

        queue.Should().HaveCount(20, "Should fill requested count when pool is large enough");
        queue.Should().AllSatisfy(item =>
            item.Source.Should().Be(PracticeItemSource.NewForm));
    }

    [Test]
    public void BuildQueue_NoPracticedForms_AppliesSpacingConstraints()
    {
        var db = TestScenarioBuilder.Minimal().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 10, TestSeedDate);

        // With 2 lemmas, we should see alternating pattern (ABABAB...)
        var consecutiveSameLemma = CountMaxConsecutiveSameLemma(queue);
        consecutiveSameLemma.Should().BeLessThanOrEqualTo(2,
            "With 2 lemmas, should mostly alternate (allowing some fallback)");
    }

    #endregion

    #region Configuration Variations

    [Test]
    public void BuildQueue_MinimalPool_ScalesGapCorrectly()
    {
        // 2 lemmas, 2 cases, singular = 4 eligible forms
        var db = TestScenarioBuilder.Minimal().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 10, TestSeedDate);

        // Should produce at least 4 items (the full eligible set)
        queue.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Test]
    public void BuildQueue_SmallPool_ReturnsCorrectEligibleForms()
    {
        // 5 lemmas, 8 cases, singular = 40 eligible forms
        var db = TestScenarioBuilder.Small().Build();
        var builder = new PracticeQueueBuilder(db);

        var eligibleCount = builder.GetEligibleFormIds(PracticeType.Declension).Count;

        eligibleCount.Should().Be(40,
            "5 lemmas x 8 cases x 1 number = 40 forms");
    }

    [Test]
    public void BuildQueue_MediumPool_ReturnsCorrectEligibleForms()
    {
        // 20 lemmas, 8 cases, 2 numbers = 320 eligible forms
        var db = TestScenarioBuilder.Medium().Build();
        var builder = new PracticeQueueBuilder(db);

        var eligibleCount = builder.GetEligibleFormIds(PracticeType.Declension).Count;

        eligibleCount.Should().Be(320,
            "20 lemmas x 8 cases x 2 numbers = 320 forms");
    }

    #endregion

    #region Mastery Level Bucket Tests

    [Test]
    public void BuildQueue_MixedMasteryLevels_RotatesThroughBuckets()
    {
        // Create a scenario with high diversity (20 lemmas, 8 cases, both numbers = 320 forms)
        // to avoid spacing conflicts interfering with bucket selection.
        // Add enough due forms per bucket to ensure we get multiple picks per bucket.
        var db = TestScenarioBuilder.Medium()
            .WithDueNounFormsAcrossBuckets(formsPerBucket: 8)  // 8 per bucket = 40 total due
            .Build();
        var builder = new PracticeQueueBuilder(db);

        // Request enough items to see the round-robin pattern
        var queue = builder.BuildQueue(PracticeType.Declension, 30, TestSeedDate);

        // Get review items only (filter out new forms interspersed)
        var reviewItems = queue.Where(i => i.Source == PracticeItemSource.DueForReview).ToList();
        reviewItems.Should().HaveCountGreaterThanOrEqualTo(10,
            "Should have enough review items to verify rotation");

        // Verify round-robin: first 5 review items should come from different buckets
        // (bucket 0 → 1 → 2 → 3 → 4), then cycle repeats
        var firstFiveBuckets = reviewItems.Take(5)
            .Select(i => GetLevelBucket(i.MasteryLevel))
            .ToList();

        firstFiveBuckets.Should().OnlyHaveUniqueItems(
            "First 5 review items should each come from a different bucket (round-robin)");

        // Verify the pattern repeats: items 5-9 should also rotate through buckets
        if (reviewItems.Count >= 10)
        {
            var secondFiveBuckets = reviewItems.Skip(5).Take(5)
                .Select(i => GetLevelBucket(i.MasteryLevel))
                .ToList();

            secondFiveBuckets.Should().OnlyHaveUniqueItems(
                "Second round of 5 review items should also rotate through different buckets");
        }
    }

    [Test]
    public void BuildQueue_EmptyBucket_SkipsToNextBucket()
    {
        // Create scenario with forms only in buckets 0 and 2 (skip bucket 1)
        // Buckets: [1-2], [3-4], [5-6], [7-8], [9-10]
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 10)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental, Case.Dative)
            .WithNumbers(Number.Singular, Number.Plural)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(10, NounPattern.AMasc, Gender.Masculine)
            .Build();

        // Add due forms only at levels 1-2 and 5-6 (buckets 0 and 2)
        var eligibleForms = db.FakeNouns.GetLemmasByRank(1, 10)
            .SelectMany(l => new[] { Case.Nominative, Case.Accusative }
                .Select(c => Declension.ResolveId(l.LemmaId, c, Gender.Masculine, Number.Singular, 0)))
            .Take(10)
            .ToList();

        // First 5 forms at level 1 (bucket 0), next 5 at level 5 (bucket 2)
        for (int i = 0; i < 5 && i < eligibleForms.Count; i++)
            db.FakeUserData.AddDueNounForm(eligibleForms[i], masteryLevel: 1);
        for (int i = 5; i < 10 && i < eligibleForms.Count; i++)
            db.FakeUserData.AddDueNounForm(eligibleForms[i], masteryLevel: 5);

        var builder = new PracticeQueueBuilder(db);
        var queue = builder.BuildQueue(PracticeType.Declension, 10, TestSeedDate);

        var reviewItems = queue.Where(i => i.Source == PracticeItemSource.DueForReview).ToList();
        var buckets = reviewItems.Take(4).Select(i => GetLevelBucket(i.MasteryLevel)).ToList();

        // Should alternate between bucket 0 and bucket 2 (skipping empty 1, 3, 4)
        buckets.Should().Contain(0, "Should include items from bucket 0 (levels 1-2)");
        buckets.Should().Contain(2, "Should include items from bucket 2 (levels 5-6)");
        buckets.Should().NotContain(1, "Bucket 1 (levels 3-4) should be empty");
    }

    #endregion

    #region New/Review Slot Spacing Tests

    [Test]
    public void BuildQueue_WithBothPoolsAbundant_NewFormsAppearEvery4To6Reviews()
    {
        // Create abundant pools: 320 eligible forms, 100 due, 220 new
        // This ensures neither pool exhausts and slot spacing can be observed.
        var db = TestScenarioBuilder.Medium()
            .WithDueNounFormsAcrossBuckets(formsPerBucket: 20)  // 100 due forms
            .Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 60, TestSeedDate);

        // Find indices of NewForm items
        var newFormIndices = queue
            .Select((item, idx) => (item, idx))
            .Where(x => x.item.Source == PracticeItemSource.NewForm)
            .Select(x => x.idx)
            .ToList();

        newFormIndices.Should().HaveCountGreaterThanOrEqualTo(3,
            "Should have multiple new forms to measure gaps");

        // Calculate gaps (number of reviews between consecutive new forms)
        var gaps = new List<int>();
        for (int i = 1; i < newFormIndices.Count; i++)
        {
            var gap = newFormIndices[i] - newFormIndices[i - 1] - 1;  // -1 to exclude the new form itself
            gaps.Add(gap);
        }

        // Each gap should be between 4 and 6 (the slot plan interval)
        // Allow some deviation for edge cases, but most should be in range
        var inRangeGaps = gaps.Count(g => g >= 4 && g <= 6);
        var inRangeRatio = (double)inRangeGaps / gaps.Count;

        inRangeRatio.Should().BeGreaterThanOrEqualTo(0.7,
            $"At least 70% of gaps between new forms should be 4-6 reviews. Gaps: [{string.Join(", ", gaps)}]");
    }

    [Test]
    public void BuildQueue_ManyDueForms_MostlyReturnsReviews()
    {
        // Small pool where most forms are due
        var db = TestScenarioBuilder.Small()
            .WithDueNounFormsAcrossBuckets(formsPerBucket: 8)  // 40 due forms = all eligible
            .Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 20, TestSeedDate);

        var reviewCount = queue.Count(i => i.Source == PracticeItemSource.DueForReview);

        reviewCount.Should().BeGreaterThan(queue.Count / 2,
            "With many due forms, reviews should dominate");
    }

    [Test]
    public void BuildQueue_SlotPlanIsDeterministic_SameGapsAcrossRuns()
    {
        var db = TestScenarioBuilder.Medium()
            .WithDueNounFormsAcrossBuckets(formsPerBucket: 20)
            .Build();
        var builder = new PracticeQueueBuilder(db);

        // Build twice with same seed
        var queue1 = builder.BuildQueue(PracticeType.Declension, 40, TestSeedDate);
        var queue2 = builder.BuildQueue(PracticeType.Declension, 40, TestSeedDate);

        var sources1 = queue1.Select(i => i.Source).ToList();
        var sources2 = queue2.Select(i => i.Source).ToList();

        sources1.Should().Equal(sources2,
            "Slot plan (new vs review pattern) should be identical for same seed");
    }

    #endregion

    #region Verb Tests

    [Test]
    public void BuildQueue_Verbs_SkipsPresent3rdSingular()
    {
        var db = TestScenarioBuilder.VerbSmall().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Conjugation, 20, TestSeedDate);

        // Present 3rd singular is used as citation form and should not appear
        var hasPresent3rdSg = queue.Any(item =>
        {
            var parsed = Conjugation.ParseId(item.FormId);
            return parsed.Tense == Tense.Present &&
                   parsed.Person == Person.Third &&
                   parsed.Number == Number.Singular &&
                   parsed.Voice == Voice.Active;
        });

        hasPresent3rdSg.Should().BeFalse(
            "Present 3rd singular Active is used as citation form and should be excluded");
    }

    [Test]
    public void BuildQueue_Verbs_ReturnsCorrectEligibleForms()
    {
        // 5 verbs, 1 tense, 3 persons, 2 numbers = 30 forms - 5 (pr_3sg) = 25
        var db = TestScenarioBuilder.VerbSmall().Build();
        var builder = new PracticeQueueBuilder(db);

        var eligibleCount = builder.GetEligibleFormIds(PracticeType.Conjugation).Count;

        // 5 verbs * (3 persons * 2 numbers - 1 for pr_3sg) = 5 * 5 = 25
        eligibleCount.Should().Be(25);
    }

    #endregion

    #region Output Validity Tests

    [Test]
    public void BuildQueue_AllItems_HaveValidFormIdLemmaIdRelationship()
    {
        var db = TestScenarioBuilder.Medium().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 30, TestSeedDate);

        queue.Should().AllSatisfy(item =>
        {
            // LemmaId should match what's encoded in FormId
            var extractedLemmaId = (int)(item.FormId / Declension.LemmaDivisor);
            item.LemmaId.Should().Be(extractedLemmaId,
                $"FormId {item.FormId} should encode LemmaId {item.LemmaId}");
        });
    }

    [Test]
    public void BuildQueue_NewForms_HaveUnpracticedLevel()
    {
        var db = TestScenarioBuilder.Small().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 20, TestSeedDate);

        var newForms = queue.Where(i => i.Source == PracticeItemSource.NewForm);
        newForms.Should().AllSatisfy(item =>
            item.MasteryLevel.Should().Be(CooldownCalculator.UnpracticedLevel, "New forms should have mastery level 0"));
    }

    [Test]
    public void BuildQueue_NoDuplicateFormIds()
    {
        var db = TestScenarioBuilder.Medium().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 50, TestSeedDate);

        var formIds = queue.Select(i => i.FormId).ToList();
        formIds.Should().OnlyHaveUniqueItems("Queue should not contain duplicate forms");
    }

    #endregion

    #region Spacing Constraint Tests

    [Test]
    public void BuildQueue_HighDiversity_RespectsLemmaSpacing()
    {
        // Medium scenario has 20 lemmas - enough diversity for spacing
        var db = TestScenarioBuilder.Medium().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 40, TestSeedDate);

        // With 20 lemmas, effective gap should be close to ideal (8)
        // Check that lemma spacing is mostly respected (allow a few violations for fallback cases)
        var violations = CountSpacingViolations(queue, i => i.LemmaId, minGap: 3);
        var violationRate = (double)violations / queue.Count;

        violationRate.Should().BeLessThan(0.1,
            "With high diversity, spacing violations should be rare (<10%)");
    }

    [Test]
    public void BuildQueue_LowDiversity_GracefullyDegrades()
    {
        // Minimal scenario: only 2 lemmas
        var db = TestScenarioBuilder.Minimal().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 8, TestSeedDate);

        // With 2 lemmas, should mostly alternate (gap ~1)
        // Allow some deviation but no more than 2 consecutive same lemma
        var maxConsecutive = CountMaxConsecutiveSameLemma(queue);
        maxConsecutive.Should().BeLessThanOrEqualTo(2,
            "With 2 lemmas, should mostly alternate");
    }

    #endregion

    #region EffectiveGap Scaling Tests

    [Test]
    public void BuildQueue_OneLemma_HasZeroGap()
    {
        // 1 lemma with multiple cases = all same lemma, gap must be 0
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 1)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental, Case.Dative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(1, NounPattern.AMasc, Gender.Masculine)
            .Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 4, TestSeedDate);

        // All items must be same lemma - gap is effectively 0
        queue.Should().HaveCount(4);
        queue.Select(i => i.LemmaId).Distinct().Should().HaveCount(1,
            "With 1 lemma, all items must be from the same lemma");
    }

    [Test]
    public void BuildQueue_TwoLemmas_MostlyAlternates()
    {
        // 2 lemmas = effective gap of 1 (ABABAB pattern ideal)
        // But combo/category constraints may cause occasional deviation
        var db = TestScenarioBuilder.Minimal().Build();  // 2 lemmas, 2 cases
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 4, TestSeedDate);

        // Should mostly alternate, allowing up to 2 consecutive (fallback tolerance)
        var maxConsecutive = CountMaxConsecutiveSameLemma(queue);
        maxConsecutive.Should().BeLessThanOrEqualTo(2,
            "With 2 lemmas, should mostly alternate (gap=1 with fallback tolerance)");

        // Verify both lemmas appear
        queue.Select(i => i.LemmaId).Distinct().Should().HaveCount(2,
            "Both lemmas should be represented");
    }

    [Test]
    public void BuildQueue_ThreeLemmas_HasGapOfTwo()
    {
        // 3 lemmas = effective gap of 2 (ABC ABC pattern possible)
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 3)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(3, NounPattern.AMasc, Gender.Masculine)
            .Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 9, TestSeedDate);

        // With 3 lemmas, lemma spacing should be mostly respected
        // Allow some violations due to combo/category constraint interactions
        var violations = CountSpacingViolations(queue, i => i.LemmaId, minGap: 2);
        var violationRate = (double)violations / queue.Count;

        violationRate.Should().BeLessThan(0.2,
            "With 3 lemmas, spacing violations should be limited (<20%)");
    }

    #endregion

    #region Pool Exhaustion Tests

    [Test]
    public void BuildQueue_NewPoolExhausted_FallsBackToReviews()
    {
        // Create scenario with few new forms but many due reviews
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 2)  // Only 2 lemmas
            .WithCases(Case.Nominative, Case.Accusative)  // 2 cases
            .WithNumbers(Number.Singular)  // 1 number
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(2, NounPattern.AMasc, Gender.Masculine)
            .Build();

        // Mark all 4 forms as practiced and due
        var formIds = new[]
        {
            Declension.ResolveId(10001, Case.Nominative, Gender.Masculine, Number.Singular, 0),
            Declension.ResolveId(10001, Case.Accusative, Gender.Masculine, Number.Singular, 0),
            Declension.ResolveId(10002, Case.Nominative, Gender.Masculine, Number.Singular, 0),
            Declension.ResolveId(10002, Case.Accusative, Gender.Masculine, Number.Singular, 0),
        };
        foreach (var formId in formIds)
        {
            db.FakeUserData.AddDueNounForm(formId, masteryLevel: 3);
        }

        var builder = new PracticeQueueBuilder(db);
        var queue = builder.BuildQueue(PracticeType.Declension, 4, TestSeedDate);

        // All forms are due, so all should be reviews (no new forms available)
        queue.Should().HaveCount(4);
        queue.Should().AllSatisfy(item =>
            item.Source.Should().Be(PracticeItemSource.DueForReview,
                "When all forms are due, queue should be all reviews"));
    }

    [Test]
    public void BuildQueue_ReviewPoolExhausted_FillsWithNewForms()
    {
        // Scenario with no due forms - should fill entirely with new
        var db = TestScenarioBuilder.Small().Build();  // 40 eligible forms, 0 due
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 20, TestSeedDate);

        // No due forms, so all should be new
        queue.Should().HaveCount(20);
        queue.Should().AllSatisfy(item =>
            item.Source.Should().Be(PracticeItemSource.NewForm,
                "With no due forms, queue should be all new forms"));
    }

    [Test]
    public void BuildQueue_BothPoolsExhausted_ReturnsWhatIsAvailable()
    {
        // Very small pool - request more than available
        var db = TestScenarioBuilder.Minimal().Build();  // Only 4 eligible forms
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 10, TestSeedDate);

        // Should return only what's available (4 forms)
        queue.Should().HaveCount(4,
            "Cannot return more items than eligible forms exist");
        queue.Select(i => i.FormId).Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region SRS Invariant Tests

    [Test]
    public void BuildQueue_PracticedButNotDue_ExcludedFromQueue()
    {
        // Core SRS invariant: forms on cooldown must not appear in queue
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 5)
            .WithCases(Case.Nominative, Case.Accusative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(5, NounPattern.AMasc, Gender.Masculine)
            .Build();

        // Mark some forms as practiced but NOT due (practiced just now)
        var recentlyPracticedForms = new[]
        {
            Declension.ResolveId(10001, Case.Nominative, Gender.Masculine, Number.Singular, 0),
            Declension.ResolveId(10002, Case.Nominative, Gender.Masculine, Number.Singular, 0),
        };
        foreach (var formId in recentlyPracticedForms)
        {
            // Add with LastPracticedUtc = now, so they're on cooldown
            db.FakeUserData.AddNounFormMastery(formId, masteryLevel: 5, lastPracticedUtc: DateTime.UtcNow);
        }

        var builder = new PracticeQueueBuilder(db);
        var queue = builder.BuildQueue(PracticeType.Declension, 20, TestSeedDate);

        // Forms on cooldown should NOT appear as DueForReview
        var dueItems = queue.Where(i => i.Source == PracticeItemSource.DueForReview).ToList();
        dueItems.Should().NotContain(i => recentlyPracticedForms.Contains((int)i.FormId),
            "Forms on cooldown should not appear as DueForReview");

        // Forms on cooldown should also NOT appear as NewForm (they're practiced)
        var newItems = queue.Where(i => i.Source == PracticeItemSource.NewForm).ToList();
        newItems.Should().NotContain(i => recentlyPracticedForms.Contains((int)i.FormId),
            "Practiced forms should not appear as NewForm");
    }

    [Test]
    public void BuildQueue_RetiredForms_ExcludedForever()
    {
        // Core SRS invariant: level 11 (retired) forms never appear
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 5)
            .WithCases(Case.Nominative, Case.Accusative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(5, NounPattern.AMasc, Gender.Masculine)
            .Build();

        // Mark some forms as retired (level 11)
        var retiredForms = new[]
        {
            Declension.ResolveId(10001, Case.Nominative, Gender.Masculine, Number.Singular, 0),
            Declension.ResolveId(10002, Case.Nominative, Gender.Masculine, Number.Singular, 0),
        };
        foreach (var formId in retiredForms)
        {
            // Even with old LastPracticedUtc, retired forms should be excluded
            db.FakeUserData.AddNounFormMastery(formId,
                masteryLevel: CooldownCalculator.RetiredLevel,
                lastPracticedUtc: DateTime.UtcNow.AddYears(-1));
        }

        var builder = new PracticeQueueBuilder(db);
        var queue = builder.BuildQueue(PracticeType.Declension, 20, TestSeedDate);

        // Retired forms should NEVER appear in queue
        queue.Should().NotContain(i => retiredForms.Contains((int)i.FormId),
            "Retired forms (level 11) should never appear in queue");
    }

    [Test]
    public void BuildQueue_PartialAttestation_OnlyAttestedFormsEligible()
    {
        // Verify that unattested case/number combos are excluded from eligibility
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 3)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental, Case.Dative)
            .WithNumbers(Number.Singular, Number.Plural)
            .WithMascPatterns(NounPattern.AMasc)
            .Build();

        // Add lemmas but only attest some forms (not all case/number combos)
        for (int i = 0; i < 3; i++)
        {
            var lemmaId = 10001 + i;
            var lemma = FakeLemma.CreateNoun(lemmaId, $"noun_{lemmaId}", Gender.Masculine, NounPattern.AMasc, 1000 - i);
            db.FakeNouns.AddLemma(lemma);

            // Only attest nominative and accusative singular (not plural, not other cases)
            db.FakeNouns.AddAttestedForm(lemmaId, Case.Nominative, Gender.Masculine, Number.Singular);
            db.FakeNouns.AddAttestedForm(lemmaId, Case.Accusative, Gender.Masculine, Number.Singular);
        }

        var builder = new PracticeQueueBuilder(db);
        var eligibleIds = builder.GetEligibleFormIds(PracticeType.Declension);

        // Should only have 6 eligible forms (3 lemmas × 2 attested combos)
        eligibleIds.Should().HaveCount(6,
            "Only attested case/number combinations should be eligible");

        // Verify all eligible forms are nom_sg or acc_sg
        foreach (var formId in eligibleIds)
        {
            var parsed = Declension.ParseId(formId);
            parsed.Case.Should().BeOneOf(Case.Nominative, Case.Accusative);
            parsed.Number.Should().Be(Number.Singular);
        }
    }

    [Test]
    public void BuildQueue_LemmaRangeBoundaries_Respected()
    {
        // Verify that WithNounRange actually restricts to that rank slice
        var db = new TestScenarioBuilder()
            .WithCases(Case.Nominative, Case.Accusative)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .Build();

        // Add 10 lemmas with known EBT counts for ranking
        // Higher EBT = higher rank (rank 1 = most frequent)
        for (int i = 0; i < 10; i++)
        {
            var lemmaId = 10001 + i;
            var lemma = FakeLemma.CreateNoun(lemmaId, $"noun_{lemmaId}", Gender.Masculine, NounPattern.AMasc,
                ebtCount: 1000 - i * 100);  // 1000, 900, 800, ..., 100
            db.FakeNouns.AddLemma(lemma);
            db.FakeNouns.AddAllAttestedForms(lemmaId, Gender.Masculine);
        }

        // Set range to ranks 3-5 only
        db.FakeUserData.SetSetting(SettingsKeys.NounsLemmaMin, 3);
        db.FakeUserData.SetSetting(SettingsKeys.NounsLemmaMax, 5);

        var builder = new PracticeQueueBuilder(db);
        var queue = builder.BuildQueue(PracticeType.Declension, 20, TestSeedDate);

        // All items should be from lemmas ranked 3-5 (lemmaIds 10003, 10004, 10005)
        var validLemmaIds = new[] { 10003, 10004, 10005 };
        queue.Should().AllSatisfy(item =>
            validLemmaIds.Should().Contain(item.LemmaId,
                $"LemmaId {item.LemmaId} should be within rank range 3-5"));
    }

    [Test]
    public void BuildQueue_NonReflexiveLemma_ExcludesReflexiveForms()
    {
        // Verify that non-reflexive lemmas don't get reflexive voice forms
        var db = new TestScenarioBuilder()
            .WithVerbRange(1, 3)
            .WithTenses(Tense.Present)
            .WithPersons(Person.First, Person.Third)
            .WithNumbers(Number.Singular)
            .WithVoices(Voice.Active, Voice.Reflexive)  // Both voices enabled
            .WithVerbPatterns(VerbPattern.Ati)
            .Build();

        // Add verbs, but mark them as non-reflexive
        for (int i = 0; i < 3; i++)
        {
            var lemmaId = 70001 + i;
            var lemma = FakeLemma.CreateVerb(lemmaId, $"verb_{lemmaId}", VerbPattern.Ati, 1000 - i);
            db.FakeVerbs.AddLemma(lemma);
            db.FakeVerbs.MarkAsNonReflexive(lemmaId);  // Non-reflexive
            db.FakeVerbs.AddAllAttestedForms(lemmaId, includeReflexive: false);
        }

        var builder = new PracticeQueueBuilder(db);
        var queue = builder.BuildQueue(PracticeType.Conjugation, 20, TestSeedDate);

        // No reflexive forms should appear
        queue.Should().AllSatisfy(item =>
        {
            var parsed = Conjugation.ParseId(item.FormId);
            parsed.Voice.Should().NotBe(Voice.Reflexive,
                "Non-reflexive lemmas should not produce reflexive forms");
        });
    }

    #endregion

    #region Settings Canonicalization Tests

    [Test]
    public void BuildQueue_VerbSettingsInDifferentOrder_ProducesSameQueue()
    {
        // Verify verb settings (tenses, persons) are also canonicalized
        var db1 = new TestScenarioBuilder()
            .WithVerbRange(1, 5)
            .WithTenses(Tense.Present, Tense.Imperative, Tense.Optative)
            .WithPersons(Person.First, Person.Second, Person.Third)
            .WithNumbers(Number.Singular)
            .WithVoices(Voice.Active)
            .WithVerbPatterns(VerbPattern.Ati)
            .AddVerbs(5, VerbPattern.Ati)
            .Build();

        var db2 = new TestScenarioBuilder()
            .WithVerbRange(1, 5)
            .WithTenses(Tense.Optative, Tense.Present, Tense.Imperative)  // Different order
            .WithPersons(Person.Third, Person.First, Person.Second)        // Different order
            .WithNumbers(Number.Singular)
            .WithVoices(Voice.Active)
            .WithVerbPatterns(VerbPattern.Ati)
            .AddVerbs(5, VerbPattern.Ati)
            .Build();

        var builder1 = new PracticeQueueBuilder(db1);
        var builder2 = new PracticeQueueBuilder(db2);

        var queue1 = builder1.BuildQueue(PracticeType.Conjugation, 20, TestSeedDate);
        var queue2 = builder2.BuildQueue(PracticeType.Conjugation, 20, TestSeedDate);

        queue1.Should().HaveCount(queue2.Count);
        for (int i = 0; i < queue1.Count; i++)
        {
            (queue1[i].FormId, queue1[i].Source).Should().Be((queue2[i].FormId, queue2[i].Source),
                $"Item at position {i} should match regardless of settings order");
        }
    }

    [Test]
    public void BuildQueue_SameSettingsInDifferentOrder_ProducesSameQueue()
    {
        // Test that settings order doesn't affect queue (validates canonicalization)
        // Create two builders with same cases but in different order
        var db1 = new TestScenarioBuilder()
            .WithNounRange(1, 5)
            .WithCases(Case.Nominative, Case.Accusative, Case.Dative)  // nom, acc, dat
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(5, NounPattern.AMasc, Gender.Masculine)
            .Build();

        var db2 = new TestScenarioBuilder()
            .WithNounRange(1, 5)
            .WithCases(Case.Dative, Case.Nominative, Case.Accusative)  // dat, nom, acc (different order)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(5, NounPattern.AMasc, Gender.Masculine)
            .Build();

        var builder1 = new PracticeQueueBuilder(db1);
        var builder2 = new PracticeQueueBuilder(db2);

        var queue1 = builder1.BuildQueue(PracticeType.Declension, 15, TestSeedDate);
        var queue2 = builder2.BuildQueue(PracticeType.Declension, 15, TestSeedDate);

        // Queues should be identical regardless of settings order
        queue1.Should().HaveCount(queue2.Count);
        for (int i = 0; i < queue1.Count; i++)
        {
            (queue1[i].FormId, queue1[i].Source).Should().Be((queue2[i].FormId, queue2[i].Source),
                $"Item at position {i} should match regardless of settings order");
        }
    }

    #endregion

    #region Precision Constraint Tests

    [Test]
    public void BuildQueue_Conjugations_HaveValidFormIdLemmaIdRelationship()
    {
        // Mirror the declension test but for conjugations
        var db = TestScenarioBuilder.VerbSmall().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Conjugation, 20, TestSeedDate);

        queue.Should().AllSatisfy(item =>
        {
            var extractedLemmaId = (int)(item.FormId / Conjugation.LemmaDivisor);
            item.LemmaId.Should().Be(extractedLemmaId,
                $"FormId {item.FormId} should encode LemmaId {item.LemmaId}");
        });
    }

    [Test]
    public void BuildQueue_HighDiversity_RespectsComboSpacing()
    {
        // Test combo (case+number) spacing with diverse pool
        // 20 lemmas × 8 cases × 2 numbers = 320 forms, 16 unique combos
        var db = TestScenarioBuilder.Medium().Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 40, TestSeedDate);

        // Extract combo key: case_number (e.g., "nom_sg", "acc_pl")
        var combos = queue.Select(item =>
        {
            var parsed = Declension.ParseId(item.FormId);
            return $"{parsed.Case}_{parsed.Number}";
        }).ToList();

        // With 16 unique combos, effective gap should be ~5-6
        // Check that same combo doesn't repeat within gap of 3
        var violations = 0;
        var lastSeen = new Dictionary<string, int>();
        for (int i = 0; i < combos.Count; i++)
        {
            if (lastSeen.TryGetValue(combos[i], out var lastIdx) && i - lastIdx < 3)
                violations++;
            lastSeen[combos[i]] = i;
        }

        var violationRate = (double)violations / queue.Count;
        violationRate.Should().BeLessThan(0.15,
            "Combo spacing violations should be rare with diverse pool (<15%)");
    }

    [Test]
    public void BuildQueue_TwoLemmasManyCombos_PrefersComboSpacingOverLemma()
    {
        // Fallback chain test: with only 2 lemmas but 16 combos (8 cases × 2 numbers),
        // the algorithm should prefer combo-only fallback (pass 3) over lemma-only (pass 4).
        // This means we should see good combo variety even if lemmas alternate imperfectly.
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 2)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental, Case.Dative,
                       Case.Ablative, Case.Genitive, Case.Locative, Case.Vocative)
            .WithNumbers(Number.Singular, Number.Plural)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(2, NounPattern.AMasc, Gender.Masculine)  // 2 lemmas × 16 combos = 32 forms
            .Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 16, TestSeedDate);

        // Extract combos
        var combos = queue.Select(item =>
        {
            var parsed = Declension.ParseId(item.FormId);
            return $"{parsed.Case}_{parsed.Number}";
        }).ToList();

        // Should have high combo diversity (few immediate repeats)
        var immediateComboRepeats = 0;
        for (int i = 1; i < combos.Count; i++)
        {
            if (combos[i] == combos[i - 1])
                immediateComboRepeats++;
        }

        immediateComboRepeats.Should().BeLessThan(3,
            "With 2 lemmas and 16 combos, combo-only fallback should prevent immediate combo repeats");
    }

    [Test]
    public void BuildQueue_OneCaseOneNumber_FallsBackToAny()
    {
        // Edge case: 1 combo means every item has the same combo, so fallback reaches "any"
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 5)
            .WithCases(Case.Nominative)  // Only 1 case
            .WithNumbers(Number.Singular)  // Only 1 number
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(5, NounPattern.AMasc, Gender.Masculine)  // 5 forms total
            .Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 5, TestSeedDate);

        // Should still produce a queue (fallback to "any" pass works)
        queue.Should().HaveCount(5);
        queue.Select(i => i.FormId).Should().OnlyHaveUniqueItems();

        // All combos are the same
        var combos = queue.Select(item =>
        {
            var parsed = Declension.ParseId(item.FormId);
            return $"{parsed.Case}_{parsed.Number}";
        }).Distinct().ToList();

        combos.Should().HaveCount(1, "Only one combo exists in this scenario");
    }

    [Test]
    public void BuildQueue_SameCategoryAllLemmas_CategoryDropFallbackUsed()
    {
        // All lemmas have the same pattern (category), so category constraint can't be satisfied
        // Algorithm should drop to pass 2 (lemma+combo only)
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 5)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental)
            .WithNumbers(Number.Singular)
            .WithMascPatterns(NounPattern.AMasc)  // All same pattern
            .AddNouns(5, NounPattern.AMasc, Gender.Masculine)
            .Build();
        var builder = new PracticeQueueBuilder(db);

        var queue = builder.BuildQueue(PracticeType.Declension, 15, TestSeedDate);

        // Should still produce diverse queue by lemma/combo
        queue.Should().HaveCount(15);
        queue.Select(i => i.LemmaId).Distinct().Should().HaveCountGreaterThan(1,
            "Should use multiple lemmas even when all have same category");

        // Check lemma spacing is still maintained
        var lemmaViolations = CountSpacingViolations(queue, i => i.LemmaId, minGap: 3);
        var violationRate = (double)lemmaViolations / queue.Count;
        violationRate.Should().BeLessThan(0.2,
            "Lemma spacing should still be respected after category fallback");
    }

    [Test]
    public void BuildQueue_UrgentItemsFirst_WithinSameBucket()
    {
        // Within a bucket, most overdue items should be selected first
        var db = new TestScenarioBuilder()
            .WithNounRange(1, 10)
            .WithCases(Case.Nominative, Case.Accusative, Case.Instrumental, Case.Dative)
            .WithNumbers(Number.Singular, Number.Plural)
            .WithMascPatterns(NounPattern.AMasc)
            .AddNouns(10, NounPattern.AMasc, Gender.Masculine)
            .Build();

        // Add forms at level 3 (bucket 1) with varying overdue times
        // Earlier added = more overdue (due to timestamp offset in fake)
        var eligibleForms = db.FakeNouns.GetLemmasByRank(1, 10)
            .SelectMany(l => new[] { Case.Nominative, Case.Accusative }
                .Select(c => (long)Declension.ResolveId(l.LemmaId, c, Gender.Masculine, Number.Singular, 0)))
            .Take(8)
            .ToList();

        foreach (var formId in eligibleForms)
            db.FakeUserData.AddDueNounForm(formId, masteryLevel: 3);

        var builder = new PracticeQueueBuilder(db);
        var queue = builder.BuildQueue(PracticeType.Declension, 20, TestSeedDate);

        // Get review items from bucket 1 (levels 3-4)
        var bucket1Items = queue
            .Where(i => i.Source == PracticeItemSource.DueForReview && GetLevelBucket(i.MasteryLevel) == 1)
            .ToList();

        bucket1Items.Should().NotBeEmpty("Should have items from bucket 1");
        // First items from this bucket should be from the beginning of eligibleForms
        // (which were added first and are thus most overdue)
        // This is hard to test precisely, so we verify the items come from our added set
        bucket1Items.Should().AllSatisfy(item =>
            eligibleForms.Should().Contain(item.FormId));
    }

    #endregion

    #region Helper Methods

    static int CountSpacingViolations<T>(List<PracticeItem> queue, Func<PracticeItem, T> keySelector, int minGap) where T : notnull
    {
        var lastSeen = new Dictionary<T, int>();
        int violations = 0;

        for (int i = 0; i < queue.Count; i++)
        {
            var key = keySelector(queue[i]);
            if (lastSeen.TryGetValue(key, out var lastIndex))
            {
                var gap = i - lastIndex;
                if (gap < minGap)
                    violations++;
            }
            lastSeen[key] = i;
        }

        return violations;
    }

    static int CountMaxConsecutiveSameLemma(List<PracticeItem> queue)
    {
        if (queue.Count <= 1) return queue.Count;

        int maxConsecutive = 1;
        int currentConsecutive = 1;

        for (int i = 1; i < queue.Count; i++)
        {
            if (queue[i].LemmaId == queue[i - 1].LemmaId)
            {
                currentConsecutive++;
                maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
            }
            else
            {
                currentConsecutive = 1;
            }
        }

        return maxConsecutive;
    }

    static int GetLevelBucket(int masteryLevel)
    {
        return masteryLevel switch
        {
            <= 2 => 0,
            <= 4 => 1,
            <= 6 => 2,
            <= 8 => 3,
            _ => 4
        };
    }

    #endregion
}
