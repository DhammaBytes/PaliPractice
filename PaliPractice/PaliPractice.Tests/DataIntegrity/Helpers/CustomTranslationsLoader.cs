using System.Text.Json;

namespace PaliPractice.Tests.DataIntegrity.Helpers;

/// <summary>
/// Loads and applies custom translation adjustments from custom_translations.json.
/// Mirrors the Python TranslationAdjustments class logic.
/// </summary>
public class CustomTranslationsLoader
{
    readonly Dictionary<int, PrimaryAdjustment> _primary = new();
    readonly Dictionary<int, ReplaceAdjustment> _replace = new();

    public const string DefaultPath = "/Users/ivm/Sources/PaliPractice/scripts/configs/custom_translations.json";

    record PrimaryAdjustment(string Lemma1, string Preferred);
    record ReplaceAdjustment(string Lemma1, string Target, string Preferred);

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

        // Load "primary" section
        if (doc.RootElement.TryGetProperty("primary", out var primarySection))
        {
            foreach (var prop in primarySection.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var id))
                    continue;

                var lemma1 = prop.Value.TryGetProperty("lemma_1", out var l) ? l.GetString() ?? "" : "";
                var preferred = prop.Value.TryGetProperty("preferred", out var p) ? p.GetString() ?? "" : "";

                if (!string.IsNullOrEmpty(lemma1) && !string.IsNullOrEmpty(preferred))
                {
                    _primary[id] = new PrimaryAdjustment(lemma1, preferred);
                }
            }
        }

        // Load "replace" section
        if (doc.RootElement.TryGetProperty("replace", out var replaceSection))
        {
            foreach (var prop in replaceSection.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var id))
                    continue;

                var lemma1 = prop.Value.TryGetProperty("lemma_1", out var l) ? l.GetString() ?? "" : "";
                var target = prop.Value.TryGetProperty("target", out var t) ? t.GetString() ?? "" : "";
                var preferred = prop.Value.TryGetProperty("preferred", out var p) ? p.GetString() ?? "" : "";

                if (!string.IsNullOrEmpty(lemma1) && !string.IsNullOrEmpty(target) && !string.IsNullOrEmpty(preferred))
                {
                    _replace[id] = new ReplaceAdjustment(lemma1, target, preferred);
                }
            }
        }
    }

    /// <summary>
    /// Checks if a custom translation exists for the given ID.
    /// </summary>
    public bool HasAdjustment(int id) => _primary.ContainsKey(id) || _replace.ContainsKey(id);

    /// <summary>
    /// Apply custom translation adjustment if one exists for this lemma.
    /// Mirrors Python TranslationAdjustments.apply() logic.
    /// </summary>
    /// <param name="lemmaId">The DPD headword ID</param>
    /// <param name="lemma1">The DPD lemma_1 value (e.g., "paññā 1")</param>
    /// <param name="meaning">The original meaning string (semicolon-separated)</param>
    /// <returns>Modified meaning string with adjustments applied, or original if no adjustment applies.</returns>
    public string Apply(int lemmaId, string lemma1, string meaning)
    {
        var result = meaning;

        // Apply "primary" adjustment (move preferred to front)
        if (_primary.TryGetValue(lemmaId, out var primary) && lemma1 == primary.Lemma1)
        {
            result = ApplyPrimary(result, primary.Preferred);
        }

        // Apply "replace" adjustment (replace target with preferred)
        if (_replace.TryGetValue(lemmaId, out var replace) && lemma1 == replace.Lemma1)
        {
            result = ApplyReplace(result, replace.Target, replace.Preferred);
        }

        return result;
    }

    string ApplyPrimary(string meaning, string preferred)
    {
        if (string.IsNullOrEmpty(meaning))
            return preferred;

        // Parse meanings (split by "; ")
        var parts = meaning.Split(';')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        // Check if preferred already exists (case-insensitive, startswith match - mirrors Python)
        var preferredLower = preferred.ToLowerInvariant();
        var existingIndex = parts.FindIndex(p => p.ToLowerInvariant().StartsWith(preferredLower));

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

    string ApplyReplace(string meaning, string target, string preferred)
    {
        if (string.IsNullOrEmpty(meaning))
            return preferred;

        // Parse meanings (split by "; ")
        var parts = meaning.Split(';')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        // Find and replace the target
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = preferred;
                break;
            }
        }

        return string.Join("; ", parts);
    }

    /// <summary>
    /// Number of custom translation adjustments loaded.
    /// </summary>
    public int Count => _primary.Count + _replace.Count;
}
