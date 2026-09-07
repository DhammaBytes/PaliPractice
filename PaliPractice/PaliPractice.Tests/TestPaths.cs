namespace PaliPractice.Tests;

/// <summary>
/// Shared path constants for test files.
/// Paths are relative to the test assembly output directory (bin/Debug/net10.0).
/// </summary>
public static class TestPaths
{
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
    public static string DpdDbPath =>
        System.IO.Path.Combine(RepositoryRoot, "dpd-db", "dpd.db");

    /// <summary>
    /// Path to pali.db (training database) relative to test output directory.
    /// </summary>
    public static string PaliDbPath =>
        System.IO.Path.Combine(
            RepositoryRoot, "PaliPractice", "PaliPractice", "Data", "pali.db");

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
