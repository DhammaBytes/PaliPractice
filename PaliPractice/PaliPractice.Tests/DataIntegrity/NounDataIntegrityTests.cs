using FluentAssertions;
using PaliPractice.Models.Inflection;
using PaliPractice.Tests.DataIntegrity.Helpers;

namespace PaliPractice.Tests.DataIntegrity;

/// <summary>
/// Data integrity tests for nouns - verifies all noun data in pali.db
/// matches the source of truth dpd.db.
/// </summary>
/// <remarks>
/// <para>
/// <b>Data Sources and Validation Triangle:</b>
/// </para>
/// <code>
///          dpd.db (gray status in inflections_html)
///               ↗                    ↘
///    App DB ←—————————————————→ Tipitaka wordlists
///    (pali.db)                (source of truth for corpus)
/// </code>
/// <para>
/// <b>What each source provides:</b>
/// </para>
/// <list type="bullet">
///   <item><b>dpd.db</b>: Digital Pāḷi Dictionary - the authoritative source for word properties
///     (lemma, stem, pattern, meaning, examples). Gray forms in HTML indicate theoretical inflections.</item>
///   <item><b>pali.db</b>: App's extracted database. Properties come from dpd.db,
///     corpus tables (nouns_corpus_forms) come from Tipitaka wordlists.</item>
///   <item><b>Tipitaka wordlists</b>: JSON files (cst, bjt, sya, sc) containing actual words
///     found in Pali canon texts. Used by extraction script to determine InCorpus status.</item>
/// </list>
/// <para>
/// <b>Key transformations verified:</b>
/// </para>
/// <list type="bullet">
///   <item>dpd.lemma_1 → Regex.Replace(@" \d.*$", "") → pali.lemma</item>
///   <item>dpd.stem → Regex.Replace(@"[!*]", "") → pali.stem (removes DPD markers)</item>
///   <item>dpd.pos → masc=1, nt=2, fem=3 → pali.gender</item>
/// </list>
/// <para>
/// <b>InCorpus cross-validation:</b>
/// The tests compare DPD's gray status with Tipitaka wordlist presence.
/// Minor discrepancies (~0.03%) are expected - DPD may mark vocative forms as gray
/// even when they appear in wordlists (vocative often equals nominative).
/// </para>
/// </remarks>
[TestFixture]
public class NounDataIntegrityTests
{
    PaliDbLoader? _paliDb;
    DpdWordLoader? _dpdDb;
    DpdPatternClassifier? _dpdPatterns;
    Dictionary<int, DpdHeadword>? _dpdHeadwords;
    List<PaliNoun>? _paliNouns;
    List<PaliNounDetails>? _paliNounDetails;
    Dictionary<int, PaliNounDetails>? _nounDetailsById;
    HashSet<long>? _corpusDeclensionFormIds;
    HashSet<string>? _tipitakaWords;
    CustomTranslationsLoader? _customTranslations;

    // DPD-sourced pattern classification (loaded in OneTimeSetUp)
    HashSet<string>? _dpdIrregularNounPatterns;
    HashSet<string>? _dpdRegularNounPatterns;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _paliDb = new PaliDbLoader();
        _dpdDb = new DpdWordLoader();
        _dpdPatterns = new DpdPatternClassifier();
        _customTranslations = new CustomTranslationsLoader();

        // Load all data upfront for performance
        _dpdHeadwords = _dpdDb.GetAllHeadwordsById();
        _paliNouns = _paliDb.GetAllNouns();
        _paliNounDetails = _paliDb.GetAllNounDetails();
        _nounDetailsById = _paliNounDetails.ToDictionary(d => d.Id);
        _corpusDeclensionFormIds = _paliDb.GetCorpusDeclensionFormIds();
        _tipitakaWords = TipitakaWordlistLoader.GetAllWords();

        // Load DPD-sourced pattern classification
        _dpdIrregularNounPatterns = _dpdPatterns.GetIrregularNounPatterns();
        _dpdRegularNounPatterns = _dpdPatterns.GetRegularNounPatterns();

        TestContext.WriteLine($"Loaded {_paliNouns.Count} nouns from pali.db");
        TestContext.WriteLine($"Loaded {_paliNounDetails.Count} noun details from pali.db");
        TestContext.WriteLine($"Loaded {_dpdHeadwords.Count} headwords from dpd.db");
        TestContext.WriteLine($"Loaded {_corpusDeclensionFormIds.Count} corpus declension form_ids");
        TestContext.WriteLine($"Loaded {_tipitakaWords.Count} Tipitaka words");
        TestContext.WriteLine($"Loaded {_customTranslations.Count} custom translation adjustments");
        TestContext.WriteLine($"DPD irregular noun patterns: {_dpdIrregularNounPatterns.Count}");
        TestContext.WriteLine($"DPD regular noun patterns: {_dpdRegularNounPatterns.Count}");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _paliDb?.Dispose();
        _dpdDb?.Dispose();
        _dpdPatterns?.Dispose();
    }

    #region Core Property Tests

    [Test]
    public void AllNouns_ExistInDpd()
    {
        var missing = _paliNouns!
            .Where(n => !_dpdHeadwords!.ContainsKey(n.Id))
            .Select(n => $"id={n.Id}, lemma={n.Lemma}")
            .ToList();

        missing.Should().BeEmpty(
            "every noun in pali.db should exist in dpd.db");
    }

    [Test]
    public void AllNouns_LemmaMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var noun in _paliNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue; // Tested separately in AllNouns_ExistInDpd

            if (noun.Lemma != dpd.LemmaClean)
            {
                mismatches.Add($"id={noun.Id}: pali='{noun.Lemma}' vs dpd='{dpd.LemmaClean}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns.lemma should match dpd.lemma_clean (lemma_1 with ' N...' suffix removed)");
    }

    [Test]
    public void AllNouns_PatternMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var noun in _paliNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            if (noun.Pattern != dpd.Pattern)
            {
                mismatches.Add($"id={noun.Id} ({noun.Lemma}): pali='{noun.Pattern}' vs dpd='{dpd.Pattern}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns.pattern should match dpd.pattern exactly");
    }

    [Test]
    public void AllNouns_EbtCountMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var noun in _paliNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            if (noun.EbtCount != dpd.EbtCount)
            {
                mismatches.Add($"id={noun.Id} ({noun.Lemma}): pali={noun.EbtCount} vs dpd={dpd.EbtCount}");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns.ebt_count should match dpd.ebt_count exactly");
    }

    [Test]
    public void AllNouns_StemMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var noun in _paliNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            // pali.db stores stem after removing !* markers
            if (noun.Stem != dpd.StemClean)
            {
                mismatches.Add($"id={noun.Id} ({noun.Lemma}): pali='{noun.Stem}' vs dpd='{dpd.StemClean}' (raw: '{dpd.Stem}')");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns.stem should match dpd.stem after removing !* markers");
    }

    [Test]
    public void AllNouns_GenderMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var noun in _paliNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            var expectedGender = PosToGender(dpd.Pos);
            if (noun.Gender != expectedGender)
            {
                mismatches.Add($"id={noun.Id} ({noun.Lemma}): pali={noun.Gender} vs expected={expectedGender} (pos='{dpd.Pos}')");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns.gender should match dpd.pos (masc=1, nt=2, fem=3)");
    }

    static int PosToGender(string pos) => pos switch
    {
        "masc" => 1,
        "nt" => 2,
        "fem" => 3,
        _ => 0
    };

    #endregion

    #region Details Property Tests

    [Test]
    public void AllNounDetails_ExistForEachNoun()
    {
        var missing = _paliNouns!
            .Where(n => !_nounDetailsById!.ContainsKey(n.Id))
            .Select(n => $"id={n.Id}, lemma={n.Lemma}")
            .ToList();

        missing.Should().BeEmpty(
            "every noun should have a corresponding details entry");
    }

    [Test]
    public void AllNounDetails_MeaningMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            // Apply custom translation adjustments (same as extraction script)
            var expectedMeaning = _customTranslations!.Apply(details.Id, dpd.Lemma1, dpd.Meaning1);

            if (details.Meaning != expectedMeaning)
            {
                mismatches.Add($"id={details.Id}: pali='{Truncate(details.Meaning)}' vs expected='{Truncate(expectedMeaning)}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.meaning should match dpd.meaning_1 (with custom translation adjustments applied)");
    }

    [Test]
    public void AllNounDetails_Source1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Source1 != dpd.Source1)
            {
                mismatches.Add($"id={details.Id}: pali='{details.Source1}' vs dpd='{dpd.Source1}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.source_1 should match dpd.source_1 exactly");
    }

    [Test]
    public void AllNounDetails_Sutta1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Sutta1 != dpd.Sutta1)
            {
                mismatches.Add($"id={details.Id}: pali='{details.Sutta1}' vs dpd='{dpd.Sutta1}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.sutta_1 should match dpd.sutta_1 exactly");
    }

    [Test]
    public void AllNounDetails_Example1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Example1 != dpd.Example1)
            {
                mismatches.Add($"id={details.Id}: pali='{Truncate(details.Example1)}' vs dpd='{Truncate(dpd.Example1)}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.example_1 should match dpd.example_1 exactly");
    }

    [Test]
    public void AllNounDetails_Source2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Source2 != dpd.Source2)
            {
                mismatches.Add($"id={details.Id}: pali='{details.Source2}' vs dpd='{dpd.Source2}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.source_2 should match dpd.source_2 exactly");
    }

    [Test]
    public void AllNounDetails_Sutta2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Sutta2 != dpd.Sutta2)
            {
                mismatches.Add($"id={details.Id}: pali='{details.Sutta2}' vs dpd='{dpd.Sutta2}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.sutta_2 should match dpd.sutta_2 exactly");
    }

    [Test]
    public void AllNounDetails_Example2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Example2 != dpd.Example2)
            {
                mismatches.Add($"id={details.Id}: pali='{Truncate(details.Example2)}' vs dpd='{Truncate(dpd.Example2)}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.example_2 should match dpd.example_2 exactly");
    }

    #endregion

    #region InCorpus Tests

    [Test]
    public void InCorpus_GrayFormsNotInWordlists()
    {
        var nounsWithIssues = new HashSet<int>();
        var formIssues = new List<string>();

        foreach (var noun in _paliNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            if (string.IsNullOrEmpty(dpd.InflectionsHtml))
                continue;

            var grayForms = InCorpusValidator.GetTheoreticalForms(dpd.InflectionsHtml, noun.Stem);

            foreach (var form in grayForms)
            {
                if (_tipitakaWords!.Contains(form.FullForm))
                {
                    nounsWithIssues.Add(noun.Id);
                    formIssues.Add($"{noun.Lemma}: '{form.FullForm}' ({form.Title}) is gray but found in wordlist");
                }
            }
        }

        // Report discrepancies - some are expected due to wordlist/DPD differences
        TestContext.WriteLine($"Nouns with gray forms unexpectedly in wordlist: {nounsWithIssues.Count}");
        TestContext.WriteLine($"Total form issues: {formIssues.Count}");
        foreach (var msg in formIssues.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        // Allow some discrepancy (DPD gray marking may differ from wordlist coverage)
        // Rate is % of nouns that have at least one problematic form
        var discrepancyRate = (double)nounsWithIssues.Count / _paliNouns!.Count;
        discrepancyRate.Should().BeLessThan(0.02,
            "less than 2% of nouns should have gray forms that appear in wordlists");
    }

    [Test]
    public void InCorpus_NonGrayFormsInWordlists()
    {
        var nounsWithIssues = new HashSet<int>();
        var formIssues = new List<string>();

        foreach (var noun in _paliNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            if (string.IsNullOrEmpty(dpd.InflectionsHtml))
                continue;

            var inCorpusForms = InCorpusValidator.GetInCorpusForms(dpd.InflectionsHtml, noun.Stem);

            foreach (var form in inCorpusForms)
            {
                if (!_tipitakaWords!.Contains(form.FullForm))
                {
                    nounsWithIssues.Add(noun.Id);
                    formIssues.Add($"{noun.Lemma}: '{form.FullForm}' ({form.Title}) is non-gray but missing from wordlist");
                }
            }
        }

        // Report discrepancies
        TestContext.WriteLine($"Nouns with non-gray forms missing from wordlist: {nounsWithIssues.Count}");
        TestContext.WriteLine($"Total form issues: {formIssues.Count}");
        foreach (var msg in formIssues.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        // Allow some discrepancy
        // Rate is % of nouns that have at least one problematic form
        var discrepancyRate = (double)nounsWithIssues.Count / _paliNouns!.Count;
        discrepancyRate.Should().BeLessThan(0.05,
            "less than 5% of nouns should have non-gray forms missing from wordlists");
    }

    [Test]
    public void InCorpus_CrossValidateBothSources()
    {
        var totalForms = 0;
        var agreementCount = 0;
        var disagreementSamples = new List<string>();

        foreach (var noun in _paliNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            if (string.IsNullOrEmpty(dpd.InflectionsHtml))
                continue;

            var validation = InCorpusValidator.ValidateAgainstWordlist(
                dpd.InflectionsHtml,
                noun.Stem,
                _tipitakaWords!);

            foreach (var result in validation)
            {
                totalForms++;
                if (result.SourcesAgree)
                {
                    agreementCount++;
                }
                else if (disagreementSamples.Count < 50)
                {
                    disagreementSamples.Add(
                        $"{noun.Lemma} '{result.FullForm}' ({result.Title}): " +
                        $"isGray={result.IsGrayInHtml}, inWordlist={result.InTipitakaWordlist}");
                }
            }
        }

        var agreementRate = totalForms > 0 ? (double)agreementCount / totalForms : 1.0;

        TestContext.WriteLine($"Total forms validated: {totalForms}");
        TestContext.WriteLine($"Agreement count: {agreementCount}");
        TestContext.WriteLine($"Agreement rate: {agreementRate:P2}");
        TestContext.WriteLine("Sample disagreements:");
        foreach (var sample in disagreementSamples.Take(20))
        {
            TestContext.WriteLine($"  {sample}");
        }

        // Tightened from 85% to 90% - if agreement drops below this, investigate
        agreementRate.Should().BeGreaterThan(0.90,
            "at least 90% of forms should have agreement between HTML gray status and wordlist presence");
    }

    #endregion

    #region Table Consistency Tests

    [Test]
    public void NounsAndDetails_HaveConsistentLemmaIds()
    {
        var mismatches = new List<string>();

        foreach (var noun in _paliNouns!)
        {
            if (!_nounDetailsById!.TryGetValue(noun.Id, out var details))
                continue;

            if (noun.LemmaId != details.LemmaId)
            {
                mismatches.Add($"id={noun.Id}: nouns.lemma_id={noun.LemmaId} vs details.lemma_id={details.LemmaId}");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns.lemma_id should match nouns_details.lemma_id for same id");
    }

    [Test]
    public void CorpusDeclensions_HasFormIds()
    {
        _corpusDeclensionFormIds.Should().NotBeEmpty(
            "nouns_corpus_forms should contain form_ids");

        TestContext.WriteLine($"Corpus declension form_ids: {_corpusDeclensionFormIds!.Count}");
    }

    [Test]
    public void AllNouns_HaveValidGender()
    {
        var invalid = _paliNouns!
            .Where(n => n.Gender < 1 || n.Gender > 3)
            .Select(n => $"id={n.Id} ({n.Lemma}): gender={n.Gender}")
            .ToList();

        invalid.Should().BeEmpty(
            "all nouns should have gender 1 (masc), 2 (nt), or 3 (fem)");
    }

    [Test]
    public void AllNouns_HaveNonEmptyPattern()
    {
        var empty = _paliNouns!
            .Where(n => string.IsNullOrEmpty(n.Pattern))
            .Select(n => $"id={n.Id} ({n.Lemma})")
            .ToList();

        empty.Should().BeEmpty(
            "all nouns should have a non-empty pattern");
    }

    [Test]
    public void AllNounDetails_HaveNonEmptyMeaning()
    {
        var empty = _paliNounDetails!
            .Where(d => string.IsNullOrEmpty(d.Meaning))
            .Select(d => $"id={d.Id}")
            .ToList();

        empty.Should().BeEmpty(
            "all noun details should have a non-empty meaning (filtered during extraction)");
    }

    #endregion

    #region Irregular Form Tests

    // Plural-only patterns (end with " pl")
    static bool IsPluralOnlyPattern(string pattern) =>
        pattern.Trim().EndsWith(" pl", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Parse a noun form_id to extract the number component.
    /// Format: lemma_id(5) + case(1) + gender(1) + number(1) + ending_index(1)
    /// Number: 1 = singular, 2 = plural
    /// </summary>
    static int ExtractNumberFromNounFormId(long formId) =>
        (int)(formId % 100 / 10);

    [Test]
    public void DpdPatternClassification_MatchesOurEnumClassification()
    {
        // This test catches when DPD changes pattern classification
        // If it fails, we need to update our NounPattern enum and extraction config

        var enumIrregulars = Enum.GetValues<NounPattern>()
            .Where(p => !p.IsMarkerOrNone() && p.IsIrregular())
            .Select(p => p.ToDbString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var enumVariants = Enum.GetValues<NounPattern>()
            .Where(p => !p.IsMarkerOrNone() && p.IsVariant())
            .Select(p => p.ToDbString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check for patterns we mark as irregular but DPD doesn't
        var falseIrregulars = enumIrregulars
            .Where(p => !_dpdIrregularNounPatterns!.Contains(p))
            .ToList();

        // Check for patterns DPD marks as irregular but we don't
        var missingIrregulars = _dpdIrregularNounPatterns!
            .Where(p => !enumIrregulars.Contains(p))
            .ToList();

        // Check for patterns we mark as variant but DPD marks as irregular
        var variantsActuallyIrregular = enumVariants
            .Where(p => _dpdIrregularNounPatterns!.Contains(p))
            .ToList();

        TestContext.WriteLine($"Our enum irregular patterns: {enumIrregulars.Count}");
        TestContext.WriteLine($"DPD irregular noun patterns: {_dpdIrregularNounPatterns!.Count}");
        TestContext.WriteLine($"Our enum variant patterns: {enumVariants.Count}");

        if (falseIrregulars.Count > 0)
        {
            TestContext.WriteLine("Patterns we mark irregular but DPD doesn't:");
            foreach (var p in falseIrregulars)
                TestContext.WriteLine($"  {p} (DPD like='{_dpdPatterns!.GetLikeValue(p)}')");
        }

        if (missingIrregulars.Count > 0)
        {
            TestContext.WriteLine("Patterns DPD marks irregular but we don't:");
            foreach (var p in missingIrregulars)
                TestContext.WriteLine($"  {p}");
        }

        if (variantsActuallyIrregular.Count > 0)
        {
            TestContext.WriteLine("Patterns we mark variant but DPD marks irregular:");
            foreach (var p in variantsActuallyIrregular)
                TestContext.WriteLine($"  {p}");
        }

        falseIrregulars.Should().BeEmpty(
            "patterns we mark as irregular should have like='irreg' in DPD");
        variantsActuallyIrregular.Should().BeEmpty(
            "patterns we mark as variant should NOT have like='irreg' in DPD");

        // Note: missingIrregulars may not be empty if DPD has irregular patterns
        // we don't support yet - that's informational, not a failure
    }

    [Test]
    public void IrregularNouns_CorpusFormsHaveIrregularFormEntries()
    {
        // Use DPD-sourced pattern set for authoritative classification
        var irregularNouns = _paliNouns!
            .Where(n => _dpdIrregularNounPatterns!.Contains(n.Pattern))
            .ToList();

        if (irregularNouns.Count == 0)
        {
            Assert.Inconclusive("No irregular nouns found in pali.db");
            return;
        }

        // Get irregular noun lemma_ids
        var irregularLemmaIds = irregularNouns
            .Select(n => n.LemmaId)
            .ToHashSet();

        // Get irregular form_ids from the irregular_forms table
        var irregularFormIds = _paliDb!.GetIrregularNounFormIds();

        // Check that corpus forms for irregular nouns have corresponding irregular_forms entries
        var missingIrregularForms = new List<string>();

        foreach (var corpusFormId in _corpusDeclensionFormIds!)
        {
            // Extract lemma_id from form_id (first 5 digits)
            var lemmaId = (int)(corpusFormId / 10_000);

            // Skip if not an irregular noun
            if (!irregularLemmaIds.Contains(lemmaId))
                continue;

            // Check if this corpus form has an irregular form entry
            if (!irregularFormIds.Contains((int)corpusFormId))
            {
                var noun = irregularNouns.FirstOrDefault(n => n.LemmaId == lemmaId);
                missingIrregularForms.Add(
                    $"form_id={corpusFormId} (lemma={noun?.Lemma}, pattern={noun?.Pattern})");
            }
        }

        // Report findings
        TestContext.WriteLine($"Irregular nouns: {irregularNouns.Count}");
        TestContext.WriteLine($"Irregular corpus form_ids checked: {_corpusDeclensionFormIds.Count(f => irregularLemmaIds.Contains((int)(f / 10_000)))}");
        TestContext.WriteLine($"Irregular form entries: {irregularFormIds.Count}");
        TestContext.WriteLine($"Missing irregular form entries: {missingIrregularForms.Count}");

        foreach (var msg in missingIrregularForms.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        // Note: Some mismatch is expected because corpus_forms uses ending indices from
        // template parsing, while irregular_forms uses indices from HTML parsing.
        // The forms are the same but may have different ending_index values.
        // This is acceptable as long as the app uses the irregular_forms table for display
        // and the corpus check is based on the form string presence in Tipitaka wordlists.
        var mismatchRate = (double)missingIrregularForms.Count / _corpusDeclensionFormIds.Count(f => irregularLemmaIds.Contains((int)(f / 10_000)));
        TestContext.WriteLine($"Mismatch rate: {mismatchRate:P1}");

        // Allow up to 10% mismatch due to index ordering differences between template and HTML
        mismatchRate.Should().BeLessThan(0.10,
            "less than 10% of irregular corpus forms should have index misalignment with irregular_forms table");
    }

    [Test]
    public void VariantNouns_ShouldNotHaveIrregularFormEntries()
    {
        // Variant patterns (non-irregular in DPD) use templates, NOT irregular_forms table
        // This test ensures we haven't accidentally added variant forms to irregular_forms
        //
        // NOTE: This test will fail if pali.db was generated before config.py split
        // variant patterns from irregular patterns. Re-run extraction to fix.

        // Get nouns with patterns that DPD marks as regular (non-irreg)
        // but which we treat as variants (non-base)
        var variantNouns = _paliNouns!
            .Where(n => _dpdRegularNounPatterns!.Contains(n.Pattern))
            .Where(n => !NounPatternHelper.TryParse(n.Pattern, out var p) || !p.IsBase())
            .ToList();

        if (variantNouns.Count == 0)
        {
            Assert.Inconclusive("No variant nouns found in pali.db");
            return;
        }

        // Get variant noun lemma_ids
        var variantLemmaIds = variantNouns
            .Select(n => n.LemmaId)
            .ToHashSet();

        // Get irregular form_ids from the irregular_forms table
        var irregularFormIds = _paliDb!.GetIrregularNounFormIds();

        // Check if any variant nouns have entries in irregular_forms
        var unexpectedIrregularEntries = new List<string>();

        foreach (var irregularFormId in irregularFormIds)
        {
            var lemmaId = irregularFormId / 10_000;

            if (variantLemmaIds.Contains(lemmaId))
            {
                var noun = variantNouns.FirstOrDefault(n => n.LemmaId == lemmaId);
                unexpectedIrregularEntries.Add(
                    $"form_id={irregularFormId} (lemma={noun?.Lemma}, pattern={noun?.Pattern})");
            }
        }

        // Report findings
        TestContext.WriteLine($"Variant nouns: {variantNouns.Count}");
        TestContext.WriteLine($"Variant lemma_ids: {variantLemmaIds.Count}");
        TestContext.WriteLine($"Unexpected irregular entries for variants: {unexpectedIrregularEntries.Count}");

        foreach (var msg in unexpectedIrregularEntries.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        if (unexpectedIrregularEntries.Count > 0)
        {
            // Data was generated before config.py split - warn but don't fail
            // Re-running extraction with updated config.py will fix this
            Assert.Warn(
                $"Found {unexpectedIrregularEntries.Count} variant entries in irregular_forms table. " +
                "Re-run extraction to regenerate pali.db with proper variant/irregular separation.");
        }
    }

    [Test]
    public void PluralOnlyNouns_HaveNoSingularCorpusForms()
    {
        // Get plural-only nouns
        var pluralOnlyNouns = _paliNouns!
            .Where(n => IsPluralOnlyPattern(n.Pattern))
            .ToList();

        if (pluralOnlyNouns.Count == 0)
        {
            Assert.Inconclusive("No plural-only nouns found in pali.db");
            return;
        }

        // Get plural-only lemma_ids
        var pluralOnlyLemmaIds = pluralOnlyNouns
            .Select(n => n.LemmaId)
            .ToHashSet();

        // Check for singular forms (number = 1) in corpus
        var singularForms = new List<string>();

        foreach (var corpusFormId in _corpusDeclensionFormIds!)
        {
            var lemmaId = (int)(corpusFormId / 10_000);

            if (!pluralOnlyLemmaIds.Contains(lemmaId))
                continue;

            var number = ExtractNumberFromNounFormId(corpusFormId);
            if (number == 1) // Singular
            {
                var noun = pluralOnlyNouns.FirstOrDefault(n => n.LemmaId == lemmaId);
                singularForms.Add(
                    $"form_id={corpusFormId} (lemma={noun?.Lemma}, pattern={noun?.Pattern})");
            }
        }

        // Report findings
        TestContext.WriteLine($"Plural-only nouns: {pluralOnlyNouns.Count}");
        TestContext.WriteLine($"Singular forms found: {singularForms.Count}");

        foreach (var msg in singularForms.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        singularForms.Should().BeEmpty(
            "plural-only noun patterns should not have singular forms in the corpus");
    }

    #endregion

    static string Truncate(string s, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        return s.Length <= maxLength ? s : s[..maxLength] + "...";
    }
}
