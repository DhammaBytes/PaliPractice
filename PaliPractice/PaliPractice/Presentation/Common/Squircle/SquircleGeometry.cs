using Windows.Foundation;

namespace PaliPractice.Presentation.Common.Squircle;

/// <summary>
/// Flags enum for specifying which corners should be squircle-rounded.
/// </summary>
[Flags]
public enum SquircleCorners
{
    None = 0,
    TopLeft = 0b_0001,
    TopRight = 0b_0010,
    BottomLeft = 0b_0100,
    BottomRight = 0b_1000,
    Top = TopLeft | TopRight,
    Bottom = BottomLeft | BottomRight,
    Left = TopLeft | BottomLeft,
    Right = TopRight | BottomRight,
    All = TopLeft | TopRight | BottomLeft | BottomRight
}

/// <summary>
/// Generates squircle (superellipse) geometry for WinUI/Uno.
/// Based on iOS 7 rounded rectangle curves from paintcodeapp.com.
/// </summary>
public static class SquircleGeometry
{
    /// <summary>
    /// Maximum ratio of corner radius to the smaller dimension.
    /// Prevents corners from overlapping.
    /// </summary>
    public const float MaxSideRatio = 0.47f;

    #region Radius Constants

    // ═══════════════════════════════════════════════════════════════════════════
    // LOGARITHMIC RADIUS FORMULA
    // ═══════════════════════════════════════════════════════════════════════════
    //
    // Formula: radius = A × ln(m + 1) + B
    //
    // This gives larger elements proportionally SMALLER corners (as percentage),
    // which looks more balanced visually.
    //
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ Element size │ Radius │ Percentage                                      │
    // │──────────────┼────────┼─────────────────────────────────────────────────│
    // │ 50px         │ ~6px   │ 12%                                             │
    // │ 80px         │ ~8px   │ 10%                                             │
    // │ 150px        │ ~11px  │ 7%                                              │
    // │ 300px        │ ~15px  │ 5%                                              │
    // │ 450px        │ ~17px  │ 4%                                              │
    // └─────────────────────────────────────────────────────────────────────────┘
    //
    // To make everything rounder: increase LogA or LogB
    // To make everything sharper: decrease LogA or LogB
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logarithmic coefficient - controls curve steepness.</summary>
    const double LogA = 5.0;

    /// <summary>Logarithmic offset - shifts entire curve up/down.</summary>
    const double LogB = -14.0;

    /// <summary>Minimum radius - prevents tiny elements from being too sharp.</summary>
    const double MinRadius = 4.0;

    // ═══════════════════════════════════════════════════════════════════════════
    // SEMANTIC PRESET VALUES
    // ═══════════════════════════════════════════════════════════════════════════
    //
    // Fixed values for specific UI element categories.
    // Use these when you want predictable, consistent corners regardless of size.
    //
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ Preset       │ Radius │ Use for                                         │
    // │──────────────┼────────┼─────────────────────────────────────────────────│
    // │ Card         │ 16px   │ Large cards (practice, stats, about)            │
    // │ Surface      │ 10px   │ Medium surfaces (translation, nested panels)    │
    // │ ActionButton │ 8px    │ Action buttons (reveal, hard, easy)             │
    // └─────────────────────────────────────────────────────────────────────────┘
    // ═══════════════════════════════════════════════════════════════════════════

    const double CardRadius = 12.0;
    const double SurfaceRadius = 10.0;
    const double ActionButtonRadius = 8.0;

    #endregion

    /// <summary>
    /// Creates a PathGeometry representing a squircle shape.
    /// </summary>
    /// <param name="width">Width of the shape.</param>
    /// <param name="height">Height of the shape.</param>
    /// <param name="radius">Corner radius.</param>
    /// <param name="corners">Which corners to round (default: all).</param>
    /// <param name="ignoreLimit">If true, don't limit radius to MaxSideRatio.</param>
    /// <returns>A PathGeometry with the squircle shape.</returns>
    public static PathGeometry Create(
        double width,
        double height,
        double radius,
        SquircleCorners corners = SquircleCorners.All,
        bool ignoreLimit = false)
    {
        float limit = (float)(Math.Min(width, height) * MaxSideRatio);
        float limitedRadius = ignoreLimit ? (float)radius : Math.Min((float)radius, limit);

        var rect = new Rect(0, 0, width, height);

        Point TopLeft(float x, float y) => new(rect.Left + x * limitedRadius, rect.Top + y * limitedRadius);
        Point TopRight(float x, float y) => new(rect.Right - x * limitedRadius, rect.Top + y * limitedRadius);
        Point BottomRight(float x, float y) => new(rect.Right - x * limitedRadius, rect.Bottom - y * limitedRadius);
        Point BottomLeft(float x, float y) => new(rect.Left + x * limitedRadius, rect.Bottom - y * limitedRadius);

        var figure = new PathFigure { IsClosed = true };

        // Start point
        if (corners.HasFlag(SquircleCorners.TopLeft))
        {
            figure.StartPoint = TopLeft(1.52866483f, 0.00000000f);
        }
        else
        {
            figure.StartPoint = new Point(rect.Left, rect.Top);
        }

        // Top right corner
        if (corners.HasFlag(SquircleCorners.TopRight))
        {
            var p1 = TopRight(1.52866471f, 0.00000000f);
            figure.Segments.Add(new LineSegment { Point = p1 });

            var p2 = TopRight(1.08849323f, 0.00000000f);
            var p3 = TopRight(0.86840689f, 0.00000000f);
            var p4 = TopRight(0.66993427f, 0.06549600f);
            figure.Segments.Add(new BezierSegment { Point1 = p2, Point2 = p3, Point3 = p4 });

            var p5 = TopRight(0.63149399f, 0.07491100f);
            figure.Segments.Add(new LineSegment { Point = p5 });

            var p6 = TopRight(0.37282392f, 0.16905899f);
            var p7 = TopRight(0.16906013f, 0.37282401f);
            var p8 = TopRight(0.07491176f, 0.63149399f);
            figure.Segments.Add(new BezierSegment { Point1 = p6, Point2 = p7, Point3 = p8 });

            var p9 = TopRight(0.00000000f, 0.86840701f);
            var p10 = TopRight(0.00000000f, 1.08849299f);
            var p11 = TopRight(0.00000000f, 1.52866483f);
            figure.Segments.Add(new BezierSegment { Point1 = p9, Point2 = p10, Point3 = p11 });
        }
        else
        {
            figure.Segments.Add(new LineSegment { Point = new Point(rect.Right, rect.Top) });
        }

        // Bottom right corner
        if (corners.HasFlag(SquircleCorners.BottomRight))
        {
            var p12 = BottomRight(0.00000000f, 1.52866471f);
            figure.Segments.Add(new LineSegment { Point = p12 });

            var p13 = BottomRight(0.00000000f, 1.08849323f);
            var p14 = BottomRight(0.00000000f, 0.86840689f);
            var p15 = BottomRight(0.06549569f, 0.66993493f);
            figure.Segments.Add(new BezierSegment { Point1 = p13, Point2 = p14, Point3 = p15 });

            var p16 = BottomRight(0.07491111f, 0.63149399f);
            figure.Segments.Add(new LineSegment { Point = p16 });

            var p17 = BottomRight(0.16905883f, 0.37282392f);
            var p18 = BottomRight(0.37282392f, 0.16905883f);
            var p19 = BottomRight(0.63149399f, 0.07491111f);
            figure.Segments.Add(new BezierSegment { Point1 = p17, Point2 = p18, Point3 = p19 });

            var p20 = BottomRight(0.86840689f, 0.00000000f);
            var p21 = BottomRight(1.08849323f, 0.00000000f);
            var p22 = BottomRight(1.52866471f, 0.00000000f);
            figure.Segments.Add(new BezierSegment { Point1 = p20, Point2 = p21, Point3 = p22 });
        }
        else
        {
            figure.Segments.Add(new LineSegment { Point = new Point(rect.Right, rect.Bottom) });
        }

        // Bottom left corner
        if (corners.HasFlag(SquircleCorners.BottomLeft))
        {
            var p23 = BottomLeft(1.52866483f, 0.00000000f);
            figure.Segments.Add(new LineSegment { Point = p23 });

            var p24 = BottomLeft(1.08849299f, 0.00000000f);
            var p25 = BottomLeft(0.86840701f, 0.00000000f);
            var p26 = BottomLeft(0.66993397f, 0.06549569f);
            figure.Segments.Add(new BezierSegment { Point1 = p24, Point2 = p25, Point3 = p26 });

            var p27 = BottomLeft(0.63149399f, 0.07491111f);
            figure.Segments.Add(new LineSegment { Point = p27 });

            var p28 = BottomLeft(0.37282401f, 0.16905883f);
            var p29 = BottomLeft(0.16906001f, 0.37282392f);
            var p30 = BottomLeft(0.07491100f, 0.63149399f);
            figure.Segments.Add(new BezierSegment { Point1 = p28, Point2 = p29, Point3 = p30 });

            var p31 = BottomLeft(0.00000000f, 0.86840689f);
            var p32 = BottomLeft(0.00000000f, 1.08849323f);
            var p33 = BottomLeft(0.00000000f, 1.52866471f);
            figure.Segments.Add(new BezierSegment { Point1 = p31, Point2 = p32, Point3 = p33 });
        }
        else
        {
            figure.Segments.Add(new LineSegment { Point = new Point(rect.Left, rect.Bottom) });
        }

        // Top left corner
        if (corners.HasFlag(SquircleCorners.TopLeft))
        {
            var p34 = TopLeft(0.00000000f, 1.52866483f);
            figure.Segments.Add(new LineSegment { Point = p34 });

            var p35 = TopLeft(0.00000000f, 1.08849299f);
            var p36 = TopLeft(0.00000000f, 0.86840701f);
            var p37 = TopLeft(0.06549600f, 0.66993397f);
            figure.Segments.Add(new BezierSegment { Point1 = p35, Point2 = p36, Point3 = p37 });

            var p38 = TopLeft(0.07491100f, 0.63149399f);
            figure.Segments.Add(new LineSegment { Point = p38 });

            var p39 = TopLeft(0.16906001f, 0.37282401f);
            var p40 = TopLeft(0.37282401f, 0.16906001f);
            var p41 = TopLeft(0.63149399f, 0.07491100f);
            figure.Segments.Add(new BezierSegment { Point1 = p39, Point2 = p40, Point3 = p41 });

            var p42 = TopLeft(0.86840701f, 0.00000000f);
            var p43 = TopLeft(1.08849299f, 0.00000000f);
            var p44 = TopLeft(1.52866483f, 0.00000000f);
            figure.Segments.Add(new BezierSegment { Point1 = p42, Point2 = p43, Point3 = p44 });
        }
        else
        {
            figure.Segments.Add(new LineSegment { Point = new Point(rect.Left, rect.Top) });
        }

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    /// <summary>
    /// Calculates an appropriate radius based on dimensions and shape type.
    /// </summary>
    public static double CalculateRadius(double width, double height, SquircleRadiusMode mode)
    {
        var m = Math.Min(width, height);
        var maxAllowed = m * MaxSideRatio;

        var radius = mode switch
        {
            // Logarithmic curve - larger elements get proportionally smaller corners
            SquircleRadiusMode.Harmonized => CalculateLogarithmicRadius(m),

            // Semantic presets - fixed values for specific UI categories
            SquircleRadiusMode.Card => CardRadius,
            SquircleRadiusMode.Surface => SurfaceRadius,
            SquircleRadiusMode.ActionButton => ActionButtonRadius,

            // Pill shapes - percentage of smaller dimension
            SquircleRadiusMode.Pill => m * 0.44,
            SquircleRadiusMode.NearPill => m * 0.40,

            _ => CalculateLogarithmicRadius(m)
        };

        // Never exceed the geometric maximum
        return Math.Round(Math.Min(radius, maxAllowed));
    }

    /// <summary>
    /// Calculates radius using a logarithmic curve.
    /// Larger elements get proportionally smaller corners (as percentage of size).
    /// </summary>
    static double CalculateLogarithmicRadius(double m)
    {
        if (m <= 0) return 0;
        var r = LogA * Math.Log(m + 1) + LogB;
        return Math.Max(r, MinRadius);
    }
}

/// <summary>
/// Preset radius calculation modes for squircle elements.
/// </summary>
public enum SquircleRadiusMode
{
    // ─────────────────────────────────────────────────────────────────────────
    // DYNAMIC (size-based calculation)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Logarithmic curve (default). Larger elements get proportionally smaller
    /// corners as a percentage of their size. Good general-purpose choice.
    /// </summary>
    Harmonized,

    // ─────────────────────────────────────────────────────────────────────────
    // SEMANTIC PRESETS (fixed values)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fixed 16px radius. For large cards (practice, stats, about).
    /// </summary>
    Card,

    /// <summary>
    /// Fixed 10px radius. For medium surfaces (translation, nested panels).
    /// </summary>
    Surface,

    /// <summary>
    /// Fixed 8px radius. For action buttons (reveal, hard, easy).
    /// </summary>
    ActionButton,

    // ─────────────────────────────────────────────────────────────────────────
    // PILL SHAPES (percentage of smaller dimension)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Nearly fully rounded (44% of height). Capsule/pill shape with tiny flat sides.
    /// </summary>
    Pill,

    /// <summary>
    /// Very rounded (40% of height). Good for app bar buttons.
    /// </summary>
    NearPill
}
