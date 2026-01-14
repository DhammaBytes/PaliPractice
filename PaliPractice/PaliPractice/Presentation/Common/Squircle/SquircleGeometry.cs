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
    // FIXED RADIUS VALUES (based on Figma specs at 3x → points)
    // ═══════════════════════════════════════════════════════════════════════════
    //
    // Figma uses 40px and 30px at 3x scale, which translates to:
    //   40px @ 3x = ~13px in points
    //   30px @ 3x = ~10px in points
    //
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ Mode             │ Radius │ Use for                                     │
    // │──────────────────┼────────┼─────────────────────────────────────────────│
    // │ CardLarge        │ 13px   │ Practice card, stats cards, about cards     │
    // │ CardSmall        │ 13px   │ Translation background                      │
    // │ ButtonLarge      │ 13px   │ Start page buttons                          │
    // │ ButtonMedium     │ 10px   │ Reveal, Easy, Hard buttons                  │
    // │ ButtonNavigation │ 40%    │ App bar buttons, contact button             │
    // │ Pill             │ 44%    │ Badges, progress bars                       │
    // └─────────────────────────────────────────────────────────────────────────┘
    // ═══════════════════════════════════════════════════════════════════════════

    const double RadiusLarge = 13.0;   // 40px @ 3x
    const double RadiusMedium = 10.0;  // 30px @ 3x

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
            // Fixed values (13px - from Figma 40px @ 3x)
            SquircleRadiusMode.CardLarge => RadiusLarge,
            SquircleRadiusMode.CardSmall => RadiusLarge,
            SquircleRadiusMode.ButtonLarge => RadiusLarge,

            // Fixed values (10px - from Figma 30px @ 3x)
            SquircleRadiusMode.ButtonMedium => RadiusMedium,

            // Percentage of smaller dimension
            SquircleRadiusMode.ButtonNavigation => m * 0.40,
            SquircleRadiusMode.Pill => m * 0.44,

            _ => RadiusLarge // Default to large card radius
        };

        // Never exceed the geometric maximum
        return Math.Round(Math.Min(radius, maxAllowed));
    }
}

/// <summary>
/// Preset radius calculation modes for squircle elements.
/// All values derived from Figma specs (3x scale → points).
/// </summary>
public enum SquircleRadiusMode
{
    // ─────────────────────────────────────────────────────────────────────────
    // CARDS (fixed 13px - Figma 40px @ 3x)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Practice card, stats cards, about cards.</summary>
    CardLarge,

    /// <summary>Translation background (narrower/shorter card).</summary>
    CardSmall,

    // ─────────────────────────────────────────────────────────────────────────
    // BUTTONS (fixed values)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Start page buttons (13px - Figma 40px @ 3x).</summary>
    ButtonLarge,

    /// <summary>Reveal, Easy, Hard buttons (10px - Figma 30px @ 3x).</summary>
    ButtonMedium,

    /// <summary>App bar buttons, contact button (40% of height).</summary>
    ButtonNavigation,

    // ─────────────────────────────────────────────────────────────────────────
    // PILL (percentage-based)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Badges, progress bars (44% of height).</summary>
    Pill
}
