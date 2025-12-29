using System.Text.Json;

namespace PaliPractice.Tests.DataIntegrity.Helpers;

/// <summary>
/// Loads and applies custom translation adjustments from custom_translations.json.
/// Mirrors the Python TranslationAdjustments class logic.
/// </summary>
public class CustomTranslationsLoader
{
    readonly Dictionary<int, (string Lemma1, string Preferred)> _adjustments = new();

    public const string DefaultPath = "/Users/ivm/Sources/PaliPractice/scripts/configs/custom_translations.json";

    public CustomTranslationsLoader(string? path = null)
    {
        Load(path ?? DefaultPath);
    }

    void Load(string path)
    {
        if (!File.Exists(path))
            return;

        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out var id))
                continue;

            var lemma1 = prop.Value.TryGetProperty("lemma_1", out var l) ? l.GetString() ?? "" : "";
            var preferred = prop.Value.TryGetProperty("preferred", out var p) ? p.GetString() ?? "" : "";

            if (!string.IsNullOrEmpty(lemma1) && !string.IsNullOrEmpty(preferred))
            {
                _adjustments[id] = (lemma1, preferred);
            }
        }
    }

    /// <summary>
    /// Checks if a custom translation exists for the given ID.
    /// </summary>
    public bool HasAdjustment(int id) => _adjustments.ContainsKey(id);

    /// <summary>
    /// Apply custom translation adjustment if one exists for this lemma.
    /// Mirrors Python TranslationAdjustments.apply() logic.
    /// </summary>
    /// <param name="lemmaId">The DPD headword ID</param>
    /// <param name="lemma1">The DPD lemma_1 value (e.g., "paññā 1")</param>
    /// <param name="meaning">The original meaning string (semicolon-separated)</param>
    /// <returns>Modified meaning string with preferred translation first, or original if no adjustment applies.</returns>
    public string Apply(int lemmaId, string lemma1, string meaning)
    {
        if (!_adjustments.TryGetValue(lemmaId, out var adjustment))
            return meaning;

        var (expectedLemma, preferred) = adjustment;

        // Validate lemma_1 matches
        if (lemma1 != expectedLemma)
            return meaning;

        if (string.IsNullOrEmpty(meaning))
            return preferred;

        // Parse meanings (split by "; ")
        var parts = meaning.Split(';')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        // Check if preferred already exists (case-insensitive search)
        var preferredLower = preferred.ToLowerInvariant();
        var existingIndex = parts.FindIndex(p => p.ToLowerInvariant() == preferredLower);

        if (existingIndex >= 0)
        {
            if (existingIndex == 0)
            {
                // Already first, nothing to do
                return meaning;
            }

            // Remove from current position and prepend
            var actualPreferred = parts[existingIndex]; // Keep original casing
            parts.RemoveAt(existingIndex);
            parts.Insert(0, actualPreferred);
        }
        else
        {
            // Doesn't exist, prepend
            parts.Insert(0, preferred);
        }

        return string.Join("; ", parts);
    }

    /// <summary>
    /// Number of custom translation adjustments loaded.
    /// </summary>
    public int Count => _adjustments.Count;
}
