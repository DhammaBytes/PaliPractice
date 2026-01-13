namespace PaliPractice.Themes;

public static class OptionPresentation
{
    // Cached brushes to avoid allocations
    static readonly Dictionary<Color, SolidColorBrush> _brushCache = new();

    // Pre-defined color constants
    static readonly Color NumberColor = Color.FromArgb(255, 230, 230, 255);
    static readonly Color PersonColor = Color.FromArgb(255, 230, 255, 230);
    static readonly Color ActiveVoiceColor = Color.FromArgb(255, 230, 245, 255);
    static readonly Color ReflexiveColor = Color.FromArgb(255, 255, 230, 230);
    static readonly Color TenseColor = Color.FromArgb(255, 240, 240, 255);
    static readonly Color GenderColor = Color.FromArgb(255, 220, 255, 220);
    static readonly Color CaseColor = Color.FromArgb(255, 255, 243, 224);
    static readonly Color DefaultColor = Color.FromArgb(255, 255, 0, 255);

    public static Color GetChipColor<T>(T value) where T : struct, Enum => value switch
    {
        Number.Singular => NumberColor,
        Number.Plural => NumberColor,

        Person.First => PersonColor,
        Person.Second => PersonColor,
        Person.Third => PersonColor,

        Tense.Present => TenseColor,
        Tense.Imperative => TenseColor,
        Tense.Optative => TenseColor,
        Tense.Future => TenseColor,
        // Tense.Aorist => TenseColor,

        Gender.Masculine => GenderColor,
        Gender.Neuter => GenderColor,
        Gender.Feminine => GenderColor,

        Case.Nominative => CaseColor,
        Case.Accusative => CaseColor,
        Case.Instrumental => CaseColor,
        Case.Dative => CaseColor,
        Case.Ablative => CaseColor,
        Case.Genitive => CaseColor,
        Case.Locative => CaseColor,
        Case.Vocative => CaseColor,

        Voice.Active => ActiveVoiceColor,
        Voice.Reflexive => ReflexiveColor,

        _ => DefaultColor
    };

    /// <summary>
    /// Gets the chip color for reflexive indicator.
    /// </summary>
    public static Color GetReflexiveChipColor() => ReflexiveColor;

    /// <summary>
    /// Gets a cached SolidColorBrush for reflexive indicator.
    /// </summary>
    public static SolidColorBrush GetReflexiveChipBrush()
    {
        if (!_brushCache.TryGetValue(ReflexiveColor, out var brush))
        {
            brush = new SolidColorBrush(ReflexiveColor);
            _brushCache[ReflexiveColor] = brush;
        }
        return brush;
    }

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
