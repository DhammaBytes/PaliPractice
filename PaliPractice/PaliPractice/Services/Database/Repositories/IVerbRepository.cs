namespace PaliPractice.Services.Database.Repositories;

/// <summary>
/// Interface for verb repository operations.
/// Enables testing with fake implementations.
/// </summary>
public interface IVerbRepository : ILemmaRepository
{
    /// <summary>
    /// Check if a verb lemma has reflexive conjugation forms.
    /// </summary>
    bool HasReflexive(int lemmaId);

    /// <summary>
    /// Check if any ending variant is attested for this verb form.
    /// </summary>
    bool HasAttestedForm(int lemmaId, Tense tense, Person person, Number number, bool reflexive);

    /// <summary>
    /// Check if a specific verb form appears in the corpus.
    /// </summary>
    bool IsFormInCorpus(int lemmaId, Tense tense, Person person, Number number, bool reflexive, int endingIndex);

    /// <summary>
    /// Get all irregular verb forms for a specific grammatical combination.
    /// </summary>
    List<string> GetIrregularForms(int lemmaId, Tense tense, Person person, Number number, bool reflexive);
}
