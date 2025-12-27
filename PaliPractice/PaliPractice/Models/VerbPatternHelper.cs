namespace PaliPractice.Models;

/// <summary>
/// Extension methods and helpers for VerbPattern enum.
///
/// Pattern Hierarchy:
/// - Regular patterns (Ati, Eti, Oti, Āti) appear as UI checkboxes
/// - Irregular patterns are children of regular patterns and inherit their checkbox state
/// - Use ParentRegular() to get an irregular's parent, ChildIrregulars() to get a regular's children
/// - When filtering by checkbox selection, map irregulars to their parent's checkbox
/// </summary>
public static class VerbPatternHelper
{
    /// <summary>
    /// Regular patterns that appear as UI checkboxes.
    /// </summary>
    public static readonly VerbPattern[] RegularPatterns =
        [VerbPattern.Ati, VerbPattern.Eti, VerbPattern.Oti, VerbPattern.Āti];

    /// <summary>
    /// Irregular patterns that map to regular checkbox groups.
    /// </summary>
    public static readonly VerbPattern[] IrregularPatterns =
        [VerbPattern.Hoti, VerbPattern.Atthi, VerbPattern.Natthi,
         VerbPattern.Dakkhati, VerbPattern.Dammi, VerbPattern.Hanati,
         VerbPattern.Eti2,
         VerbPattern.Karoti, VerbPattern.Brūti, VerbPattern.Kubbati];

    /// <summary>
    /// All patterns (regular + irregular).
    /// </summary>
    public static readonly VerbPattern[] AllPatterns =
        [.. RegularPatterns, .. IrregularPatterns];

    /// <summary>
    /// Converts enum to database string: VerbPattern.Ati → "ati pr"
    /// </summary>
    public static string ToDbString(this VerbPattern pattern) => pattern switch
    {
        // Regular
        VerbPattern.Ati => "ati pr",
        VerbPattern.Eti => "eti pr",
        VerbPattern.Oti => "oti pr",
        VerbPattern.Āti => "āti pr",
        // Irregulars → Ati
        VerbPattern.Hoti => "hoti pr",
        VerbPattern.Atthi => "atthi pr",
        VerbPattern.Natthi => "natthi pr",
        VerbPattern.Dakkhati => "dakkhati pr",
        VerbPattern.Dammi => "dammi pr",
        VerbPattern.Hanati => "hanati pr",
        // Irregulars → Eti
        VerbPattern.Eti2 => "eti pr 2",
        // Irregulars → Oti
        VerbPattern.Karoti => "karoti pr",
        VerbPattern.Brūti => "brūti pr",
        VerbPattern.Kubbati => "kubbati pr",
        _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown verb pattern")
    };

    /// <summary>
    /// Parses database string to enum: "ati pr" → VerbPattern.Ati
    /// </summary>
    public static VerbPattern Parse(string dbString) => dbString switch
    {
        // Regular
        "ati pr" => VerbPattern.Ati,
        "eti pr" => VerbPattern.Eti,
        "oti pr" => VerbPattern.Oti,
        "āti pr" => VerbPattern.Āti,
        // Irregulars → Ati
        "hoti pr" => VerbPattern.Hoti,
        "atthi pr" => VerbPattern.Atthi,
        "natthi pr" => VerbPattern.Natthi,
        "dakkhati pr" => VerbPattern.Dakkhati,
        "dammi pr" => VerbPattern.Dammi,
        "hanati pr" => VerbPattern.Hanati,
        // Irregulars → Eti
        "eti pr 2" => VerbPattern.Eti2,
        // Irregulars → Oti
        "karoti pr" => VerbPattern.Karoti,
        "brūti pr" => VerbPattern.Brūti,
        "kubbati pr" => VerbPattern.Kubbati,
        _ => throw new ArgumentException($"Unknown verb pattern: {dbString}", nameof(dbString))
    };

    /// <summary>
    /// Tries to parse database string to enum. Returns false if unknown.
    /// </summary>
    public static bool TryParse(string dbString, out VerbPattern pattern)
    {
        pattern = default;
        try
        {
            pattern = Parse(dbString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    extension(VerbPattern pattern)
    {
        /// <summary>
        /// Gets UI display label (stem only): VerbPattern.Ati → "ati"
        /// </summary>
        public string ToDisplayLabel()
            => pattern.ToDbString().Split(' ')[0];

        /// <summary>
        /// Returns true if the pattern is irregular.
        /// </summary>
        public bool IsIrregular()
            => pattern is VerbPattern.Hoti or VerbPattern.Atthi or VerbPattern.Natthi or
                          VerbPattern.Dakkhati or VerbPattern.Dammi or VerbPattern.Hanati or
                          VerbPattern.Eti2 or
                          VerbPattern.Karoti or VerbPattern.Brūti or VerbPattern.Kubbati;

        /// <summary>
        /// Gets the parent regular pattern for an irregular.
        /// Throws if called on a regular pattern.
        /// </summary>
        public VerbPattern ParentRegular() => pattern switch
        {
            // Irregulars → Ati
            VerbPattern.Hoti or VerbPattern.Atthi or VerbPattern.Natthi or
            VerbPattern.Dakkhati or VerbPattern.Dammi or VerbPattern.Hanati => VerbPattern.Ati,
            // Irregulars → Eti
            VerbPattern.Eti2 => VerbPattern.Eti,
            // Irregulars → Oti
            VerbPattern.Karoti or VerbPattern.Brūti or VerbPattern.Kubbati => VerbPattern.Oti,
            // Regular patterns have no parent
            _ => throw new InvalidOperationException($"{pattern} is a regular pattern, not an irregular")
        };

        /// <summary>
        /// Gets all child irregular patterns for a regular pattern.
        /// Throws if called on an irregular pattern.
        /// </summary>
        public HashSet<VerbPattern> ChildIrregulars() => pattern switch
        {
            VerbPattern.Ati => [VerbPattern.Hoti, VerbPattern.Atthi, VerbPattern.Natthi,
                                VerbPattern.Dakkhati, VerbPattern.Dammi, VerbPattern.Hanati],
            VerbPattern.Eti => [VerbPattern.Eti2],
            VerbPattern.Oti => [VerbPattern.Karoti, VerbPattern.Brūti, VerbPattern.Kubbati],
            VerbPattern.Āti => [],
            _ => throw new InvalidOperationException($"{pattern} is an irregular pattern, not a regular")
        };
    }
}
