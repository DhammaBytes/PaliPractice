namespace PaliPractice.Tests;

/// <summary>
/// Shared path constants for test files.
/// Paths are relative to the test assembly output directory (bin/Debug/net10.0).
/// </summary>
public static class TestPaths
{
    /// <summary>
    /// Path to dpd.db relative to test output directory.
    /// Structure: bin/Debug/net10.0 → ../../../../../dpd-db/dpd.db
    /// </summary>
    public static string DpdDbPath =>
        System.IO.Path.GetFullPath(System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "dpd-db", "dpd.db"));

    /// <summary>
    /// Path to pali.db (training database) relative to test output directory.
    /// </summary>
    public static string PaliDbPath =>
        System.IO.Path.GetFullPath(System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "PaliPractice", "PaliPractice", "Data", "pali.db"));

    /// <summary>
    /// Validates that required test databases exist. Call in OneTimeSetUp.
    /// </summary>
    public static void ValidateDpdDbExists()
    {
        if (!System.IO.File.Exists(DpdDbPath))
        {
            throw new System.IO.FileNotFoundException(
                $"DPD database not found at: {DpdDbPath}. " +
                $"Ensure dpd-db submodule is initialized: git submodule update --init");
        }
    }
}
