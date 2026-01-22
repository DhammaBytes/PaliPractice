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

public static class BadgePresentation
{
    // === Badge Color Assignments ===
    // Change these to quickly reassign colors to badge types
    const BadgeColor CaseBadge = BadgeColor.Green;
    const BadgeColor GenderBadge = BadgeColor.Yellow;

    const BadgeColor TenseBadge = BadgeColor.Blue;
    const BadgeColor PersonBadge = BadgeColor.Purple;

    const BadgeColor NumberBadge = BadgeColor.Pink; // Shared between declension/conjugation

    const BadgeColor VoiceBadge = BadgeColor.Gray; // Rare reflexive voice (Active has no badge)

    /// <summary>
    /// Gets the theme brush for a badge color (e.g., "BadgePinkBrush").
    /// Automatically respects light/dark theme.
    /// </summary>
    static SolidColorBrush GetBrush(BadgeColor badge) =>
        Application.Current.Resources[$"Badge{badge}Brush"] as SolidColorBrush
        ?? new SolidColorBrush(Colors.Magenta); // Fallback for debugging

    static Color GetColor(BadgeColor badge) => GetBrush(badge).Color;

    public static Color GetChipColor<T>(T value) where T : struct, Enum => GetColor(value switch
    {
        Number => NumberBadge,
        Person => PersonBadge,
        Tense => TenseBadge,
        Voice => VoiceBadge,
        Gender => GenderBadge,
        Case => CaseBadge,
        _ => VoiceBadge
    });
}
