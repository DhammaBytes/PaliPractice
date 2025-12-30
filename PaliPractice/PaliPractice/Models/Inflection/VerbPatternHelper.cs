namespace PaliPractice.Models.Inflection;

/// <summary>
/// Extension methods and helpers for VerbPattern enum.
///
/// The enum uses a breakpoint marker for efficient regularity checks:
/// - pattern &lt; _Irregular → Regular pattern
/// - pattern &gt; _Irregular → Irregular (use ParentRegular() to get parent)
/// </summary>
public static class VerbPatternHelper
{
    /// <summary>
    /// Parses database string to enum: "ati pr" → VerbPattern.Ati
    /// </summary>
    public static VerbPattern Parse(string dbString) => dbString switch
    {
        // Regular (traditional order: a, ā, e, o)
        "ati pr" => VerbPattern.Ati,
        "āti pr" => VerbPattern.Āti,
        "eti pr" => VerbPattern.Eti,
        "oti pr" => VerbPattern.Oti,

        // Irregulars → Ati
        "atthi pr" => VerbPattern.Atthi,
        "dakkhati pr" => VerbPattern.Dakkhati,
        "dammi pr" => VerbPattern.Dammi,
        "hanati pr" => VerbPattern.Hanati,
        "hoti pr" => VerbPattern.Hoti,
        "kubbati pr" => VerbPattern.Kubbati,
        "natthi pr" => VerbPattern.Natthi,

        // Irregulars → Eti
        "eti pr 2" => VerbPattern.Eti2,

        // Irregulars → Oti
        "brūti pr" => VerbPattern.Brūti,
        "karoti pr" => VerbPattern.Karoti,

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
        /// Returns true if the pattern is None or a breakpoint marker (_Irregular).
        /// These values should never appear in runtime data.
        /// </summary>
        public bool IsMarkerOrNone()
            => pattern == VerbPattern.None || pattern == VerbPattern._Irregular;

        /// <summary>
        /// Converts enum to database string: VerbPattern.Ati → "ati pr"
        /// Throws for None or marker patterns.
        /// </summary>
        public string ToDbString()
        {
            if (pattern.IsMarkerOrNone())
                throw new InvalidOperationException($"Cannot convert marker/None to db string: {pattern}");

            return pattern switch
        {
            // Regular (traditional order: a, ā, e, o)
            VerbPattern.Ati => "ati pr",
            VerbPattern.Āti => "āti pr",
            VerbPattern.Eti => "eti pr",
            VerbPattern.Oti => "oti pr",

            // Irregulars → Ati
            VerbPattern.Atthi => "atthi pr",
            VerbPattern.Dakkhati => "dakkhati pr",
            VerbPattern.Dammi => "dammi pr",
            VerbPattern.Hanati => "hanati pr",
            VerbPattern.Hoti => "hoti pr",
            VerbPattern.Kubbati => "kubbati pr",
            VerbPattern.Natthi => "natthi pr",

            // Irregulars → Eti
            VerbPattern.Eti2 => "eti pr 2",

            // Irregulars → Oti
            VerbPattern.Brūti => "brūti pr",
            VerbPattern.Karoti => "karoti pr",

            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown pattern")
            };
        }

        /// <summary>
        /// Gets UI display label (stem only): VerbPattern.Ati → "ati"
        /// </summary>
        public string ToDisplayLabel()
            => pattern.ToDbString().Split(' ')[0];

        /// <summary>
        /// Returns true if the pattern is irregular (pattern > _Irregular).
        /// </summary>
        public bool IsIrregular()
            => pattern > VerbPattern._Irregular;

        /// <summary>
        /// Gets the parent regular pattern for an irregular.
        /// Throws if called on a regular or breakpoint pattern.
        /// </summary>
        public VerbPattern ParentRegular() => pattern switch
        {
            // Irregulars → Ati
            VerbPattern.Atthi or VerbPattern.Dakkhati or VerbPattern.Dammi or
            VerbPattern.Hanati or VerbPattern.Hoti or VerbPattern.Kubbati or
            VerbPattern.Natthi => VerbPattern.Ati,

            // Irregulars → Eti
            VerbPattern.Eti2 => VerbPattern.Eti,

            // Irregulars → Oti
            VerbPattern.Brūti or VerbPattern.Karoti => VerbPattern.Oti,

            // Regular or breakpoint patterns have no parent
            _ => throw new InvalidOperationException($"{pattern} is not an irregular pattern")
        };
    }
}
