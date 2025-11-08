namespace PaliPractice.Themes;

public static class OptionPresentation
{
    // Cached brushes to avoid allocations
    static readonly Dictionary<Color, SolidColorBrush> _brushCache = new();

    // Pre-defined color constants
    static readonly Color NumberColor = Color.FromArgb(255, 230, 230, 255);
    static readonly Color PersonColor = Color.FromArgb(255, 230, 255, 230);
    static readonly Color VoiceColor = Color.FromArgb(255, 255, 230, 230);
    static readonly Color TenseColor = Color.FromArgb(255, 240, 240, 255);
    static readonly Color GenderColor = Color.FromArgb(255, 220, 255, 220);
    static readonly Color CaseColor = Color.FromArgb(255, 255, 243, 224);
    static readonly Color DefaultColor = Color.FromArgb(255, 255, 0, 255);

    public static string? GetGlyph<T>(T value) where T : struct, Enum => value switch
    {
        Number.Singular => "\uE77B",
        Number.Plural => "\uE716",

        Person.First => "\uE77B",
        Person.Second => "\uE748",
        Person.Third => "\uE716",

        Voice.Active => "\uE768",
        Voice.Reflexive => "\uE74C",
        Voice.Passive => "\uE72C",
        Voice.Causative => "\uE74E",

        Gender.Masculine => "\uE71A",
        Gender.Neuter => "\uE734",
        Gender.Feminine => "\uE716",

        _ => null
    };

    public static Color GetChipColor<T>(T value) where T : struct, Enum => value switch
    {
        Number.Singular => NumberColor,
        Number.Plural => NumberColor,

        Person.First => PersonColor,
        Person.Second => PersonColor,
        Person.Third => PersonColor,

        Voice.Active => VoiceColor,
        Voice.Reflexive => VoiceColor,
        Voice.Passive => VoiceColor,
        Voice.Causative => VoiceColor,

        Tense.Present => TenseColor,
        Tense.Imperative => TenseColor,
        Tense.Optative => TenseColor,
        Tense.Future => TenseColor,
        Tense.Aorist => TenseColor,

        Gender.Masculine => GenderColor,
        Gender.Neuter => GenderColor,
        Gender.Feminine => GenderColor,

        NounCase.Nominative => CaseColor,
        NounCase.Accusative => CaseColor,
        NounCase.Instrumental => CaseColor,
        NounCase.Dative => CaseColor,
        NounCase.Ablative => CaseColor,
        NounCase.Genitive => CaseColor,
        NounCase.Locative => CaseColor,
        NounCase.Vocative => CaseColor,

        _ => DefaultColor
    };

    /// <summary>
    /// Gets a cached SolidColorBrush for the given enum value.
    /// Avoids creating new brush instances on every call.
    /// </summary>
    public static SolidColorBrush GetChipBrush<T>(T value) where T : struct, Enum
    {
        var color = GetChipColor(value);
        if (!_brushCache.TryGetValue(color, out var brush))
        {
            brush = new SolidColorBrush(color);
            _brushCache[color] = brush;
        }
        return brush;
    }
}
