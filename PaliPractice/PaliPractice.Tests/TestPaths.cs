namespace PaliPractice.Tests;

/// <summary>
/// Shared path constants for test files.
/// Paths are relative to the test assembly output directory (bin/Debug/net10.0).
/// </summary>
public static class TestPaths
{
    // Inputs are immutable for a test process. Hash the large DPD database once.
    static readonly Lazy<IReadOnlyDictionary<string, string>> Inputs = new(LoadInputs);

    public static string InputPath(string name) => Inputs.Value[name];

    static IReadOnlyDictionary<string, string> LoadInputs()
    {
        var manifest = Environment.GetEnvironmentVariable("PALIPRACTICE_INPUT_MANIFEST");
        var candidate = Environment.GetEnvironmentVariable("PALIPRACTICE_CANDIDATE_DB");
        if (candidate is not null && manifest is null)
            throw new InvalidOperationException("Candidate comparisons require PALIPRACTICE_INPUT_MANIFEST.");
        var paths = manifest ?? System.IO.Path.Combine(RepositoryRoot, "quality", "config", "test-inputs.json");
        var provenance = candidate is not null ? manifest! : System.IO.Path.Combine(
            RepositoryRoot, "PaliPractice", "PaliPractice", "Data", "pali.manifest.json");
        return PinnedTestInputs.Load(paths, provenance);
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
    /// DPD release input verified against the bundle or candidate provenance.
    /// </summary>
    public static string DpdDbPath => InputPath("dpd");

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
                "Provision the pinned source described in scripts/SETUP.md.");
        }
    }
}
