using PaliPractice.Models.Words;

namespace PaliPractice.Presentation.Practice.ViewModels.Common.Entries;

/// <summary>
/// Represents a unique translation (meaning) with its associated references.
/// Used for cycling through translations in the practice carousel.
/// </summary>
public class TranslationEntry
{
    static readonly Random _random = new();

    /// <summary>
    /// The translation/meaning text.
    /// </summary>
    public string Meaning { get; }

    /// <summary>
    /// All references (example+sutta combinations) for this translation.
    /// </summary>
    public IReadOnlyList<ExampleEntry> Examples { get; }

    /// <summary>
    /// The currently selected example for reference display.
    /// </summary>
    public ExampleEntry CurrentExample { get; private set; }

    public TranslationEntry(string meaning, IReadOnlyList<ExampleEntry> examples)
    {
        Meaning = meaning;
        Examples = examples;
        CurrentExample = PickRandomExample();
    }

    /// <summary>
    /// Picks a new random example for reference display.
    /// </summary>
    public void ShuffleReference()
    {
        CurrentExample = PickRandomExample();
    }

    ExampleEntry PickRandomExample()
    {
        if (Examples.Count == 0)
            throw new InvalidOperationException("TranslationEntry must have at least one example");
        if (Examples.Count == 1)
            return Examples[0];
        return Examples[_random.Next(Examples.Count)];
    }

    /// <summary>
    /// Builds translation entries from all words under a lemma.
    /// Groups by unique meaning, collecting all examples for each.
    /// </summary>
    public static IReadOnlyList<TranslationEntry> BuildFromLemma(ILemma lemma)
    {
        return BuildFromWords(lemma.Words);
    }

    /// <summary>
    /// Builds translation entry from a single word.
    /// </summary>
    public static IReadOnlyList<TranslationEntry> BuildFromWord(IWord word)
    {
        return BuildFromWords([word]);
    }

    /// <summary>
    /// Builds translation entries from a collection of words.
    /// Groups by unique meaning, collecting all examples for each.
    /// </summary>
    static IReadOnlyList<TranslationEntry> BuildFromWords(IEnumerable<IWord> words)
    {
        var examplesByMeaning = new Dictionary<string, List<ExampleEntry>>();
        var firstWordByMeaning = new Dictionary<string, IWord>();

        foreach (var word in words)
        {
            var meaning = word.Meaning ?? string.Empty;
            if (string.IsNullOrWhiteSpace(meaning))
                continue;

            if (!examplesByMeaning.TryGetValue(meaning, out var examples))
            {
                examples = [];
                examplesByMeaning[meaning] = examples;
                firstWordByMeaning[meaning] = word;
            }

            // Add examples that have references
            if (!string.IsNullOrEmpty(word.Example1))
                examples.Add(new ExampleEntry(word, 0));
            if (!string.IsNullOrEmpty(word.Example2))
                examples.Add(new ExampleEntry(word, 1));
        }

        // Build entries, ensuring each has at least one example for reference
        var entries = new List<TranslationEntry>();
        foreach (var (meaning, examples) in examplesByMeaning)
        {
            if (examples.Count > 0)
            {
                entries.Add(new TranslationEntry(meaning, examples));
            }
            else if (firstWordByMeaning.TryGetValue(meaning, out var firstWord))
            {
                // Create a dummy entry with the first word if no examples
                entries.Add(new TranslationEntry(meaning, [new ExampleEntry(firstWord, 0)]));
            }
        }

        return entries;
    }
}
