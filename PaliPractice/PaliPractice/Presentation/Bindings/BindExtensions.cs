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
    /// Binds ButtonBase.Command to an ICommand property path
    /// </summary>
    public static T Command<T, TDC>(this T button, Expression<Func<TDC, ICommand>> path)
        where T : ButtonBase
    {
        button.SetBinding(ButtonBase.CommandProperty, Bind.Path(path));
        return button;
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
    /// Use invert=true to reverse (show when empty, hide when non-empty).
    /// </summary>
    public static T StringToVisibility<T, TDC>(this T element, Expression<Func<TDC, string>> path, bool invert = false)
        where T : UIElement
    {
        var binding = Bind.Path(path);
        binding.Converter = StringNullOrEmptyToVisibilityConverter.Instance;
        binding.ConverterParameter = invert;
        element.SetBinding(UIElement.VisibilityProperty, binding);
        return element;
    }

    /// <summary>
    /// Binds SquircleBorder.Fill to a Color property path, converting to SolidColorBrush
    /// </summary>
    public static SquircleBorder FillColor<TDC>(this SquircleBorder border, Expression<Func<TDC, Color>> path)
    {
        var binding = Bind.Path(path);
        binding.Converter = ColorToBrushConverter.Instance;
        border.SetBinding(SquircleBorder.FillProperty, binding);
        return border;
    }


    extension(FontIcon icon)
    {
        /// <summary>
        /// Binds FontIcon visibility to whether the glyph string is non-null/non-empty
        /// </summary>
        public FontIcon GlyphWithVisibility<TDC>(Expression<Func<TDC, string?>> path)
        {
            icon.SetBinding(FontIcon.GlyphProperty, Bind.Path(path));
            var visBinding = Bind.Path(path);
            visBinding.Converter = StringNullOrEmptyToVisibilityConverter.Instance;
            icon.SetBinding(UIElement.VisibilityProperty, visBinding);
            return icon;
        }
    }

    /// <summary>
    /// Binds BitmapIcon.UriSource to a string path and visibility to whether the path is non-null/non-empty.
    /// Use with ShowAsMonochrome=true for Foreground tinting.
    /// </summary>
    public static BitmapIcon UriSourceWithVisibility<TDC>(this BitmapIcon icon, Expression<Func<TDC, string?>> path)
    {
        var binding = Bind.Path(path);
        binding.Converter = StringToUriConverter.Instance;
        icon.SetBinding(BitmapIcon.UriSourceProperty, binding);
        var visBinding = Bind.Path(path);
        visBinding.Converter = StringNullOrEmptyToVisibilityConverter.Instance;
        icon.SetBinding(UIElement.VisibilityProperty, visBinding);
        return icon;
    }

    /// <summary>
    /// Binds ToggleSwitch.IsOn to a bool property path (TwoWay binding)
    /// </summary>
    public static void IsOn<TDC>(this ToggleSwitch toggle, Expression<Func<TDC, bool>> path)
    {
        toggle.SetBinding(ToggleSwitch.IsOnProperty, Bind.TwoWayPath(path));
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

    extension<T>(T element) where T : UIElement
    {
        /// <summary>
        /// Binds UIElement.Opacity to a bool property within the current DataContext scope.
        /// true → 1.0 (visible), false → 0.0 (invisible but keeps layout space)
        /// </summary>
        public T OpacityWithin<TScope>(Expression<Func<TScope, bool>> path)
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
        public T BoolToOpacity<TDC>(Expression<Func<TDC, bool>> path)
        {
            var binding = Bind.Path(path);
            binding.Converter = BoolToOpacityConverter.Instance;
            element.SetBinding(UIElement.OpacityProperty, binding);
            return element;
        }
    }
}
