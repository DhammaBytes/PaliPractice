namespace PaliPractice.Models;

/// <summary>
/// Implementation of ILemma that groups words by lemma_clean.
/// </summary>
public class Lemma : ILemma
{
    readonly List<IWord> _words;

    public string LemmaClean { get; }
    public IReadOnlyList<IWord> Words => _words;
    public IWord PrimaryWord { get; }
    public int EbtCount => PrimaryWord.EbtCount;

    public Lemma(string lemmaClean, IEnumerable<IWord> words)
    {
        LemmaClean = lemmaClean;
        _words = words.OrderByDescending(w => w.EbtCount).ToList();
        PrimaryWord = _words.First();
    }
}
