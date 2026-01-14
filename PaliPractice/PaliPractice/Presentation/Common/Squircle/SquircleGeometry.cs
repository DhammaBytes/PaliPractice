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

    #region Harmonized Radius System
    // ═══════════════════════════════════════════════════════════════════════════
    // HARMONIZED CORNER RADIUS SYSTEM
    // ═══════════════════════════════════════════════════════════════════════════
    //
    // This system creates visually consistent corners across all UI element sizes
    // using a saturating exponential curve. All elements share the same "curve
    // family" - they look like they belong together even at different sizes.
    //
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ FORMULA                                                                 │
    // ├─────────────────────────────────────────────────────────────────────────┤
    // │ r = Scale × Rref × (1 - e^(-m / (F × Rref)))                            │
    // │                                                                         │
    // │ Where:                                                                  │
    // │   m     = min(width, height)  — the limiting dimension                  │
    // │   Scale = uniform multiplier  — adjusts entire curve up/down            │
    // │   Rref  = reference radius    — asymptote that large elements approach  │
    // │   F     = falloff factor      — how quickly small elements catch up     │
    // └─────────────────────────────────────────────────────────────────────────┘
    //
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EXAMPLE OUTPUTS (Scale=1.0, Rref=14, F=4)                               │
    // ├─────────────────────────────────────────────────────────────────────────┤
    // │ Element          │ Dimensions │ m (min) │ Radius │ % of height         │
    // │──────────────────┼────────────┼─────────┼────────┼─────────────────────│
    // │ Small badge      │ 100×30     │ 30      │ ~6px   │ 20%                 │
    // │ Button           │ 150×50     │ 50      │ ~8px   │ 16%                 │
    // │ Translation box  │ 300×80     │ 80      │ ~10px  │ 12%                 │
    // │ Card             │ 450×300    │ 300     │ ~14px  │ 5%                  │
    // └─────────────────────────────────────────────────────────────────────────┘
    //
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ SCALE FACTOR EXAMPLES                                                   │
    // ├─────────────────────────────────────────────────────────────────────────┤
    // │ To adjust the entire curve uniformly, change HarmonizedScale:           │
    // │                                                                         │
    // │ Scale │ Badge (30px) │ Button (50px) │ Box (80px) │ Card (300px)        │
    // │───────┼──────────────┼───────────────┼────────────┼─────────────────────│
    // │ 0.7   │ ~4px         │ ~6px          │ ~7px       │ ~10px    (sharper)  │
    // │ 0.8   │ ~5px         │ ~6px          │ ~8px       │ ~11px               │
    // │ 1.0   │ ~6px         │ ~8px          │ ~10px      │ ~14px    (default)  │
    // │ 1.2   │ ~7px         │ ~10px         │ ~12px      │ ~17px               │
    // │ 1.4   │ ~8px         │ ~11px         │ ~14px      │ ~20px    (rounder)  │
    // └─────────────────────────────────────────────────────────────────────────┘
    //
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ TUNING GUIDE                                                            │
    // ├─────────────────────────────────────────────────────────────────────────┤
    // │ Problem                        │ Adjust                                 │
    // │────────────────────────────────┼────────────────────────────────────────│
    // │ Everything too round/sharp     │ Scale ↑/↓ (uniform change)             │
    // │ Large elements not round enough│ Rref ↑ (e.g., 18)                      │
    // │ Small elements too sharp       │ Falloff ↓ (e.g., 3) or MinRatio ↑      │
    // │ Small elements becoming pills  │ MaxRatio ↓ (e.g., 0.28)                │
    // └─────────────────────────────────────────────────────────────────────────┘
    //
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Uniform scale factor for the entire curve.
    /// 1.0 = default, &gt;1.0 = rounder, &lt;1.0 = sharper.
    /// This is the primary knob for adjusting the overall "roundness feel".
    /// </summary>
    const double HarmonizedScale = 1.0;

    /// <summary>
    /// Reference radius - the asymptotic value that large elements approach.
    /// Increasing this makes large elements rounder (small elements less affected).
    /// </summary>
    const double HarmonizedRref = 14.0;

    /// <summary>
    /// Falloff factor - controls curve steepness.
    /// Lower values make small elements catch up to Rref faster.
    /// Higher values keep small elements relatively sharper.
    /// </summary>
    const double HarmonizedFalloff = 4.0;

    /// <summary>
    /// Minimum corner ratio - prevents very small elements from being too sharp.
    /// Applied as: minRadius = elementSize × MinRatio
    /// </summary>
    const double HarmonizedMinRatio = 0.10;

    /// <summary>
    /// Maximum corner ratio - prevents elements from becoming pill-shaped.
    /// Applied as: maxRadius = elementSize × MaxRatio × Scale
    /// Note: Also scaled by HarmonizedScale to maintain proportions.
    /// </summary>
    const double HarmonizedMaxRatio = 0.32;

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
    public static double CalculateRadius(double width, double height, SquircleRadiusMode mode) => mode switch
    {
        SquircleRadiusMode.Harmonized => CalculateHarmonizedRadius(width, height),
        SquircleRadiusMode.Pill => Math.Round(Math.Min(width, height) * 0.44), // Slightly less than max to create tiny flat sides
        SquircleRadiusMode.NearPill => Math.Round(Math.Min(width, height) * 0.40), // 85% of full pill for app bar buttons
        SquircleRadiusMode.SemiPill => Math.Round(Math.Min(width, height) * 0.30), // 60% of full pill (50% * 0.6)
        SquircleRadiusMode.Natural => Math.Round(Math.Min(width, height) * 0.30),
        _ => CalculateHarmonizedRadius(width, height)
    };

    /// <summary>
    /// Calculates a harmonized radius using a saturating exponential curve.
    /// All elements share the same curve family, creating visual consistency.
    /// </summary>
    /// <remarks>
    /// Formula: r = Scale × Rref × (1 - exp(-m / (F × Rref)))
    /// where m = min(width, height), Scale = uniform multiplier,
    /// Rref = target radius, F = falloff factor.
    ///
    /// With default constants (Scale=1.0, Rref=14, F=4), this produces:
    /// - Small elements (30px): ~6px radius
    /// - Medium elements (80px): ~10px radius
    /// - Large elements (300px): ~14px radius (approaches Rref asymptotically)
    ///
    /// To adjust the entire curve uniformly, change HarmonizedScale.
    /// See the detailed documentation in the constants region above.
    /// </remarks>
    static double CalculateHarmonizedRadius(double width, double height)
    {
        var m = Math.Min(width, height);
        if (m <= 0) return 0;

        // Saturating curve: approaches (Scale × Rref) as m grows large
        var r = HarmonizedScale * HarmonizedRref * (1.0 - Math.Exp(-m / (HarmonizedFalloff * HarmonizedRref)));

        // Clamp to prevent pills and too-sharp corners
        // MaxRatio is also scaled to maintain proportions when Scale changes
        var rMin = m * HarmonizedMinRatio;
        var rMax = m * HarmonizedMaxRatio * HarmonizedScale;

        // Ensure we never exceed the absolute maximum (prevents geometry overlap)
        rMax = Math.Min(rMax, m * MaxSideRatio);

        return Math.Round(Math.Clamp(r, rMin, rMax));
    }
}

/// <summary>
/// Preset radius calculation modes for squircle elements.
/// </summary>
public enum SquircleRadiusMode
{
    /// <summary>
    /// Harmonized curve for all UI elements (default).
    /// Uses a saturating exponential that creates visual consistency
    /// across different element sizes.
    /// </summary>
    Harmonized,

    /// <summary>
    /// Nearly fully rounded ends (capsule/pill shape) with tiny flat sides.
    /// Uses min(width, height) * 0.44 to avoid sharp curve meeting points.
    /// </summary>
    Pill,

    /// <summary>
    /// Near-pill shape (85% of full pill radius).
    /// Very rounded buttons with minimal straight edges.
    /// Good for app bar buttons that should be very rounded.
    /// Uses min(width, height) * 0.4.
    /// </summary>
    NearPill,

    /// <summary>
    /// Semi-pill shape (60% of full pill radius).
    /// Creates buttons that are more rounded than Harmonized but retain
    /// some vertical straight edges.
    /// Uses min(width, height) * 0.3.
    /// </summary>
    SemiPill,

    /// <summary>
    /// Natural radius mode - uses approximately 30% of the smaller dimension.
    /// Useful for elements that need to be visually rounder than Harmonized
    /// but not as extreme as full Pill mode.
    /// </summary>
    Natural
}
