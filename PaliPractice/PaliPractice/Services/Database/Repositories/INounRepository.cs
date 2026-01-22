namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Interface for noun repository operations.
/// Enables testing with fake implementations.
/// </summary>
public interface INounRepository : ILemmaRepository
{
    /// <summary>
    /// Check if any ending variant is attested for this noun form.
    /// </summary>
    bool HasAttestedForm(int lemmaId, Case @case, Gender gender, Number number);

    /// <summary>
    /// Check if a specific noun form appears in the corpus.
    /// </summary>
    bool IsFormInCorpus(int lemmaId, Case @case, Gender gender, Number number, int endingIndex);

    /// <summary>
    /// Get all irregular noun forms for a specific grammatical combination.
    /// </summary>
    List<string> GetIrregularForms(int lemmaId, Case @case, Gender gender, Number number);

    /// <summary>
    /// Check if irregular forms exist for this noun grammatical combination.
    /// </summary>
    bool HasIrregularForm(int lemmaId, Case @case, Gender gender, Number number);
}
