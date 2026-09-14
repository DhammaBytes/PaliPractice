namespace PaliPractice.Presentation.Practice.Common;

public readonly record struct BadgeLabelOption(string Full, string Short);

public readonly record struct BadgeWidths(double Full, double Short);

/// <summary>Uses full labels, then only the first full, then compact labels.</summary>
public static class BadgeLabelSelector
{
    public static int SelectAbbreviatedMask(double availableWidth, IReadOnlyList<BadgeWidths> badges, double spacing)
    {
        if (badges.Count == 0) return 0;

        var fullWidth = spacing * (badges.Count - 1) + badges.Sum(badge => badge.Full);
        if (fullWidth <= availableWidth) return 0;

        var firstFullWidth = spacing * (badges.Count - 1) + badges[0].Full +
            badges.Skip(1).Sum(badge => badge.Short);
        if (firstFullWidth <= availableWidth)
            return ((1 << badges.Count) - 1) & ~1;

        // If even the compact labels cannot fit, BadgePanel wraps them.
        return (1 << badges.Count) - 1;
    }
}
