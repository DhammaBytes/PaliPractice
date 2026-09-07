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

        // Validate DPD HTML structure hasn't changed (fail loudly if it has)
        var sampleHtml = _dpdHeadwords.Values
            .FirstOrDefault(h => !string.IsNullOrEmpty(h.InflectionsHtml))?.InflectionsHtml;
        if (sampleHtml != null)
        {
            InCorpusValidator.ValidateHtmlStructure(sampleHtml);
            TestContext.WriteLine("DPD HTML structure validation: PASSED");
        }

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

    static int PosToGender(string pos) => (int)NounEndings.PosToGender(pos);

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
    public void StoredCorpusFormsExactlyMatchPinnedWordlistMembership()
    {
        var forms = _paliDb!.GetNounCorpusForms();
        forms.Should().NotBeEmpty();
        forms.Where(form => !_tipitakaWords!.Contains(form.Form)).Should().BeEmpty(
            "stored attestation must refer to the actual rendered string; upstream HTML gray status is not the corpus contract");
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

    [Test]
    public void RussianMeaningStorageMatchesTheDeclaredLanguageLayer()
    {
        var translated = _paliNounDetails!.Count(d => !string.IsNullOrWhiteSpace(d.MeaningRu));
        if (TestPaths.IsEnglishCandidate)
            translated.Should().Be(0, "core English extraction must not import translations");
        else
            translated.Should().BeGreaterThan(0, "the existing Russian-capable bundle must retain translations");
    }

    #endregion

    #region Irregular Form Tests

    // Plural-only patterns (end with " pl")
    static bool IsPluralOnlyPattern(string pattern) =>
        pattern.Trim().EndsWith(" pl", StringComparison.OrdinalIgnoreCase);

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
        var irregular = _paliDb!.GetNounIrregularForms().ToHashSet();
        irregular.Should().NotBeEmpty();
        var corpus = _paliDb.GetNounCorpusForms()
            .Where(form => _dpdIrregularNounPatterns!.Contains(form.Pattern));
        corpus.Where(form => !irregular.Contains(form)).Should().BeEmpty(
            "every irregular corpus record must match the same headword, grammar ID, and rendered form exactly");
    }

    [Test]
    public void VariantNouns_ShouldNotHaveIrregularFormEntries()
    {
        _paliDb!.GetNounIrregularForms()
            .Where(form => !_dpdIrregularNounPatterns!.Contains(form.Pattern))
            .Should().BeEmpty("regular and variant headwords must not be stored as irregular forms");
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

        // Check for singular forms in corpus
        var singularForms = new List<string>();

        foreach (var corpusFormId in _corpusDeclensionFormIds!)
        {
            var parsed = Declension.ParseId((int)corpusFormId);

            if (!pluralOnlyLemmaIds.Contains(parsed.LemmaId))
                continue;

            if (parsed.Number == Number.Singular)
            {
                var noun = pluralOnlyNouns.FirstOrDefault(n => n.LemmaId == parsed.LemmaId);
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
