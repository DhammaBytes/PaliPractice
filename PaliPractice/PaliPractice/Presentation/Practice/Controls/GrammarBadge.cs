namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Non-interactive badge for displaying grammatical category (gender, number, case, etc.).
/// Uses Action bindings to ensure lambdas are visible at call site per CLAUDE.md pattern.
/// </summary>
public static class GrammarBadge
{
    /// <summary>
    /// Builds a badge with bindings for label, background brush, and optional glyph.
    /// </summary>
    /// <param name="bindLabel">Action to bind label text</param>
    /// <param name="bindBackground">Action to bind background brush</param>
    /// <param name="bindGlyph">Optional action to bind glyph (FontIcon)</param>
    public static Border Build<TDC>(
        Action<TextBlock> bindLabel,
        Action<Border> bindBackground,
        Action<FontIcon>? bindGlyph = null)
    {
        var badge = new Border()
            .CornerRadius(16)
            .Padding(12, 6);

        bindBackground(badge);

        var contentChildren = new List<UIElement>();

        // Optional glyph icon
        if (bindGlyph != null)
        {
            var glyphIcon = new FontIcon()
                .FontSize(14)
                .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"));
            bindGlyph(glyphIcon);
            contentChildren.Add(glyphIcon);
        }

        // Label text
        var labelText = new TextBlock()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"));
        bindLabel(labelText);
        contentChildren.Add(labelText);

        badge.Child(
            new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(contentChildren.ToArray())
        );

        return badge;
    }

    /// <summary>
    /// Builds a row of badges for declension: [Gender] [Number] [Case]
    /// </summary>
    public static StackPanel BuildDeclensionRow<TDC>(
        Action<TextBlock> bindGenderLabel,
        Action<Border> bindGenderBackground,
        Action<FontIcon> bindGenderGlyph,
        Action<TextBlock> bindNumberLabel,
        Action<Border> bindNumberBackground,
        Action<FontIcon> bindNumberGlyph,
        Action<TextBlock> bindCaseLabel,
        Action<Border> bindCaseBackground)
    {
        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(12)
            .Children(
                Build<TDC>(bindCaseLabel, bindCaseBackground),
                Build<TDC>(bindGenderLabel, bindGenderBackground, bindGenderGlyph),
                Build<TDC>(bindNumberLabel, bindNumberBackground, bindNumberGlyph)
            );
    }

    /// <summary>
    /// Builds a row of badges for conjugation: [Person] [Number] [Tense]
    /// </summary>
    public static StackPanel BuildConjugationRow<TDC>(
        Action<TextBlock> bindPersonLabel,
        Action<Border> bindPersonBackground,
        Action<FontIcon> bindPersonGlyph,
        Action<TextBlock> bindNumberLabel,
        Action<Border> bindNumberBackground,
        Action<FontIcon> bindNumberGlyph,
        Action<TextBlock> bindTenseLabel,
        Action<Border> bindTenseBackground)
    {
        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(12)
            .Children(
                Build<TDC>(bindTenseLabel, bindTenseBackground),
                Build<TDC>(bindPersonLabel, bindPersonBackground, bindPersonGlyph),
                Build<TDC>(bindNumberLabel, bindNumberBackground, bindNumberGlyph)
            );
    }
}
