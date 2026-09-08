using System.IO.Compression;
using System.Text.Json;
using PaliPractice.Services.UserData.Entities;
using SQLite;

namespace PaliPractice.Services.Database;

internal static class LegacyHistorySnapshots
{
    static Dictionary<long, string[]> Load()
    {
        using var resource = typeof(LegacyHistorySnapshots).Assembly
            .GetManifestResourceStream("PaliPractice.HistoryV11")
            ?? throw new InvalidDataException("Released history reconstruction resource is missing");
        using var decompressed = new GZipStream(resource, CompressionMode.Decompress);
        return JsonSerializer.Deserialize<Dictionary<long, string[]>>(decompressed)
            ?? throw new InvalidDataException("Released history reconstruction resource is empty");
    }

    public static void Backfill(SQLiteConnection connection, Func<Dictionary<long, string[]>>? load = null)
    {
        // Shared only by this migration. Empty/already-snapshotted histories never load it.
        var released = new Lazy<Dictionary<long, string[]>>(load ?? Load);
        Backfill<NounsPracticeHistory>(connection, released);
        Backfill<VerbsPracticeHistory>(connection, released);
    }

    static void Backfill<T>(SQLiteConnection connection, Lazy<Dictionary<long, string[]>> released)
        where T : PracticeHistoryBase, new()
    {
        var table = connection.GetMapping(typeof(T)).TableName;
        long lastId = long.MinValue;
        while (true)
        {
            var batch = connection.Query<T>(
                $"SELECT * FROM {table} WHERE (form_text IS NULL OR form_text = '') AND id > ? ORDER BY id LIMIT 256",
                lastId);
            if (batch.Count == 0) return;
            foreach (var history in batch)
            {
                // Advance past unresolved rows too; they must not stall the next batch.
                lastId = history.Id;
                if (!released.Value.TryGetValue(history.FormId, out var snapshot)) continue;
                history.FormText = snapshot[0];
                history.LemmaText = snapshot[1];
                history.GrammarText = snapshot[2];
                history.SnapshotOrigin = HistorySnapshotOrigin.ReconstructedV11;
                connection.Update(history);
            }
        }
    }
}
