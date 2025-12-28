using FluentAssertions;
using PaliPractice.Tests.DataIntegrity.Helpers;

namespace PaliPractice.Tests.DataIntegrity;

/// <summary>
/// Data integrity tests for nouns - verifies all noun data in training.db
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
///    (training.db)                (source of truth for corpus)
/// </code>
/// <para>
/// <b>What each source provides:</b>
/// </para>
/// <list type="bullet">
///   <item><b>dpd.db</b>: Digital Pāḷi Dictionary - the authoritative source for word properties
///     (lemma, stem, pattern, meaning, examples). Gray forms in HTML indicate theoretical inflections.</item>
///   <item><b>training.db</b>: App's extracted database. Properties come from dpd.db,
///     corpus tables (nouns_corpus_forms) come from Tipitaka wordlists.</item>
///   <item><b>Tipitaka wordlists</b>: JSON files (cst, bjt, sya, sc) containing actual words
///     found in Pali canon texts. Used by extraction script to determine InCorpus status.</item>
/// </list>
/// <para>
/// <b>Key transformations verified:</b>
/// </para>
/// <list type="bullet">
///   <item>dpd.lemma_1 → Regex.Replace(@" \d.*$", "") → training.lemma</item>
///   <item>dpd.stem → Regex.Replace(@"[!*]", "") → training.stem (removes DPD markers)</item>
///   <item>dpd.pos → masc=1, nt=2, fem=3 → training.gender</item>
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
    TrainingDbLoader? _trainingDb;
    DpdWordLoader? _dpdDb;
    Dictionary<int, DpdHeadword>? _dpdHeadwords;
    List<TrainingNoun>? _trainingNouns;
    List<TrainingNounDetails>? _trainingNounDetails;
    Dictionary<int, TrainingNounDetails>? _nounDetailsById;
    HashSet<long>? _corpusDeclensionFormIds;
    HashSet<string>? _tipitakaWords;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _trainingDb = new TrainingDbLoader();
        _dpdDb = new DpdWordLoader();

        // Load all data upfront for performance
        _dpdHeadwords = _dpdDb.GetAllHeadwordsById();
        _trainingNouns = _trainingDb.GetAllNouns();
        _trainingNounDetails = _trainingDb.GetAllNounDetails();
        _nounDetailsById = _trainingNounDetails.ToDictionary(d => d.Id);
        _corpusDeclensionFormIds = _trainingDb.GetCorpusDeclensionFormIds();
        _tipitakaWords = TipitakaWordlistLoader.GetAllWords();

        TestContext.WriteLine($"Loaded {_trainingNouns.Count} nouns from training.db");
        TestContext.WriteLine($"Loaded {_trainingNounDetails.Count} noun details from training.db");
        TestContext.WriteLine($"Loaded {_dpdHeadwords.Count} headwords from dpd.db");
        TestContext.WriteLine($"Loaded {_corpusDeclensionFormIds.Count} corpus declension form_ids");
        TestContext.WriteLine($"Loaded {_tipitakaWords.Count} Tipitaka words");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _trainingDb?.Dispose();
        _dpdDb?.Dispose();
    }

    #region Core Property Tests

    [Test]
    public void AllNouns_ExistInDpd()
    {
        var missing = _trainingNouns!
            .Where(n => !_dpdHeadwords!.ContainsKey(n.Id))
            .Select(n => $"id={n.Id}, lemma={n.Lemma}")
            .ToList();

        missing.Should().BeEmpty(
            "every noun in training.db should exist in dpd.db");
    }

    [Test]
    public void AllNouns_LemmaMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var noun in _trainingNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue; // Tested separately in AllNouns_ExistInDpd

            if (noun.Lemma != dpd.LemmaClean)
            {
                mismatches.Add($"id={noun.Id}: training='{noun.Lemma}' vs dpd='{dpd.LemmaClean}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns.lemma should match dpd.lemma_clean (lemma_1 with ' N...' suffix removed)");
    }

    [Test]
    public void AllNouns_PatternMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var noun in _trainingNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            if (noun.Pattern != dpd.Pattern)
            {
                mismatches.Add($"id={noun.Id} ({noun.Lemma}): training='{noun.Pattern}' vs dpd='{dpd.Pattern}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns.pattern should match dpd.pattern exactly");
    }

    [Test]
    public void AllNouns_EbtCountMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var noun in _trainingNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            if (noun.EbtCount != dpd.EbtCount)
            {
                mismatches.Add($"id={noun.Id} ({noun.Lemma}): training={noun.EbtCount} vs dpd={dpd.EbtCount}");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns.ebt_count should match dpd.ebt_count exactly");
    }

    [Test]
    public void AllNouns_StemMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var noun in _trainingNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            // training.db stores stem after removing !* markers
            if (noun.Stem != dpd.StemClean)
            {
                mismatches.Add($"id={noun.Id} ({noun.Lemma}): training='{noun.Stem}' vs dpd='{dpd.StemClean}' (raw: '{dpd.Stem}')");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns.stem should match dpd.stem after removing !* markers");
    }

    [Test]
    public void AllNouns_GenderMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var noun in _trainingNouns!)
        {
            if (!_dpdHeadwords!.TryGetValue(noun.Id, out var dpd))
                continue;

            var expectedGender = PosToGender(dpd.Pos);
            if (noun.Gender != expectedGender)
            {
                mismatches.Add($"id={noun.Id} ({noun.Lemma}): training={noun.Gender} vs expected={expectedGender} (pos='{dpd.Pos}')");
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
        var missing = _trainingNouns!
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

        foreach (var details in _trainingNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Meaning != dpd.Meaning1)
            {
                mismatches.Add($"id={details.Id}: training='{Truncate(details.Meaning)}' vs dpd='{Truncate(dpd.Meaning1)}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.meaning should match dpd.meaning_1 exactly");
    }

    [Test]
    public void AllNounDetails_Source1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Source1 != dpd.Source1)
            {
                mismatches.Add($"id={details.Id}: training='{details.Source1}' vs dpd='{dpd.Source1}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.source_1 should match dpd.source_1 exactly");
    }

    [Test]
    public void AllNounDetails_Sutta1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Sutta1 != dpd.Sutta1)
            {
                mismatches.Add($"id={details.Id}: training='{details.Sutta1}' vs dpd='{dpd.Sutta1}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.sutta_1 should match dpd.sutta_1 exactly");
    }

    [Test]
    public void AllNounDetails_Example1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Example1 != dpd.Example1)
            {
                mismatches.Add($"id={details.Id}: training='{Truncate(details.Example1)}' vs dpd='{Truncate(dpd.Example1)}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.example_1 should match dpd.example_1 exactly");
    }

    [Test]
    public void AllNounDetails_Source2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Source2 != dpd.Source2)
            {
                mismatches.Add($"id={details.Id}: training='{details.Source2}' vs dpd='{dpd.Source2}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.source_2 should match dpd.source_2 exactly");
    }

    [Test]
    public void AllNounDetails_Sutta2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Sutta2 != dpd.Sutta2)
            {
                mismatches.Add($"id={details.Id}: training='{details.Sutta2}' vs dpd='{dpd.Sutta2}'");
            }
        }

        mismatches.Should().BeEmpty(
            "nouns_details.sutta_2 should match dpd.sutta_2 exactly");
    }

    [Test]
    public void AllNounDetails_Example2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingNounDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Example2 != dpd.Example2)
            {
                mismatches.Add($"id={details.Id}: training='{Truncate(details.Example2)}' vs dpd='{Truncate(dpd.Example2)}'");
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
        var unexpectedInCorpus = new List<string>();

        foreach (var noun in _trainingNouns!)
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
                    unexpectedInCorpus.Add($"{noun.Lemma}: '{form.FullForm}' ({form.Title}) is gray but found in wordlist");
                }
            }
        }

        // Report discrepancies - some are expected due to wordlist/DPD differences
        TestContext.WriteLine($"Gray forms unexpectedly in wordlist: {unexpectedInCorpus.Count}");
        foreach (var msg in unexpectedInCorpus.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        // Allow some discrepancy (DPD gray marking may differ from wordlist coverage)
        // Tightened from 5% to 2% to catch real data issues
        var discrepancyRate = (double)unexpectedInCorpus.Count / _trainingNouns!.Count;
        discrepancyRate.Should().BeLessThan(0.02,
            "less than 2% of words should have gray forms that appear in wordlists");
    }

    [Test]
    public void InCorpus_NonGrayFormsInWordlists()
    {
        var missingFromWordlist = new List<string>();

        foreach (var noun in _trainingNouns!)
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
                    missingFromWordlist.Add($"{noun.Lemma}: '{form.FullForm}' ({form.Title}) is non-gray but missing from wordlist");
                }
            }
        }

        // Report discrepancies
        TestContext.WriteLine($"Non-gray forms missing from wordlist: {missingFromWordlist.Count}");
        foreach (var msg in missingFromWordlist.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        // Allow some discrepancy - tightened from 10% to 5%
        var discrepancyRate = (double)missingFromWordlist.Count / _trainingNouns!.Count;
        discrepancyRate.Should().BeLessThan(0.05,
            "less than 5% of words should have non-gray forms missing from wordlists");
    }

    [Test]
    public void InCorpus_CrossValidateBothSources()
    {
        var totalForms = 0;
        var agreementCount = 0;
        var disagreementSamples = new List<string>();

        foreach (var noun in _trainingNouns!)
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

        foreach (var noun in _trainingNouns!)
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
        var invalid = _trainingNouns!
            .Where(n => n.Gender < 1 || n.Gender > 3)
            .Select(n => $"id={n.Id} ({n.Lemma}): gender={n.Gender}")
            .ToList();

        invalid.Should().BeEmpty(
            "all nouns should have gender 1 (masc), 2 (nt), or 3 (fem)");
    }

    [Test]
    public void AllNouns_HaveNonEmptyPattern()
    {
        var empty = _trainingNouns!
            .Where(n => string.IsNullOrEmpty(n.Pattern))
            .Select(n => $"id={n.Id} ({n.Lemma})")
            .ToList();

        empty.Should().BeEmpty(
            "all nouns should have a non-empty pattern");
    }

    [Test]
    public void AllNounDetails_HaveNonEmptyMeaning()
    {
        var empty = _trainingNounDetails!
            .Where(d => string.IsNullOrEmpty(d.Meaning))
            .Select(d => $"id={d.Id}")
            .ToList();

        empty.Should().BeEmpty(
            "all noun details should have a non-empty meaning (filtered during extraction)");
    }

    #endregion

    static string Truncate(string s, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        return s.Length <= maxLength ? s : s[..maxLength] + "...";
    }
}
