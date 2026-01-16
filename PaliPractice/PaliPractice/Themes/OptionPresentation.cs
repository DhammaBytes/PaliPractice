namespace PaliPractice.Themes;

/// <summary>
/// Badge color names matching ThemeResources.xaml Badge Colors section.
/// Change assignments here to quickly swap badge colors.
/// </summary>
public enum BadgeColor
{
    Pink,
    Green,
    Blue,
    Yellow,
    Purple,
    Gray
}

public static class OptionPresentation
{
    // === Badge Color Assignments ===
    // Change these to quickly reassign colors to badge types
    const BadgeColor CaseBadge = BadgeColor.Green;
    const BadgeColor GenderBadge = BadgeColor.Yellow;

    const BadgeColor TenseBadge = BadgeColor.Blue;
    const BadgeColor PersonBadge = BadgeColor.Purple;

    const BadgeColor NumberBadge = BadgeColor.Pink; // Shared between declension/conjugation

    const BadgeColor VoiceReflexiveBadge = BadgeColor.Gray; // Rare reflexive voice (Active has no badge)

    /// <summary>
    /// Gets the theme brush for a badge color (e.g., "BadgePinkBrush").
    /// Automatically respects light/dark theme.
    /// </summary>
    static SolidColorBrush GetBrush(BadgeColor badge) =>
        Application.Current.Resources[$"Badge{badge}Brush"] as SolidColorBrush
        ?? new SolidColorBrush(Colors.Magenta); // Fallback for debugging

    static Color GetColor(BadgeColor badge) => GetBrush(badge).Color;

    public static Color GetChipColor<T>(T value) where T : struct, Enum => value switch
    {
        // Number - shared color between declension and conjugation
        Number.Singular => GetColor(NumberBadge),
        Number.Plural => GetColor(NumberBadge),

        // Conjugation badges
        Person.First => GetColor(PersonBadge),
        Person.Second => GetColor(PersonBadge),
        Person.Third => GetColor(PersonBadge),

        Tense.Present => GetColor(TenseBadge),
        Tense.Imperative => GetColor(TenseBadge),
        Tense.Optative => GetColor(TenseBadge),
        Tense.Future => GetColor(TenseBadge),

        Voice.Reflexive => GetColor(VoiceReflexiveBadge), // Active voice has no badge

        // Declension badges
        Gender.Masculine => GetColor(GenderBadge),
        Gender.Neuter => GetColor(GenderBadge),
        Gender.Feminine => GetColor(GenderBadge),

        Case.Nominative => GetColor(CaseBadge),
        Case.Accusative => GetColor(CaseBadge),
        Case.Instrumental => GetColor(CaseBadge),
        Case.Dative => GetColor(CaseBadge),
        Case.Ablative => GetColor(CaseBadge),
        Case.Genitive => GetColor(CaseBadge),
        Case.Locative => GetColor(CaseBadge),
        Case.Vocative => GetColor(CaseBadge),

        _ => GetColor(BadgeColor.Gray) // Fallback
    };

    /// <summary>
    /// Gets the chip color for reflexive indicator.
    /// </summary>
    public static Color GetReflexiveChipColor() => GetColor(VoiceReflexiveBadge);

    /// <summary>
    /// Gets the theme brush for reflexive indicator.
    /// </summary>
    public static SolidColorBrush GetReflexiveChipBrush() => GetBrush(VoiceReflexiveBadge);

    /// <summary>
    /// Gets the badge color assignment for an enum value.
    /// </summary>
    static BadgeColor GetBadgeColor<T>(T value) where T : struct, Enum => value switch
    {
        Number => NumberBadge,
        Person => PersonBadge,
        Tense => TenseBadge,
        Voice.Reflexive => VoiceReflexiveBadge,
        Gender => GenderBadge,
        Case => CaseBadge,
        _ => BadgeColor.Gray
    };

    /// <summary>
    /// Gets the theme brush for the given enum value.
    /// </summary>
    public static SolidColorBrush GetChipBrush<T>(T value) where T : struct, Enum =>
        GetBrush(GetBadgeColor(value));
}
