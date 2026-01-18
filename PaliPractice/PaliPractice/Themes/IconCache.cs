using Microsoft.UI.Xaml.Media.Imaging;

namespace PaliPractice.Themes;

/// <summary>
/// Preloads and caches all app icons to eliminate loading jitter.
/// BitmapIcon loads images asynchronously, causing layout shifts when icons
/// appear after text. Preloading ensures images are in memory before use.
///
/// Call Preload() early in app startup (e.g., in App.OnLaunched or MainPage constructor).
/// </summary>
public static class IconCache
{
    static bool _isPreloaded;

    /// <summary>
    /// All icon paths used in the app.
    /// </summary>
    static readonly string[] AllIconPaths =
    [
        // Badge icons - Cases
        BadgeIcons.CaseNominative,
        BadgeIcons.CaseAccusative,
        BadgeIcons.CaseInstrumental,
        BadgeIcons.CaseDative,
        BadgeIcons.CaseAblative,
        BadgeIcons.CaseGenitive,
        BadgeIcons.CaseLocative,
        BadgeIcons.CaseVocative,

        // Badge icons - Genders
        BadgeIcons.GenderMasculine,
        BadgeIcons.GenderFeminine,
        BadgeIcons.GenderNeuter,

        // Badge icons - Numbers
        BadgeIcons.NumberSingular,
        BadgeIcons.NumberPlural,

        // Badge icons - Persons
        BadgeIcons.Person1st,
        BadgeIcons.Person2nd,
        BadgeIcons.Person3rd,

        // Badge icons - Tenses
        BadgeIcons.TensePresent,
        BadgeIcons.TenseImperative,
        BadgeIcons.TenseOptative,
        BadgeIcons.TenseFuture,

        // Badge icons - Voice
        BadgeIcons.VoiceReflexive,

        // Practice icons
        PracticeIcons.Reveal,
        PracticeIcons.Easy,
        PracticeIcons.Hard,
        PracticeIcons.ChevronLeft,
        PracticeIcons.ChevronRight,

        // Menu icons
        MenuIcons.Nouns,
        MenuIcons.Verbs,
        MenuIcons.Settings,
        MenuIcons.Stats,
        MenuIcons.Help,
    ];

    /// <summary>
    /// Preloads all icons by creating BitmapImage instances and triggering their load.
    /// Should be called once at app startup. Safe to call multiple times (no-op after first).
    /// </summary>
    public static void Preload()
    {
        if (_isPreloaded) return;
        _isPreloaded = true;

        foreach (var path in AllIconPaths)
        {
            // Create BitmapImage and set UriSource to trigger async load
            // The platform caches loaded images, so subsequent BitmapIcon uses are instant
            var bitmap = new BitmapImage();
            bitmap.UriSource = new Uri(path);
        }
    }
}
