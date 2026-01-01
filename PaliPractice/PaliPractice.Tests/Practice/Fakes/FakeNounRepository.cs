using PaliPractice.Models;
using PaliPractice.Models.Inflection;
using PaliPractice.Models.Words;
using PaliPractice.Services.Database.Repositories;

namespace PaliPractice.Tests.Practice.Fakes;

/// <summary>
/// Fake implementation of INounRepository for testing.
/// Stores lemmas and attested forms in memory.
/// </summary>
public class FakeNounRepository : INounRepository
{
    readonly List<ILemma> _lemmas = [];
    readonly HashSet<int> _attestedFormIds = [];
    readonly Dictionary<int, string> _irregularForms = [];

    /// <summary>
    /// Adds a lemma to the repository.
    /// </summary>
    public void AddLemma(ILemma lemma)
    {
        _lemmas.Add(lemma);
    }

    /// <summary>
    /// Marks a specific form as attested in the corpus.
    /// Uses endingId=1 as the default ending.
    /// </summary>
    public void AddAttestedForm(int lemmaId, Case @case, Gender gender, Number number, int endingId = 1)
    {
        var formId = Declension.ResolveId(lemmaId, @case, gender, number, endingId);
        _attestedFormIds.Add(formId);
    }

    /// <summary>
    /// Adds all standard case/number combinations as attested for a noun.
    /// </summary>
    public void AddAllAttestedForms(int lemmaId, Gender gender)
    {
        foreach (Case @case in Enum.GetValues<Case>())
        {
            if (@case == Case.None) continue;
            foreach (Number number in Enum.GetValues<Number>())
            {
                if (number == Number.None) continue;
                AddAttestedForm(lemmaId, @case, gender, number);
            }
        }
    }

    /// <summary>
    /// Adds an irregular form for a specific grammatical combination.
    /// </summary>
    public void AddIrregularForm(int lemmaId, Case @case, Gender gender, Number number, string form, int endingId = 1)
    {
        var formId = Declension.ResolveId(lemmaId, @case, gender, number, endingId);
        _irregularForms[formId] = form;
    }

    // INounRepository implementation

    public int GetCount() => _lemmas.Count;

    public ILemma? GetLemma(int lemmaId) =>
        _lemmas.FirstOrDefault(l => l.LemmaId == lemmaId);

    public List<ILemma> GetLemmasByRank(int minRank, int maxRank) =>
        _lemmas
            .OrderByDescending(l => l.EbtCount)
            .Skip(minRank - 1)
            .Take(maxRank - minRank + 1)
            .ToList();

    public bool HasAttestedForm(int lemmaId, Case @case, Gender gender, Number number)
    {
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        // Check endings 1-6
        for (int endingId = 1; endingId <= 6; endingId++)
        {
            if (_attestedFormIds.Contains(baseFormId + endingId))
                return true;
        }
        return false;
    }

    public bool IsFormInCorpus(int lemmaId, Case @case, Gender gender, Number number, int endingIndex)
    {
        var formId = Declension.ResolveId(lemmaId, @case, gender, number, endingIndex);
        return _attestedFormIds.Contains(formId);
    }

    public List<string> GetIrregularForms(int lemmaId, Case @case, Gender gender, Number number)
    {
        var forms = new List<string>();
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        for (int endingId = 1; endingId <= 6; endingId++)
        {
            if (_irregularForms.TryGetValue(baseFormId + endingId, out var form))
                forms.Add(form);
        }
        return forms;
    }

    public bool HasIrregularForm(int lemmaId, Case @case, Gender gender, Number number)
    {
        var baseFormId = Declension.ResolveId(lemmaId, @case, gender, number, 0);
        for (int endingId = 1; endingId <= 6; endingId++)
        {
            if (_irregularForms.ContainsKey(baseFormId + endingId))
                return true;
        }
        return false;
    }

    public void EnsureDetails(ILemma lemma)
    {
        // No-op for tests - details are pre-loaded
    }
}
