using System.Security.Cryptography;
using System.Text.Json;
using Path = System.IO.Path;

namespace PaliPractice.Tests;

/// <summary>
/// Resolves comparison inputs and checks their bytes before any database queries.
/// Bundled comparisons use the bundle's provenance; candidates use their input manifest.
/// </summary>
public static class PinnedTestInputs
{
    public static IReadOnlyDictionary<string, string> Load(string pathsManifest, string provenanceManifest)
    {
        using var locations = JsonDocument.Parse(File.ReadAllText(pathsManifest));
        using var provenance = JsonDocument.Parse(File.ReadAllText(provenanceManifest));
        var pins = provenance.RootElement;
        if (pins.TryGetProperty("english", out var english)) pins = english;
        var paths = new Dictionary<string, string>();
        foreach (var name in new[] { "dpd", "cst", "bjt", "sya", "sc", "adjustments" })
        {
            var location = locations.RootElement.GetProperty("inputs").GetProperty(name);
            var expected = pins.GetProperty("inputs").GetProperty(name).GetProperty("sha256").GetString()!;
            if (location.TryGetProperty("sha256", out var declared) && declared.GetString() != expected)
                throw new InvalidDataException($"Pinned input '{name}' disagrees with {provenanceManifest}.");
            var path = Path.GetFullPath(location.GetProperty("path").GetString()!,
                Path.GetDirectoryName(Path.GetFullPath(pathsManifest))!);
            Verify(name, path, expected);
            paths.Add(name, path);
        }
        return paths;
    }

    static void Verify(string name, string path, string expected)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Pinned input '{name}' is missing: {path}. Provision the source recorded in the database manifest; see scripts/SETUP.md.", path);
        if (name == "dpd" && File.Exists(path + "-wal") && new FileInfo(path + "-wal").Length > 0)
            throw new InvalidDataException($"Pinned DPD has a nonempty WAL: {path}. Supply a checkpointed database.");
        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexStringLower(SHA256.HashData(stream));
        if (actual != expected)
            throw new InvalidDataException(
                $"Pinned input '{name}' checksum mismatch: {path}. Expected {expected}, found {actual}. Provision the matching source; see scripts/SETUP.md.");
    }
}
