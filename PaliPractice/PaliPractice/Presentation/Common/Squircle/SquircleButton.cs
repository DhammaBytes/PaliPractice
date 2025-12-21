using Microsoft.UI.Xaml.Markup;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;

namespace PaliPractice.Presentation.Common.Squircle;

/// <summary>
/// A Button with squircle (superellipse) corners.
/// Derives from Button to get full button behavior (Command, keyboard, accessibility, VisualStateManager).
/// Uses a ControlTemplate with squircle-shaped Path overlays for visual states.
/// </summary>
/// <remarks>
/// <para><b>Usage:</b></para>
/// <code>
/// new SquircleButton()
///     .Fill(ThemeResource.Get&lt;Brush&gt;("PrimaryBrush"))
///     .RadiusMode(SquircleRadiusMode.ButtonSmall)
///     .Padding(20, 16)
///     .Command(() => vm.SomeCommand)
///     .Child(new TextBlock().Text("Click me"))
/// </code>
///
/// <para><b>Customization:</b></para>
/// <list type="bullet">
///   <item><c>Fill</c> - Background color of the button</item>
///   <item><c>RadiusMode</c> - Corner radius calculation mode (see <see cref="SquircleRadiusMode"/>)</item>
/// </list>
///
/// <para><b>To change overlay colors:</b> Modify the XAML template's PART_Hover and PART_Pressed Fill attributes.</para>
/// <para><b>To change overlay opacity:</b> Modify the To="0.08"/To="0.12" values in the VisualState Storyboards.</para>
/// <para><b>To change animation timing:</b> Modify the Duration values in the VisualState Storyboards.</para>
/// </remarks>
public class SquircleButton : Button
{
    // Template parts - retrieved in OnApplyTemplate after the template is applied
    ShapePath? _backgroundPath;
    ShapePath? _hoverPath;
    ShapePath? _pressedPath;

    public SquircleButton()
    {
        // Remove default button chrome (background/border) so our squircle is the only visual
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        BorderThickness = new Thickness(0);

        // Apply our custom ControlTemplate (created via XamlReader - see CreateControlTemplate)
        Template = CreateControlTemplate();

        // Geometry must be recalculated when size changes
        SizeChanged += OnSizeChanged;
    }

    #region Dependency Properties

    public static readonly DependencyProperty RadiusModeProperty = DependencyProperty.Register(
        nameof(RadiusMode),
        typeof(SquircleRadiusMode),
        typeof(SquircleButton),
        new PropertyMetadata(SquircleRadiusMode.ButtonSmall, OnShapePropertyChanged));

    /// <summary>
    /// The radius calculation mode for the squircle shape.
    /// </summary>
    public SquircleRadiusMode RadiusMode
    {
        get => (SquircleRadiusMode)GetValue(RadiusModeProperty);
        set => SetValue(RadiusModeProperty, value);
    }

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill),
        typeof(Brush),
        typeof(SquircleButton),
        new PropertyMetadata(null, OnFillChanged));

    /// <summary>
    /// The background fill brush.
    /// </summary>
    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    #endregion

    /// <summary>
    /// Creates the ControlTemplate for SquircleButton using XamlReader.
    /// This is the standard way to create templates in code without external XAML files.
    /// </summary>
    /// <remarks>
    /// Template structure:
    /// - Grid (RootGrid) - container with VisualStateManager
    ///   - Path (PART_Background) - squircle background, Fill set via UpdateFill()
    ///   - Path (PART_Hover) - dark overlay for hover state, opacity animated 0→0.08
    ///   - Path (PART_Pressed) - dark overlay for pressed state, opacity animated 0→0.12
    ///   - ContentPresenter - displays Button.Content with proper alignment/padding
    ///
    /// Visual States (CommonStates group - standard Button states):
    /// - Normal: both overlays at opacity 0
    /// - PointerOver: PART_Hover at 0.08 opacity (150ms animation)
    /// - Pressed: PART_Pressed at 0.12 opacity (50ms animation)
    /// - Disabled: RootGrid at 0.4 opacity
    ///
    /// To customize:
    /// - Overlay colors: modify Fill="Black" on PART_Hover/PART_Pressed
    /// - Overlay intensity: modify To="0.08" (hover) / To="0.12" (pressed) values
    /// - Animation speed: modify Duration="0:0:0.15" values
    /// - Focus states: add FocusStates VisualStateGroup if needed
    /// </remarks>
    static ControlTemplate CreateControlTemplate()
    {
        const string xaml = """
            <ControlTemplate
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                TargetType="Button">
                <Grid x:Name="RootGrid">
                    <VisualStateManager.VisualStateGroups>
                        <VisualStateGroup x:Name="CommonStates">
                            <VisualState x:Name="Normal">
                                <Storyboard>
                                    <DoubleAnimation Storyboard.TargetName="PART_Hover"
                                                     Storyboard.TargetProperty="Opacity"
                                                     To="0" Duration="0:0:0.15"/>
                                    <DoubleAnimation Storyboard.TargetName="PART_Pressed"
                                                     Storyboard.TargetProperty="Opacity"
                                                     To="0" Duration="0:0:0.15"/>
                                </Storyboard>
                            </VisualState>
                            <VisualState x:Name="PointerOver">
                                <Storyboard>
                                    <DoubleAnimation Storyboard.TargetName="PART_Hover"
                                                     Storyboard.TargetProperty="Opacity"
                                                     To="0.08" Duration="0:0:0.15"/>
                                </Storyboard>
                            </VisualState>
                            <VisualState x:Name="Pressed">
                                <Storyboard>
                                    <DoubleAnimation Storyboard.TargetName="PART_Pressed"
                                                     Storyboard.TargetProperty="Opacity"
                                                     To="0.12" Duration="0:0:0.05"/>
                                </Storyboard>
                            </VisualState>
                            <VisualState x:Name="Disabled">
                                <Storyboard>
                                    <DoubleAnimation Storyboard.TargetName="RootGrid"
                                                     Storyboard.TargetProperty="Opacity"
                                                     To="0.4" Duration="0:0:0"/>
                                </Storyboard>
                            </VisualState>
                        </VisualStateGroup>
                    </VisualStateManager.VisualStateGroups>
                    <Path x:Name="PART_Background"/>
                    <Path x:Name="PART_Hover" Fill="Black" Opacity="0"/>
                    <Path x:Name="PART_Pressed" Fill="Black" Opacity="0"/>
                    <ContentPresenter
                        Content="{TemplateBinding Content}"
                        Padding="{TemplateBinding Padding}"
                        HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}"
                        VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"/>
                </Grid>
            </ControlTemplate>
            """;

        return (ControlTemplate)XamlReader.Load(xaml);
    }

    /// <summary>
    /// Called when the ControlTemplate is applied. Retrieves template parts for later manipulation.
    /// </summary>
    /// <remarks>
    /// GetTemplateChild finds named elements defined in the XAML template.
    /// These references are needed to:
    /// - Set the Fill brush on PART_Background (can't use TemplateBinding for custom DP)
    /// - Update Path.Data geometry when size changes
    /// </remarks>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Retrieve named parts from the template for programmatic updates
        _backgroundPath = GetTemplateChild("PART_Background") as ShapePath;
        _hoverPath = GetTemplateChild("PART_Hover") as ShapePath;
        _pressedPath = GetTemplateChild("PART_Pressed") as ShapePath;

        // Apply initial Fill and geometry
        UpdateFill();
        UpdateGeometry();
    }

    static void OnShapePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SquircleButton button)
        {
            button.UpdateGeometry();
        }
    }

    static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SquircleButton button)
        {
            button.UpdateFill();
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

    /// <summary>
    /// Updates the squircle geometry on all Path elements based on current size and RadiusMode.
    /// </summary>
    /// <remarks>
    /// All three paths share the same geometry instance since they need identical shapes.
    /// The squircle is generated using iOS 7-style superellipse curves.
    /// See <see cref="SquircleGeometry"/> for the mathematical details.
    /// </remarks>
    void UpdateGeometry()
    {
        var width = ActualWidth;
        var height = ActualHeight;

        if (width <= 0 || height <= 0)
            return;

        // Calculate radius based on dimensions and RadiusMode preset
        var radius = SquircleGeometry.CalculateRadius(width, height, RadiusMode);

        // Generate the squircle PathGeometry (reused for all paths)
        var geometry = SquircleGeometry.Create(width, height, radius);

        // Apply same geometry to all three paths
        if (_backgroundPath != null)
            _backgroundPath.Data = geometry;

        if (_hoverPath != null)
            _hoverPath.Data = geometry;

        if (_pressedPath != null)
            _pressedPath.Data = geometry;
    }
}
