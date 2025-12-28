using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings.Converters;

namespace PaliPractice.Presentation.Bindings;

/// <summary>
/// Fluent extensions for compile-time-safe data binding in Uno C# Markup.
/// These extensions allow expression-based bindings that defer to runtime DataContext.
/// </summary>
public static class BindExtensions
{
    /// <summary>
    /// Binds an element's DataContext to a scoped sub-ViewModel path.
    /// Use this to create a binding scope, then use relative bindings inside.
    /// </summary>
    public static T Scope<T, TDC, TScope>(this T element, Expression<Func<TDC, TScope>> path)
        where T : FrameworkElement
    {
        element.SetBinding(FrameworkElement.DataContextProperty, Bind.Path(path));
        return element;
    }

    /// <summary>
    /// Creates a binding relative to the current DataContext type (no parent TDC).
    /// Use this inside components that have set their own DataContext via Scope.
    /// </summary>
    public static Binding Relative<TScope, TProp>(Expression<Func<TScope, TProp>> expr)
        => Bind.Path(expr);

    // Absolute bindings (from parent DataContext)

    /// <summary>
    /// Binds TextBlock.Text to a string property path
    /// </summary>
    public static TextBlock Text<TDC>(this TextBlock textBlock, Expression<Func<TDC, string>> path)
    {
        textBlock.SetBinding(TextBlock.TextProperty, Bind.Path(path));
        return textBlock;
    }

    /// <summary>
    /// Binds ProgressBar.Value to a double property path
    /// </summary>
    public static ProgressBar Value<TDC>(this ProgressBar progressBar, Expression<Func<TDC, double>> path)
    {
        progressBar.SetBinding(ProgressBar.ValueProperty, Bind.Path(path));
        return progressBar;
    }

    /// <summary>
    /// Binds ButtonBase.Command to an ICommand property path
    /// </summary>
    public static T Command<T, TDC>(this T button, Expression<Func<TDC, ICommand>> path)
        where T : ButtonBase
    {
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(path));
        return button;
    }

    /// <summary>
    /// Binds ProgressRing.IsActive to a bool property path
    /// </summary>
    public static ProgressRing IsActive<TDC>(this ProgressRing progressRing, Expression<Func<TDC, bool>> path)
    {
        progressRing.SetBinding(ProgressRing.IsActiveProperty, Bind.Path(path));
        return progressRing;
    }

    /// <summary>
    /// Binds UIElement.Visibility to a bool property path with BoolToVisibilityConverter
    /// Automatically detects and unwraps logical NOT (!) expressions
    /// </summary>
    public static T BoolToVisibility<T, TDC>(this T element, Expression<Func<TDC, bool>> path, bool invert = false)
        where T : UIElement
    {
        // Detect and unwrap logical NOT
        if (path.Body is UnaryExpression { NodeType: ExpressionType.Not } u)
        {
            invert = !invert;
            path = Expression.Lambda<Func<TDC, bool>>(u.Operand, path.Parameters);
        }

        var binding = Bind.Path(path);
        binding.Converter = BoolToVisibilityConverter.Instance;
        binding.ConverterParameter = invert;
        element.SetBinding(UIElement.VisibilityProperty, binding);
        return element;
    }

    /// <summary>
    /// Binds UIElement.Visibility to a string property path.
    /// Empty/null strings are hidden, non-empty strings are visible.
    /// </summary>
    public static T StringToVisibility<T, TDC>(this T element, Expression<Func<TDC, string>> path)
        where T : UIElement
    {
        var binding = Bind.Path(path);
        binding.Converter = StringNullOrEmptyToVisibilityConverter.Instance;
        element.SetBinding(UIElement.VisibilityProperty, binding);
        return element;
    }

    /// <summary>
    /// Binds Control.Background to a Brush property path
    /// </summary>
    public static T Background<T, TDC>(this T control, Expression<Func<TDC, Brush>> path)
        where T : Control
    {
        control.SetBinding(Control.BackgroundProperty, Bind.Path(path));
        return control;
    }

    /// <summary>
    /// Binds Border.Background to a Brush property path
    /// </summary>
    public static Border Background<TDC>(this Border border, Expression<Func<TDC, Brush>> path)
    {
        border.SetBinding(Border.BackgroundProperty, Bind.Path(path));
        return border;
    }

    /// <summary>
    /// Binds SquircleBorder.Fill to a Brush property path
    /// </summary>
    public static SquircleBorder Fill<TDC>(this SquircleBorder border, Expression<Func<TDC, Brush>> path)
    {
        border.SetBinding(SquircleBorder.FillProperty, Bind.Path(path));
        return border;
    }


    /// <summary>
    /// Binds FormattedTextBehavior.HtmlText to a string property path.
    /// Parses &lt;b&gt;...&lt;/b&gt; tags and creates bold Runs.
    /// </summary>
    public static TextBlock HtmlText<TDC>(this TextBlock textBlock, Expression<Func<TDC, string>> path)
    {
        textBlock.SetBinding(FormattedTextBehavior.HtmlTextProperty, Bind.Path(path));
        return textBlock;
    }

    /// <summary>
    /// Binds FontIcon.Glyph to a string property path
    /// </summary>
    public static FontIcon Glyph<TDC>(this FontIcon icon, Expression<Func<TDC, string?>> path)
    {
        icon.SetBinding(FontIcon.GlyphProperty, Bind.Path(path));
        return icon;
    }

    /// <summary>
    /// Binds FontIcon visibility to whether the glyph string is non-null/non-empty
    /// </summary>
    public static FontIcon GlyphWithVisibility<TDC>(this FontIcon icon, Expression<Func<TDC, string?>> path)
    {
        icon.SetBinding(FontIcon.GlyphProperty, Bind.Path(path));
        var visBinding = Bind.Path(path);
        visBinding.Converter = StringNullOrEmptyToVisibilityConverter.Instance;
        icon.SetBinding(UIElement.VisibilityProperty, visBinding);
        return icon;
    }

    /// <summary>
    /// Binds ToggleSwitch.IsOn to a bool property path (TwoWay binding)
    /// </summary>
    public static ToggleSwitch IsOn<TDC>(this ToggleSwitch toggle, Expression<Func<TDC, bool>> path)
    {
        toggle.SetBinding(ToggleSwitch.IsOnProperty, Bind.TwoWayPath(path));
        return toggle;
    }

    /// <summary>
    /// Binds Control.IsEnabled to a bool property path
    /// </summary>
    public static T IsEnabled<T, TDC>(this T control, Expression<Func<TDC, bool>> path)
        where T : Control
    {
        control.SetBinding(Control.IsEnabledProperty, Bind.Path(path));
        return control;
    }

    /// <summary>
    /// Binds UIElement.Visibility to a bool property path (true = Visible, false = Collapsed)
    /// </summary>
    public static T Visibility<T, TDC>(this T element, Expression<Func<TDC, bool>> path)
        where T : UIElement
    {
        return element.BoolToVisibility<T, TDC>(path);
    }

    /// <summary>
    /// Binds CheckBox.IsChecked to a bool property path (TwoWay binding)
    /// </summary>
    public static CheckBox IsChecked<TDC>(this CheckBox checkBox, Expression<Func<TDC, bool>> path)
    {
        checkBox.SetBinding(CheckBox.IsCheckedProperty, Bind.TwoWayPath(path));
        return checkBox;
    }
}

/// <summary>
/// Scoped binding extensions for use inside components with a defined DataContext scope.
/// These bind to properties on the current (scoped) DataContext without needing to know the parent VM type.
/// Use after calling .Scope() to set the component's DataContext to a sub-ViewModel.
/// </summary>
public static class BindWithin
{
    /// <summary>
    /// Binds TextBlock.Text to a property within the current DataContext scope
    /// </summary>
    public static TextBlock TextWithin<TScope>(this TextBlock textBlock, Expression<Func<TScope, string>> path)
    {
        textBlock.SetBinding(TextBlock.TextProperty, BindExtensions.Relative(path));
        return textBlock;
    }

    /// <summary>
    /// Binds FormattedTextBehavior.HtmlText to a property within the current DataContext scope.
    /// Parses &lt;b&gt;...&lt;/b&gt; tags and creates bold Runs.
    /// </summary>
    public static TextBlock HtmlTextWithin<TScope>(this TextBlock textBlock, Expression<Func<TScope, string>> path)
    {
        textBlock.SetBinding(FormattedTextBehavior.HtmlTextProperty, BindExtensions.Relative(path));
        return textBlock;
    }

    /// <summary>
    /// Binds ProgressBar.Value to a property within the current DataContext scope
    /// </summary>
    public static ProgressBar ValueWithin<TScope>(this ProgressBar progressBar, Expression<Func<TScope, double>> path)
    {
        progressBar.SetBinding(ProgressBar.ValueProperty, BindExtensions.Relative(path));
        return progressBar;
    }

    /// <summary>
    /// Binds UIElement.Visibility to a bool property within the current DataContext scope.
    /// Automatically detects and unwraps logical NOT (!) expressions.
    /// </summary>
    public static T VisibilityWithin<T, TScope>(this T element, Expression<Func<TScope, bool>> path, bool invert = false)
        where T : UIElement
    {
        // Detect and unwrap logical NOT
        if (path.Body is UnaryExpression { NodeType: ExpressionType.Not } u)
        {
            invert = !invert;
            path = Expression.Lambda<Func<TScope, bool>>(u.Operand, path.Parameters);
        }

        var binding = BindExtensions.Relative(path);
        binding.Converter = BoolToVisibilityConverter.Instance;
        binding.ConverterParameter = invert;
        element.SetBinding(UIElement.VisibilityProperty, binding);
        return element;
    }

    /// <summary>
    /// Binds ButtonBase.Command to a property within the current DataContext scope
    /// </summary>
    public static T CommandWithin<T, TScope>(this T button, Expression<Func<TScope, ICommand>> path)
        where T : ButtonBase
    {
        button.SetBinding(ButtonBase.CommandProperty, BindExtensions.Relative(path));
        return button;
    }

    /// <summary>
    /// Binds UIElement.Opacity to a bool property within the current DataContext scope.
    /// true → 1.0 (visible), false → 0.0 (invisible but keeps layout space)
    /// </summary>
    public static T OpacityWithin<T, TScope>(this T element, Expression<Func<TScope, bool>> path)
        where T : UIElement
    {
        var binding = BindExtensions.Relative(path);
        binding.Converter = BoolToOpacityConverter.Instance;
        element.SetBinding(UIElement.OpacityProperty, binding);
        return element;
    }

    /// <summary>
    /// Binds UIElement.Opacity to a bool property path.
    /// true → 1.0 (visible), false → 0.0 (invisible but keeps layout space)
    /// </summary>
    public static T BoolToOpacity<T, TDC>(this T element, Expression<Func<TDC, bool>> path)
        where T : UIElement
    {
        var binding = Bind.Path(path);
        binding.Converter = BoolToOpacityConverter.Instance;
        element.SetBinding(UIElement.OpacityProperty, binding);
        return element;
    }
}
