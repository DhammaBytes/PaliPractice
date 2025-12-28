using FluentAssertions;
using PaliPractice.Tests.DataIntegrity.Helpers;

namespace PaliPractice.Tests.DataIntegrity;

/// <summary>
/// Data integrity tests for verbs - verifies all verb data in training.db
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
    TrainingDbLoader? _trainingDb;
    DpdWordLoader? _dpdDb;
    Dictionary<int, DpdHeadword>? _dpdHeadwords;
    List<TrainingVerb>? _trainingVerbs;
    List<TrainingVerbDetails>? _trainingVerbDetails;
    Dictionary<int, TrainingVerbDetails>? _verbDetailsById;
    HashSet<long>? _corpusConjugationFormIds;
    HashSet<int>? _nonReflexiveLemmaIds;
    HashSet<string>? _tipitakaWords;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _trainingDb = new TrainingDbLoader();
        _dpdDb = new DpdWordLoader();

        // Load all data upfront for performance
        _dpdHeadwords = _dpdDb.GetAllHeadwordsById();
        _trainingVerbs = _trainingDb.GetAllVerbs();
        _trainingVerbDetails = _trainingDb.GetAllVerbDetails();
        _verbDetailsById = _trainingVerbDetails.ToDictionary(d => d.Id);
        _corpusConjugationFormIds = _trainingDb.GetCorpusConjugationFormIds();
        _nonReflexiveLemmaIds = _trainingDb.GetNonReflexiveLemmaIds();
        _tipitakaWords = TipitakaWordlistLoader.GetAllWords();

        TestContext.WriteLine($"Loaded {_trainingVerbs.Count} verbs from training.db");
        TestContext.WriteLine($"Loaded {_trainingVerbDetails.Count} verb details from training.db");
        TestContext.WriteLine($"Loaded {_dpdHeadwords.Count} headwords from dpd.db");
        TestContext.WriteLine($"Loaded {_corpusConjugationFormIds.Count} corpus conjugation form_ids");
        TestContext.WriteLine($"Loaded {_nonReflexiveLemmaIds.Count} non-reflexive verb lemma_ids");
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
    public void AllVerbs_ExistInDpd()
    {
        var missing = _trainingVerbs!
            .Where(v => !_dpdHeadwords!.ContainsKey(v.Id))
            .Select(v => $"id={v.Id}, lemma={v.Lemma}")
            .ToList();

        missing.Should().BeEmpty(
            "every verb in training.db should exist in dpd.db");
    }

    [Test]
    public void AllVerbs_LemmaMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var verb in _trainingVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            if (verb.Lemma != dpd.LemmaClean)
            {
                mismatches.Add($"id={verb.Id}: training='{verb.Lemma}' vs dpd='{dpd.LemmaClean}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs.lemma should match dpd.lemma_clean (lemma_1 with ' N...' suffix removed)");
    }

    [Test]
    public void AllVerbs_PatternMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var verb in _trainingVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            if (verb.Pattern != dpd.Pattern)
            {
                mismatches.Add($"id={verb.Id} ({verb.Lemma}): training='{verb.Pattern}' vs dpd='{dpd.Pattern}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs.pattern should match dpd.pattern exactly");
    }

    [Test]
    public void AllVerbs_EbtCountMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var verb in _trainingVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            if (verb.EbtCount != dpd.EbtCount)
            {
                mismatches.Add($"id={verb.Id} ({verb.Lemma}): training={verb.EbtCount} vs dpd={dpd.EbtCount}");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs.ebt_count should match dpd.ebt_count exactly");
    }

    [Test]
    public void AllVerbs_StemMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var verb in _trainingVerbs!)
        {
            if (!_dpdHeadwords!.TryGetValue(verb.Id, out var dpd))
                continue;

            // training.db stores stem after removing !* markers
            if (verb.Stem != dpd.StemClean)
            {
                mismatches.Add($"id={verb.Id} ({verb.Lemma}): training='{verb.Stem}' vs dpd='{dpd.StemClean}' (raw: '{dpd.Stem}')");
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
        var missing = _trainingVerbs!
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

        foreach (var details in _trainingVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Meaning != dpd.Meaning1)
            {
                mismatches.Add($"id={details.Id}: training='{Truncate(details.Meaning)}' vs dpd='{Truncate(dpd.Meaning1)}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.meaning should match dpd.meaning_1 exactly");
    }

    [Test]
    public void AllVerbDetails_Source1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Source1 != dpd.Source1)
            {
                mismatches.Add($"id={details.Id}: training='{details.Source1}' vs dpd='{dpd.Source1}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.source_1 should match dpd.source_1 exactly");
    }

    [Test]
    public void AllVerbDetails_Sutta1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Sutta1 != dpd.Sutta1)
            {
                mismatches.Add($"id={details.Id}: training='{details.Sutta1}' vs dpd='{dpd.Sutta1}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.sutta_1 should match dpd.sutta_1 exactly");
    }

    [Test]
    public void AllVerbDetails_Example1MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Example1 != dpd.Example1)
            {
                mismatches.Add($"id={details.Id}: training='{Truncate(details.Example1)}' vs dpd='{Truncate(dpd.Example1)}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.example_1 should match dpd.example_1 exactly");
    }

    [Test]
    public void AllVerbDetails_Source2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Source2 != dpd.Source2)
            {
                mismatches.Add($"id={details.Id}: training='{details.Source2}' vs dpd='{dpd.Source2}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.source_2 should match dpd.source_2 exactly");
    }

    [Test]
    public void AllVerbDetails_Sutta2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Sutta2 != dpd.Sutta2)
            {
                mismatches.Add($"id={details.Id}: training='{details.Sutta2}' vs dpd='{dpd.Sutta2}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.sutta_2 should match dpd.sutta_2 exactly");
    }

    [Test]
    public void AllVerbDetails_Example2MatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Example2 != dpd.Example2)
            {
                mismatches.Add($"id={details.Id}: training='{Truncate(details.Example2)}' vs dpd='{Truncate(dpd.Example2)}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.example_2 should match dpd.example_2 exactly");
    }

    [Test]
    public void AllVerbDetails_VerbTypeMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.VerbType != dpd.Verb)
            {
                mismatches.Add($"id={details.Id}: training='{details.VerbType}' vs dpd='{dpd.Verb}'");
            }
        }

        mismatches.Should().BeEmpty(
            "verbs_details.type should match dpd.verb exactly");
    }

    [Test]
    public void AllVerbDetails_TransMatchesDpd()
    {
        var mismatches = new List<string>();

        foreach (var details in _trainingVerbDetails!)
        {
            if (!_dpdHeadwords!.TryGetValue(details.Id, out var dpd))
                continue;

            if (details.Trans != dpd.Trans)
            {
                mismatches.Add($"id={details.Id}: training='{details.Trans}' vs dpd='{dpd.Trans}'");
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
        var unexpectedInCorpus = new List<string>();

        foreach (var verb in _trainingVerbs!)
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
                    unexpectedInCorpus.Add($"{verb.Lemma}: '{form.FullForm}' ({form.Title}) is gray but found in wordlist");
                }
            }
        }

        // Report discrepancies
        TestContext.WriteLine($"Gray forms unexpectedly in wordlist: {unexpectedInCorpus.Count}");
        foreach (var msg in unexpectedInCorpus.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        // Tightened from 5% to 2% to catch real data issues
        var discrepancyRate = (double)unexpectedInCorpus.Count / _trainingVerbs!.Count;
        discrepancyRate.Should().BeLessThan(0.02,
            "less than 2% of words should have gray forms that appear in wordlists");
    }

    [Test]
    public void InCorpus_NonGrayFormsInWordlists()
    {
        var missingFromWordlist = new List<string>();

        foreach (var verb in _trainingVerbs!)
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
                    missingFromWordlist.Add($"{verb.Lemma}: '{form.FullForm}' ({form.Title}) is non-gray but missing from wordlist");
                }
            }
        }

        // Report discrepancies
        TestContext.WriteLine($"Non-gray forms missing from wordlist: {missingFromWordlist.Count}");
        foreach (var msg in missingFromWordlist.Take(20))
        {
            TestContext.WriteLine($"  {msg}");
        }

        // Tightened from 10% to 5%
        var discrepancyRate = (double)missingFromWordlist.Count / _trainingVerbs!.Count;
        discrepancyRate.Should().BeLessThan(0.05,
            "less than 5% of words should have non-gray forms missing from wordlists");
    }

    [Test]
    public void InCorpus_CrossValidateBothSources()
    {
        var totalForms = 0;
        var agreementCount = 0;
        var disagreementSamples = new List<string>();

        foreach (var verb in _trainingVerbs!)
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
        var verbsByLemmaId = _trainingVerbs!.GroupBy(v => v.LemmaId);

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

        foreach (var verb in _trainingVerbs!)
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
        var empty = _trainingVerbs!
            .Where(v => string.IsNullOrEmpty(v.Pattern))
            .Select(v => $"id={v.Id} ({v.Lemma})")
            .ToList();

        empty.Should().BeEmpty(
            "all verbs should have a non-empty pattern");
    }

    [Test]
    public void AllVerbDetails_HaveNonEmptyMeaning()
    {
        var empty = _trainingVerbDetails!
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
        var outOfRange = _trainingVerbs!
            .Where(v => v.LemmaId < 70001 || v.LemmaId > 99999)
            .Select(v => $"id={v.Id} ({v.Lemma}): lemma_id={v.LemmaId}")
            .ToList();

        outOfRange.Should().BeEmpty(
            "all verb lemma_ids should be in range 70001-99999");
    }

    #endregion

    static string Truncate(string s, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        return s.Length <= maxLength ? s : s[..maxLength] + "...";
    }
}
