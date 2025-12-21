using Windows.Foundation;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;

namespace PaliPractice.Presentation.Common.Squircle;

// Simple squircle button background
// new SquircleBorder()
//     .Fill(ThemeResource.Get<Brush>("PrimaryBrush"))
//     .Radius(20)
//     .Child(
//         new TextBlock().Text("Click me")
//     )
//
// Auto-calculated pill shape (like badges)
// new SquircleBorder()
//     .Fill(ThemeResource.Get<Brush>("AccentBrush"))
//     .RadiusMode(SquircleRadiusMode.Pill)
//     .Child(...)
//
// Only round top corners
// new SquircleBorder()
//     .Fill(ThemeResource.Get<Brush>("SurfaceBrush"))
//     .Radius(24)
//     .Corners(SquircleCorners.Top)
//     .Child(...)
//
// With stroke border
// new SquircleBorder()
//     .Stroke(ThemeResource.Get<Brush>("OutlineBrush"))
//     .StrokeThickness(2)
//     .Radius(16)
//     .Child(...)

/// <summary>
/// A border control with squircle (superellipse) corners.
/// Use instead of Border when you want iOS-style smooth corners.
/// </summary>
public class SquircleBorder : ContentControl
{
    ShapePath? _backgroundPath;
    ShapePath? _borderPath;
    ContentPresenter? _contentPresenter;
    Grid? _rootGrid;

    public SquircleBorder()
    {
        DefaultStyleKey = typeof(SquircleBorder);
        SizeChanged += OnSizeChanged;
        BuildVisualTree();
    }

    #region Dependency Properties

    public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(
        nameof(Radius),
        typeof(double),
        typeof(SquircleBorder),
        new PropertyMetadata(16.0, OnShapePropertyChanged));

    /// <summary>
    /// The corner radius for the squircle shape.
    /// </summary>
    public double Radius
    {
        get => (double)GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public static readonly DependencyProperty CornersProperty = DependencyProperty.Register(
        nameof(Corners),
        typeof(SquircleCorners),
        typeof(SquircleBorder),
        new PropertyMetadata(SquircleCorners.All, OnShapePropertyChanged));

    public SquircleCorners Corners
    {
        get => (SquircleCorners)GetValue(CornersProperty);
        set => SetValue(CornersProperty, value);
    }

    public static readonly DependencyProperty RadiusModeProperty = DependencyProperty.Register(
        nameof(RadiusMode),
        typeof(SquircleRadiusMode?),
        typeof(SquircleBorder),
        new PropertyMetadata(null, OnShapePropertyChanged));

    /// <summary>
    /// If set, overrides Radius with an auto-calculated value based on dimensions.
    /// </summary>
    public SquircleRadiusMode? RadiusMode
    {
        get => (SquircleRadiusMode?)GetValue(RadiusModeProperty);
        set => SetValue(RadiusModeProperty, value);
    }

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill),
        typeof(Brush),
        typeof(SquircleBorder),
        new PropertyMetadata(null, OnFillChanged));

    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        nameof(Stroke),
        typeof(Brush),
        typeof(SquircleBorder),
        new PropertyMetadata(null, OnStrokeChanged));

    public Brush? Stroke
    {
        get => (Brush?)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness),
        typeof(double),
        typeof(SquircleBorder),
        new PropertyMetadata(1.0, OnStrokeChanged));

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    #endregion

    void BuildVisualTree()
    {
        _backgroundPath = new ShapePath();
        _borderPath = new ShapePath();
        _contentPresenter = new ContentPresenter
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };

        _rootGrid = new Grid();
        _rootGrid.Children.Add(_backgroundPath);
        _rootGrid.Children.Add(_contentPresenter);
        _rootGrid.Children.Add(_borderPath);

        base.Content = _rootGrid;

        UpdateFill();
        UpdateStroke();
    }

    // Override Content to route to our ContentPresenter
    public new object? Content
    {
        get => _contentPresenter?.Content;
        set
        {
            if (_contentPresenter != null)
            {
                _contentPresenter.Content = value;
            }
        }
    }

    static void OnShapePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SquircleBorder border)
        {
            border.UpdateGeometry();
        }
    }

    static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SquircleBorder border)
        {
            border.UpdateFill();
        }
    }

    static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SquircleBorder border)
        {
            border.UpdateStroke();
        }
    }

    void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateGeometry();
    }

    void UpdateFill()
    {
        if (_backgroundPath != null)
        {
            _backgroundPath.Fill = Fill;
        }
    }

    void UpdateStroke()
    {
        if (_borderPath != null)
        {
            _borderPath.Stroke = Stroke;
            _borderPath.StrokeThickness = StrokeThickness;
            _borderPath.Fill = null;
        }
    }

    void UpdateGeometry()
    {
        var width = ActualWidth;
        var height = ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        var radius = RadiusMode.HasValue
            ? SquircleGeometry.CalculateRadius(width, height, RadiusMode.Value)
            : Radius;

        var geometry = SquircleGeometry.Create(width, height, radius, Corners);

        if (_backgroundPath != null)
        {
            _backgroundPath.Data = geometry;
        }

        if (_borderPath != null && Stroke != null)
        {
            // For border, create slightly inset geometry to account for stroke thickness
            var inset = StrokeThickness / 2;
            var borderGeometry = SquircleGeometry.Create(
                width - StrokeThickness,
                height - StrokeThickness,
                Math.Max(0, radius - inset),
                Corners);

            // Translate to center the border stroke
            var transform = new TranslateTransform { X = inset, Y = inset };
            borderGeometry.Transform = transform;
            _borderPath.Data = borderGeometry;
        }
    }
}
