using PaliPractice.Models;
using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Repositories;

namespace PaliPractice.Tests.Practice.Fakes;

/// <summary>
/// Fake implementation of IVerbRepository for testing.
/// Stores lemmas and attested forms in memory.
/// </summary>
public class FakeVerbRepository : IVerbRepository
{
    readonly List<ILemma> _lemmas = [];
    readonly HashSet<long> _attestedFormIds = [];
    readonly HashSet<int> _nonReflexiveLemmaIds = [];
    readonly Dictionary<long, string> _irregularForms = [];

    /// <summary>
    /// Adds a lemma to the repository.
    /// </summary>
    public void AddLemma(ILemma lemma)
    {
        _lemmas.Add(lemma);
    }

    /// <summary>
    /// Marks a lemma as non-reflexive (no reflexive forms available).
    /// </summary>
    public void MarkAsNonReflexive(int lemmaId)
    {
        _nonReflexiveLemmaIds.Add(lemmaId);
    }

    /// <summary>
    /// Marks a specific form as attested in the corpus.
    /// </summary>
    public void AddAttestedForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive, int endingId = 1)
    {
        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        var formId = Conjugation.ResolveId(lemmaId, tense, person, number, voice, endingId);
        _attestedFormIds.Add(formId);
    }

    /// <summary>
    /// Adds all standard tense/person/number combinations as attested for a verb (active voice).
    /// Skips Present 3rd singular as it's used as the citation form.
    /// </summary>
    public void AddAllAttestedForms(int lemmaId, bool includeReflexive = false)
    {
        foreach (Tense tense in Enum.GetValues<Tense>())
        {
            if (tense == Tense.None) continue;
            foreach (Person person in Enum.GetValues<Person>())
            {
                if (person == Person.None) continue;
                foreach (Number number in Enum.GetValues<Number>())
                {
                    if (number == Number.None) continue;

                    // Skip Present 3rd singular Active (citation form)
                    if (tense == Tense.Present && person == Person.Third && number == Number.Singular)
                        continue;

                    AddAttestedForm(lemmaId, tense, person, number, reflexive: false);

                    if (includeReflexive && !_nonReflexiveLemmaIds.Contains(lemmaId))
                        AddAttestedForm(lemmaId, tense, person, number, reflexive: true);
                }
            }
        }
    }

    // IVerbRepository implementation

    public int GetCount() => _lemmas.Count;

    public ILemma? GetLemma(int lemmaId) =>
        _lemmas.FirstOrDefault(l => l.LemmaId == lemmaId);

    public List<ILemma> GetLemmasByRank(int minRank, int maxRank) =>
        _lemmas
            .OrderByDescending(l => l.EbtCount)
            .Skip(minRank - 1)
            .Take(maxRank - minRank + 1)
            .ToList();

    public bool HasReflexive(int lemmaId) =>
        !_nonReflexiveLemmaIds.Contains(lemmaId);

    public bool HasAttestedForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive)
    {
        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        var baseFormId = Conjugation.ResolveId(lemmaId, tense, person, number, voice, 0);
        // Check endings 1-7
        for (int endingId = 1; endingId <= 7; endingId++)
        {
            if (_attestedFormIds.Contains(baseFormId + endingId))
                return true;
        }
        return false;
    }

    public bool IsFormInCorpus(int lemmaId, Tense tense, Person person, Number number, bool reflexive, int endingIndex)
    {
        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        var formId = Conjugation.ResolveId(lemmaId, tense, person, number, voice, endingIndex);
        return _attestedFormIds.Contains(formId);
    }

    public List<string> GetIrregularForms(int lemmaId, Tense tense, Person person, Number number, bool reflexive)
    {
        var forms = new List<string>();
        var voice = reflexive ? Voice.Reflexive : Voice.Active;
        var baseFormId = Conjugation.ResolveId(lemmaId, tense, person, number, voice, 0);
        for (int endingId = 1; endingId <= 7; endingId++)
        {
            if (_irregularForms.TryGetValue(baseFormId + endingId, out var form))
                forms.Add(form);
        }
        return forms;
    }

    public void EnsureDetails(ILemma lemma)
    {
        // No-op for tests - details are pre-loaded
    }
}
