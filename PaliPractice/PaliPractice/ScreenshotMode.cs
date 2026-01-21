namespace PaliPractice;

/// <summary>
/// Developer toggle for displaying predetermined content for store screenshots.
/// When enabled, practice screens show specific words and history shows mock data.
/// Only available in DEBUG builds - completely excluded from release.
/// </summary>
public static class ScreenshotMode
{
#if DEBUG
    /// <summary>
    /// Toggle for screenshots. Set to true when capturing store screenshots.
    /// </summary>
    public const bool IsEnabled = false;

    // Lemma IDs for screenshot content
    public const int DhammaLemmaId = 10005;   // "dhamma" noun
    public const int BhavatiLemmaId = 70008;  // "bhavati" verb
#else
    // Release builds: always disabled, constants defined to satisfy compiler
    public const bool IsEnabled = false;
    public const int DhammaLemmaId = 0;
    public const int BhavatiLemmaId = 0;
#endif
}
