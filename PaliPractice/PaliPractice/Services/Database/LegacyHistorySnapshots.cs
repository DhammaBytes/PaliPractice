using System.IO.Compression;
using System.Text.Json;
using PaliPractice.Services.UserData.Entities;
using SQLite;

namespace PaliPractice.Services.Database;

internal static class LegacyHistorySnapshots
{
    static readonly Lazy<Dictionary<long, string[]>> Released = new(Load);

    static Dictionary<long, string[]> Load()
    {
        using var resource = typeof(LegacyHistorySnapshots).Assembly
            .GetManifestResourceStream("PaliPractice.HistoryV11")
            ?? throw new InvalidDataException("Released history reconstruction resource is missing");
        using var decompressed = new GZipStream(resource, CompressionMode.Decompress);
        return JsonSerializer.Deserialize<Dictionary<long, string[]>>(decompressed)
            ?? throw new InvalidDataException("Released history reconstruction resource is empty");
    }

    public static void Backfill<T>(SQLiteConnection connection) where T : PracticeHistoryBase, new()
    {
        foreach (var history in connection.Table<T>().ToList())
        {
            if (!string.IsNullOrEmpty(history.FormText) || !Released.Value.TryGetValue(history.FormId, out var snapshot))
                continue;
            history.FormText = snapshot[0];
            history.LemmaText = snapshot[1];
            history.GrammarText = snapshot[2];
            history.SnapshotOrigin = HistorySnapshotOrigin.ReconstructedV11;
            connection.Update(history);
        }
    }
}
