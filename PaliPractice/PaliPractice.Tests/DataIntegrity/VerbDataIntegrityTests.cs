using FluentAssertions;
using PaliPractice.Models.Inflection;
using PaliPractice.Tests.DataIntegrity.Helpers;

namespace PaliPractice.Tests.DataIntegrity;

/// <summary>
/// Data integrity tests for verbs - verifies all verb data in pali.db
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
///    (pali.db)                  (source of truth for corpus)
/// </code>
/// <para>
/// See <see cref="NounDataIntegrityTests"/> for detailed documentation of the validation approach.
/// </para>
/// <para>
/// <b>Verb-specific validations:</b>
/// </para>
/// <list type="bullet">
///   <item><b>VerbType</b>: verbs_details.type matches dpd.verb</item>
///   <item><b>Transitivity</b>: verbs_details.trans matches dpd.trans (trans/intrans/ditrans)</item>
///   <item><b>NonReflexive</b>: verbs_nonreflexive table entries should have no 'reflx' forms in DPD HTML</item>
///   <item><b>LemmaId range</b>: Verb lemma IDs are in range 70001-99999 (nouns use 10001-69999)</item>
/// </list>
/// </remarks>
[TestFixture]
public class VerbDataIntegrityTests
{
    PaliDbLoader? _paliDb;
    DpdWordLoader? _dpdDb;
    DpdPatternClassifier? _dpdPatterns;
    Dictionary<int, DpdHeadword>? _dpdHeadwords;
    List<PaliVerb>? _paliVerbs;
    List<PaliVerbDetails>? _paliVerbDetails;
    Dictionary<int, PaliVerbDetails>? _verbDetailsById;
    HashSet<long>? _corpusConjugationFormIds;
    HashSet<int>? _nonReflexiveLemmaIds;
    HashSet<string>? _tipitakaWords;
    CustomTranslationsLoader? _customTranslations;

    // DPD-sourced pattern classification (loaded in OneTimeSetUp)
    HashSet<string>? _dpdIrregularVerbPatterns;
    HashSet<string>? _dpdRegularVerbPatterns;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _paliDb = new PaliDbLoader();
        _dpdDb = new DpdWordLoader();
        _dpdPatterns = new DpdPatternClassifier();
        _customTranslations = new CustomTranslationsLoader();

        // Load all data upfront for performance
        _dpdHeadwords = _dpdDb.GetAllHeadwordsById();
        _paliVerbs = _paliDb.GetAllVerbs();
        _paliVerbDetails = _paliDb.GetAllVerbDetails();
        _verbDetailsById = _paliVerbDetails.ToDictionary(d => d.Id);
        _corpusConjugationFormIds = _paliDb.GetCorpusConjugationFormIds();
        _nonReflexiveLemmaIds = _paliDb.GetNonReflexiveLemmaIds();
        _tipitakaWords = TipitakaWordlistLoader.GetAllWords();

        // Load DPD-sourced pattern classification
        _dpdIrregularVerbPatterns = _dpdPatterns.GetIrregularVerbPatterns();
        _dpdRegularVerbPatterns = _dpdPatterns.GetRegularVerbPatterns();

        // Validate DPD HTML structure hasn't changed (fail loudly if it has)
        var sampleHtml = _dpdHeadwords.Values
            .FirstOrDefault(h => !string.IsNullOrEmpty(h.InflectionsHtml))?.InflectionsHtml;
        if (sampleHtml != null)
        {
            InCorpusValidator.ValidateHtmlStructure(sampleHtml);
            TestContext.WriteLine("DPD HTML structure validation: PASSED");
        }

        TestContext.WriteLine($"Loaded {_paliVerbs.Count} verbs from pali.db");
        TestContext.WriteLine($"Loaded {_paliVerbDetails.Count} verb details from pali.db");
        TestContext.WriteLine($"Loaded {_dpdHeadwords.Count} headwords from dpd.db");
        TestContext.WriteLine($"Loaded {_corpusConjugationFormIds.Count} corpus conjugation form_ids");
        TestContext.WriteLine($"Loaded {_nonReflexiveLemmaIds.Count} non-reflexive verb lemma_ids");
        TestContext.WriteLine($"Loaded {_tipitakaWords.Count} Tipitaka words");
        TestContext.WriteLine($"Loaded {_customTranslations.Count} custom translation adjustments");
        TestContext.WriteLine($"DPD irregular verb patterns: {_dpdIrregularVerbPatterns.Count}");
        TestContext.WriteLine($"DPD regular verb patterns: {_dpdRegularVerbPatterns.Count}");
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
    public void AllVerbs_ExistInDpd()
    {
        var missing = _paliVerbs!
            .Where(v => !_dpdHeadwords!.ContainsKey(v.Id))
            .Select(v => $"id={v.Id}, lemma={v.Lemma}")
            .ToList();

        missing.Should().BeEmpty(
            "every verb in pali.db should exist in dpd.db");
    }

    [Test]
    public void AllVerbs_LemmaMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var verb in _paliVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            if (verb.Lemma != dpd.LemmaClean)
            {
                mismatches.Add($"id={verb.Id}: pali='{verb.Lemma}' vs dpd='{dpd.LemmaClean}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs.lemma should match dpd.lemma_clean (lemma_1 with ' N...' suffix removed)");
    }

    [Test]
    public void AllVerbs_PatternMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var verb in _paliVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            if (verb.Pattern != dpd.Pattern)
            {
                mismatches.Add($"id={verb.Id} ({verb.Lemma}): pali='{verb.Pattern}' vs dpd='{dpd.Pattern}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs.pattern should match dpd.pattern exactly");
    }

    [Test]
    public void AllVerbs_EbtCountMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var verb in _paliVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            if (verb.EbtCount != dpd.EbtCount)
            {
                mismatches.Add($"id={verb.Id} ({verb.Lemma}): pali={verb.EbtCount} vs dpd={dpd.EbtCount}");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs.ebt_count should match dpd.ebt_count exactly");
    }

    [Test]
    public void AllVerbs_StemMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var verb in _paliVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            // pali.db stores stem after removing !* markers
            if (verb.Stem != dpd.StemClean)
            {
                mismatches.Add($"id={verb.Id} ({verb.Lemma}): pali='{verb.Stem}' vs dpd='{dpd.StemClean}' (raw: '{dpd.Stem}')");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs.stem should match dpd.stem after removing !* markers");
    }

    #endregion

    #region Details Property Tests

    [Test]
    public void AllVerbDetails_ExistForEachVerb()
    {
        var missing = _paliVerbs!
            .Where(v => !_verbDetailsById!.ContainsKey(v.Id))
            .Select(v => $"id={v.Id}, lemma={v.Lemma}")
            .ToList();

        missing.Should().BeEmpty(
            "every verb should have a corresponding details entry");
    }

    [Test]
    public void AllVerbDetails_MeaningMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliVerbDetails!)
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
            "verbs_details.meaning should match dpd.meaning_1 (with custom translation adjustments applied)");
    }

    [Test]
    public void AllVerbDetails_Source1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Source1 != dpd.Source1)
            {
                mismatches.Add($"id={details.Id}: pali='{details.Source1}' vs dpd='{dpd.Source1}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.source_1 should match dpd.source_1 exactly");
    }

    [Test]
    public void AllVerbDetails_Sutta1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Sutta1 != dpd.Sutta1)
            {
                mismatches.Add($"id={details.Id}: pali='{details.Sutta1}' vs dpd='{dpd.Sutta1}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.sutta_1 should match dpd.sutta_1 exactly");
    }

    [Test]
    public void AllVerbDetails_Example1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Example1 != dpd.Example1)
            {
                mismatches.Add($"id={details.Id}: pali='{Truncate(details.Example1)}' vs dpd='{Truncate(dpd.Example1)}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.example_1 should match dpd.example_1 exactly");
    }

    [Test]
    public void AllVerbDetails_Source2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Source2 != dpd.Source2)
            {
                mismatches.Add($"id={details.Id}: pali='{details.Source2}' vs dpd='{dpd.Source2}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.source_2 should match dpd.source_2 exactly");
    }

    [Test]
    public void AllVerbDetails_Sutta2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Sutta2 != dpd.Sutta2)
            {
                mismatches.Add($"id={details.Id}: pali='{details.Sutta2}' vs dpd='{dpd.Sutta2}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.sutta_2 should match dpd.sutta_2 exactly");
    }

    [Test]
    public void AllVerbDetails_Example2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Example2 != dpd.Example2)
            {
                mismatches.Add($"id={details.Id}: pali='{Truncate(details.Example2)}' vs dpd='{Truncate(dpd.Example2)}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.example_2 should match dpd.example_2 exactly");
    }

    [Test]
    public void AllVerbDetails_VerbTypeMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.VerbType != dpd.Verb)
            {
                mismatches.Add($"id={details.Id}: pali='{details.VerbType}' vs dpd='{dpd.Verb}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.type should match dpd.verb exactly");
    }

    [Test]
    public void AllVerbDetails_TransMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _paliVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Trans != dpd.Trans)
            {
                mismatches.Add($"id={details.Id}: pali='{details.Trans}' vs dpd='{dpd.Trans}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.trans should match dpd.trans exactly");
    }

    #endregion

    #region InCorpus Tests

    [Test]
    public void InCorpus_GrayFormsNotInWordlists()
    {
        var verbsWithIssues = new HashSet<int>();
        var formIssues = new List<string>();

        foreach (var verb in _paliVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            if (string.IsNullOrEmpty(dpd.InflectionsHtml))
                continue;

            var grayForms = InCorpusValidator.GetTheoreticalForms(dpd.InflectionsHtml, verb.Stem);

            foreach (var form in grayForms)
            {
                if (_tipitakaWords!.Contains(form.FullForm))
                {
                    verbsWithIssues.Add(verb.Id);
                    formIssues.Add($"{verb.Lemma}: '{form.FullForm}' ({form.Title}) is gray but found in wordlist");
                }
            }
        }

        // Report discrepancies
        TestContext.WriteLine($"Verbs with gray forms unexpectedly in wordlist: {verbsWithIssues.Count}");
        TestContext.WriteLine($"Total form issues: {formIssues.Count}");
        foreach (var msg in formIssues.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        // Rate is % of verbs that have at least one problematic form
        var discrepancyRate = (double)verbsWithIssues.Count / _paliVerbs!.Count;
        discrepancyRate.Should().BeLessThan(0.02,
            "less than 2% of verbs should have gray forms that appear in wordlists");
    }

    [Test]
    public void InCorpus_NonGrayFormsInWordlists()
    {
        var verbsWithIssues = new HashSet<int>();
        var formIssues = new List<string>();

        foreach (var verb in _paliVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            if (string.IsNullOrEmpty(dpd.InflectionsHtml))
                continue;

            var inCorpusForms = InCorpusValidator.GetInCorpusForms(dpd.InflectionsHtml, verb.Stem);

            foreach (var form in inCorpusForms)
            {
                if (!_tipitakaWords!.Contains(form.FullForm))
                {
                    verbsWithIssues.Add(verb.Id);
                    formIssues.Add($"{verb.Lemma}: '{form.FullForm}' ({form.Title}) is non-gray but missing from wordlist");
                }
            }
        }

        // Report discrepancies
        TestContext.WriteLine($"Verbs with non-gray forms missing from wordlist: {verbsWithIssues.Count}");
        TestContext.WriteLine($"Total form issues: {formIssues.Count}");
        foreach (var msg in formIssues.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        // Rate is % of verbs that have at least one problematic form
        var discrepancyRate = (double)verbsWithIssues.Count / _paliVerbs!.Count;
        discrepancyRate.Should().BeLessThan(0.05,
            "less than 5% of verbs should have non-gray forms missing from wordlists");
    }

    [Test]
    public void InCorpus_CrossValidateBothSources()
    {
        var totalForms = 0;
        var agreementCount = 0;
        var disagreementSamples = new List<string>();

        foreach (var verb in _paliVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            if (string.IsNullOrEmpty(dpd.InflectionsHtml))
                continue;

            var validation = InCorpusValidator.ValidateAgainstWordlist(
                dpd.InflectionsHtml,
                verb.Stem,
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
                        $"{verb.Lemma} '{result.FullForm}' ({result.Title}): " +
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

    #region Non-Reflexive Tests

    [Test]
    public void NonReflexiveVerbs_HaveNoReflexiveFormsInDpdHtml()
    {
        var verbsWithReflexive = new List<string>();

        // Group verbs by lemma_id to check each unique lemma
        var verbsByLemmaId = _paliVerbs!.GroupBy(v => v.LemmaId);

        foreach (var group in verbsByLemmaId)
        {
            var lemmaId = group.Key;

            // Skip if this lemma is NOT in the non-reflexive list (it has reflexive forms)
            if (!_nonReflexiveLemmaIds!.Contains(lemmaId))
                continue;

            // Check that none of the verbs with this lemma_id have reflexive forms in their HTML
            var verb = group.First();
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            if (string.IsNullOrEmpty(dpd.InflectionsHtml))
                continue;

            // Check if HTML contains 'reflx' in any title
            if (dpd.InflectionsHtml.Contains("reflx"))
            {
                verbsWithReflexive.Add($"lemma_id={lemmaId} ({verb.Lemma}): marked non-reflexive but HTML contains 'reflx'");
            }
        }

        verbsWithReflexive.Should().BeEmpty(
            "verbs in verbs_nonreflexive should not have 'reflx' forms in DPD HTML");
    }

    #endregion

    #region Table Consistency Tests

    [Test]
    public void VerbsAndDetails_HaveConsistentLemmaIds()
    {
        var mismatches = new List<string>();

        foreach (var verb in _paliVerbs!)
        {
            if (!_verbDetailsById!.TryGetValue(verb.Id, out var details))
                continue;

            if (verb.LemmaId != details.LemmaId)
            {
                mismatches.Add($"id={verb.Id}: verbs.lemma_id={verb.LemmaId} vs details.lemma_id={details.LemmaId}");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs.lemma_id should match verbs_details.lemma_id for same id");
    }

    [Test]
    public void CorpusConjugations_HasFormIds()
    {
        _corpusConjugationFormIds.Should().NotBeEmpty(
            "verbs_corpus_forms should contain form_ids");

        TestContext.WriteLine($"Corpus conjugation form_ids: {_corpusConjugationFormIds!.Count}");
    }

    [Test]
    public void AllVerbs_HaveNonEmptyPattern()
    {
        var empty = _paliVerbs!
            .Where(v => string.IsNullOrEmpty(v.Pattern))
            .Select(v => $"id={v.Id} ({v.Lemma})")
            .ToList();

        empty.Should().BeEmpty(
            "all verbs should have a non-empty pattern");
    }

    [Test]
    public void AllVerbDetails_HaveNonEmptyMeaning()
    {
        var empty = _paliVerbDetails!
            .Where(d => string.IsNullOrEmpty(d.Meaning))
            .Select(d => $"id={d.Id}")
            .ToList();

        empty.Should().BeEmpty(
            "all verb details should have a non-empty meaning (filtered during extraction)");
    }

    [Test]
    public void AllVerbLemmaIds_AreInExpectedRange()
    {
        // Verb lemma IDs should be in range 70001-99999
        var outOfRange = _paliVerbs!
            .Where(v => v.LemmaId < 70001 || v.LemmaId > 99999)
            .Select(v => $"id={v.Id} ({v.Lemma}): lemma_id={v.LemmaId}")
            .ToList();

        outOfRange.Should().BeEmpty(
            "all verb lemma_ids should be in range 70001-99999");
    }

    #endregion

    #region Irregular Form Tests

    [Test]
    public void IrregularVerbs_CorpusFormsHaveIrregularFormEntries()
    {
        // Get irregular verbs using DPD-sourced patterns
        var irregularVerbs = _paliVerbs!
            .Where(v => _dpdIrregularVerbPatterns!.Contains(v.Pattern))
            .ToList();

        if (irregularVerbs.Count == 0)
        {
            Assert.Inconclusive("No irregular verbs found in pali.db");
            return;
        }

        // Get irregular verb lemma_ids
        var irregularLemmaIds = irregularVerbs
            .Select(v => v.LemmaId)
            .ToHashSet();

        // Get irregular form_ids from the irregular_forms table
        var irregularFormIds = _paliDb!.GetIrregularVerbFormIds();

        // Check that corpus forms for irregular verbs have corresponding irregular_forms entries
        var missingIrregularForms = new List<string>();

        foreach (var corpusFormId in _corpusConjugationFormIds!)
        {
            // Use the authoritative ParseId method from Conjugation model
            var parsed = Conjugation.ParseId(corpusFormId);

            // Skip if not an irregular verb
            if (!irregularLemmaIds.Contains(parsed.LemmaId))
                continue;

            // Check if this corpus form has an irregular form entry
            if (!irregularFormIds.Contains(corpusFormId))
            {
                var verb = irregularVerbs.FirstOrDefault(v => v.LemmaId == parsed.LemmaId);
                missingIrregularForms.Add(
                    $"form_id={corpusFormId} (lemma={verb?.Lemma}, pattern={verb?.Pattern})");
            }
        }

        // Report findings
        var irregularCorpusCount = _corpusConjugationFormIds.Count(f =>
            irregularLemmaIds.Contains(Conjugation.ParseId(f).LemmaId));

        TestContext.WriteLine($"Irregular verbs: {irregularVerbs.Count}");
        TestContext.WriteLine($"Irregular corpus form_ids checked: {irregularCorpusCount}");
        TestContext.WriteLine($"Irregular form entries: {irregularFormIds.Count}");
        TestContext.WriteLine($"Missing irregular form entries: {missingIrregularForms.Count}");

        foreach (var msg in missingIrregularForms.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        // Note: Some mismatch is expected because corpus_forms uses ending indices from
        // template parsing, while irregular_forms uses indices from HTML parsing.
        // The forms are the same but may have different ending_index values.
        var mismatchRate = irregularCorpusCount > 0
            ? (double)missingIrregularForms.Count / irregularCorpusCount
            : 0;
        TestContext.WriteLine($"Mismatch rate: {mismatchRate:P1}");

        // Allow up to 5% mismatch due to index ordering differences between template and HTML
        mismatchRate.Should().BeLessThan(0.05,
            "less than 5% of irregular corpus forms should have index misalignment with irregular_forms table");
    }

    /// <summary>
    /// Verify that our VerbPattern enum classification matches DPD's inflection_templates.
    /// If DPD changes pattern classification, this test will catch it.
    /// </summary>
    [Test]
    public void DpdPatternClassification_MatchesOurEnumClassification()
    {
        // Get our enum's irregular patterns (exclude None and _Irregular marker)
        var enumIrregulars = Enum.GetValues<VerbPattern>()
            .Where(p => !p.IsMarkerOrNone() && p.IsIrregular())
            .Select(p => p.ToDbString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var enumRegulars = Enum.GetValues<VerbPattern>()
            .Where(p => !p.IsMarkerOrNone() && !p.IsIrregular())
            .Select(p => p.ToDbString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Compare against DPD's classification
        var onlyInEnumIrreg = enumIrregulars.Except(_dpdIrregularVerbPatterns!, StringComparer.OrdinalIgnoreCase).ToList();
        var onlyInDpdIrreg = _dpdIrregularVerbPatterns!.Where(p => VerbPatternHelper.TryParse(p, out _))
            .Except(enumIrregulars, StringComparer.OrdinalIgnoreCase).ToList();

        var onlyInEnumReg = enumRegulars.Except(_dpdRegularVerbPatterns!, StringComparer.OrdinalIgnoreCase).ToList();
        var onlyInDpdReg = _dpdRegularVerbPatterns!.Where(p => VerbPatternHelper.TryParse(p, out _))
            .Except(enumRegulars, StringComparer.OrdinalIgnoreCase).ToList();

        // Report differences
        TestContext.WriteLine($"Enum irregular patterns: {enumIrregulars.Count}");
        TestContext.WriteLine($"DPD irregular verb patterns: {_dpdIrregularVerbPatterns!.Count}");
        TestContext.WriteLine($"Enum regular patterns: {enumRegulars.Count}");
        TestContext.WriteLine($"DPD regular verb patterns: {_dpdRegularVerbPatterns!.Count}");

        if (onlyInEnumIrreg.Count > 0)
            TestContext.WriteLine($"Marked irregular in enum but not in DPD: {string.Join(", ", onlyInEnumIrreg)}");
        if (onlyInDpdIrreg.Count > 0)
            TestContext.WriteLine($"Marked irregular in DPD but not in enum: {string.Join(", ", onlyInDpdIrreg)}");
        if (onlyInEnumReg.Count > 0)
            TestContext.WriteLine($"Marked regular in enum but not in DPD: {string.Join(", ", onlyInEnumReg)}");
        if (onlyInDpdReg.Count > 0)
            TestContext.WriteLine($"Marked regular in DPD but not in enum: {string.Join(", ", onlyInDpdReg)}");

        // All patterns we track should match DPD classification
        onlyInEnumIrreg.Should().BeEmpty(
            "all enum irregular patterns should be marked 'irreg' in DPD inflection_templates");
        onlyInDpdIrreg.Should().BeEmpty(
            "all DPD 'irreg' verb patterns we track should be marked irregular in enum");
        onlyInEnumReg.Should().BeEmpty(
            "all enum regular patterns should NOT be marked 'irreg' in DPD");
        onlyInDpdReg.Should().BeEmpty(
            "all DPD regular verb patterns we track should NOT be marked irregular in enum");
    }

    #endregion

    static string Truncate(string s, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        return s.Length <= maxLength ? s : s[..maxLength] + "...";
    }
}
