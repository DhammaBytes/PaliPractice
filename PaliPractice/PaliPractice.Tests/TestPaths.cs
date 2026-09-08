namespace PaliPractice.Tests;

/// <summary>
/// Shared path constants for test files.
/// Paths are relative to the test assembly output directory (bin/Debug/net10.0).
/// </summary>
public static class TestPaths
{
    public static string? InputPath(string name)
    {
        var path = Environment.GetEnvironmentVariable("PALIPRACTICE_INPUT_MANIFEST");
        if (path is null) return null;
        using var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var input = manifest.RootElement.GetProperty("inputs").GetProperty(name).GetProperty("path").GetString()!;
        return System.IO.Path.GetFullPath(input, System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
    }

    /// <summary>
    /// Repository root supplied by isolated quality runs, or inferred from the
    /// conventional local test output layout.
    /// </summary>
    public static string RepositoryRoot =>
        Environment.GetEnvironmentVariable("PALIPRACTICE_REPO_ROOT")
        ?? System.IO.Path.GetFullPath(System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", ".."));

    /// <summary>
    /// Path to dpd.db relative to test output directory.
    /// Structure: bin/Debug/net10.0 → ../../../../../dpd-db/dpd.db
    /// </summary>
    public static string DpdDbPath => InputPath("dpd") ??
        System.IO.Path.Combine(RepositoryRoot, "dpd-db", "dpd.db");

    /// <summary>
    /// Path to pali.db (training database) relative to test output directory.
    /// </summary>
    public static string PaliDbPath =>
        Environment.GetEnvironmentVariable("PALIPRACTICE_CANDIDATE_DB") ??
        System.IO.Path.Combine(
            RepositoryRoot, "PaliPractice", "PaliPractice", "Data", "pali.db");

    public static string CorpusFormsPath =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PALIPRACTICE_CANDIDATE_DB"))
            ? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(PaliDbPath)!, "corpus_forms.json")
            : System.IO.Path.Combine(RepositoryRoot, "scripts", "generated", "corpus_forms.json");

    public static string PrimaryFormsPath =>
        Environment.GetEnvironmentVariable("PALIPRACTICE_CANDIDATE_DB") is not null
            ? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(PaliDbPath)!, "primary_forms.json")
            : System.IO.Path.Combine(RepositoryRoot, "scripts", "generated", "primary_forms.json");

    /// <summary>
    /// Validates that required test databases exist. Call in OneTimeSetUp.
    /// </summary>
    public static void ValidateDpdDbExists()
    {
        if (!File.Exists(DpdDbPath))
        {
            throw new FileNotFoundException(
                $"DPD database not found at: {DpdDbPath}. " +
                $"Ensure dpd-db submodule is initialized: git submodule update --init");
        }
    }
}
