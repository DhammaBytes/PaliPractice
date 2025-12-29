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
        CurrentExample = PickRandomExample([]);
    }

    /// <summary>
    /// Picks a new random example for reference display, preferring examples
    /// that don't contain any of the forms to avoid (answer forms).
    /// </summary>
    public void ShuffleReference(IReadOnlyList<string> formsToAvoid)
    {
        CurrentExample = PickRandomExample(formsToAvoid);
    }

    ExampleEntry PickRandomExample(IReadOnlyList<string> formsToAvoid)
    {
        if (Examples.Count == 0)
            throw new InvalidOperationException("TranslationEntry must have at least one example");
        if (Examples.Count == 1)
            return Examples[0];

        // Prefer examples that don't contain any answer forms
        if (formsToAvoid.Count > 0)
        {
            var safeExamples = Examples
                .Where(e => !ContainsAnyForm(e.Example, formsToAvoid))
                .ToList();

            if (safeExamples.Count < Examples.Count)
                System.Diagnostics.Debug.WriteLine("Example excluded due to answer form");

            if (safeExamples.Count > 0)
                return safeExamples[_random.Next(safeExamples.Count)];
        }

        // Fallback: pick any example if all contain answer forms
        return Examples[_random.Next(Examples.Count)];
    }

    static bool ContainsAnyForm(string? text, IReadOnlyList<string> forms)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var form in forms)
        {
            if (!string.IsNullOrEmpty(form) && text.Contains(form, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Builds translation entry from a single word details.
    /// </summary>
    public static IReadOnlyList<TranslationEntry> BuildFromDetails(IWordDetails details)
    {
        return BuildFromAllDetails([details]);
    }

    /// <summary>
    /// Builds translation entries from all word variants (all details for a lemma).
    /// Groups by unique meaning, collecting all examples for each.
    /// </summary>
    public static IReadOnlyList<TranslationEntry> BuildFromAllDetails(IEnumerable<IWordDetails> detailsCollection)
    {
        var examplesByMeaning = new Dictionary<string, List<ExampleEntry>>();
        var firstDetailsByMeaning = new Dictionary<string, IWordDetails>();

        foreach (var details in detailsCollection)
        {
            var meaning = details.Meaning ?? string.Empty;
            if (string.IsNullOrWhiteSpace(meaning))
                continue;

            if (!examplesByMeaning.TryGetValue(meaning, out var examples))
            {
                examples = [];
                examplesByMeaning[meaning] = examples;
                firstDetailsByMeaning[meaning] = details;
            }

            // Add examples that have references
            if (!string.IsNullOrEmpty(details.Example1))
                examples.Add(new ExampleEntry(details, 0));
            if (!string.IsNullOrEmpty(details.Example2))
                examples.Add(new ExampleEntry(details, 1));
        }

        // Build entries, ensuring each has at least one example for reference
        var entries = new List<TranslationEntry>();
        foreach (var (meaning, examples) in examplesByMeaning)
        {
            if (examples.Count > 0)
            {
                entries.Add(new TranslationEntry(meaning, examples));
            }
            else if (firstDetailsByMeaning.TryGetValue(meaning, out var firstDetails))
            {
                // Create a dummy entry with the first details if no examples
                entries.Add(new TranslationEntry(meaning, [new ExampleEntry(firstDetails, 0)]));
            }
        }

        return entries;
    }
}
