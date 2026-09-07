using PaliPractice.Models.Words;
using PaliPractice.Services.UserData;
using SQLite;

namespace PaliPractice.Services.Database.Repositories;

/// <summary>Queries only requested meanings, then English for missing senses.</summary>
public sealed class MeaningLoader(SQLiteConnection connection)
{
    readonly bool _hasLocalizedTable = connection.ExecuteScalar<int>(
        "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='localized_meanings'") == 1;

    public void Ensure(ILemma lemma, string detailsTable, string requestedLanguage)
    {
        var preference = TranslationLanguageResolver.PreferenceFromLanguageCode(requestedLanguage);
        var language = TranslationLanguageResolver.ResolveEffectiveLanguageCode(preference);
        if (lemma.MeaningsLanguage == preference) return;
        var details = lemma.Words.Select(w => w.Details).OfType<IWordDetails>().ToList();
        var ids = details.Select(d => d.Id).ToArray();
        var meanings = Read(ids, detailsTable, language);
        var missing = ids.Where(id => !meanings.ContainsKey(id)).ToArray();
        Dictionary<int, string> fallback = language == "en" ? [] : Read(missing, detailsTable, "en");
        foreach (var detail in details)
        {
            var translated = meanings.TryGetValue(detail.Id, out var meaning);
            detail.Meaning = translated ? meaning! : fallback.GetValueOrDefault(detail.Id, string.Empty);
            detail.MeaningLanguage = translated ? preference : TranslationLanguagePreference.English;
        }
        lemma.MeaningsLanguage = preference;
    }

    Dictionary<int, string> Read(int[] ids, string detailsTable, string language)
    {
        if (ids.Length == 0) return [];
        if (detailsTable is not ("nouns_details" or "verbs_details"))
            throw new ArgumentException("Unsupported details table", nameof(detailsTable));
        var placeholders = string.Join(",", ids.Select(_ => "?"));
        object[] parameters = ids.Cast<object>().ToArray();
        string sql;
        if (language != "en" && _hasLocalizedTable)
        {
            sql = $"SELECT headword_id AS id, meaning FROM localized_meanings WHERE headword_id IN ({placeholders}) AND language=?";
            parameters = [.. parameters, language];
        }
        else
        {
            // The current shipped bundle has Russian in its legacy details column.
            if (language == "es") return [];
            var column = language == "ru" ? "meaning_ru" : "meaning";
            sql = $"SELECT id, {column} AS meaning FROM {detailsTable} WHERE id IN ({placeholders})";
        }
        return connection.Query<MeaningRow>(sql, parameters)
            .Where(row => !string.IsNullOrWhiteSpace(row.Meaning))
            .ToDictionary(row => row.Id, row => row.Meaning);
    }

    public sealed class MeaningRow
    {
        [Column("id")] public int Id { get; set; }
        [Column("meaning")] public string Meaning { get; set; } = string.Empty;
    }
}
