namespace PaliPractice.Presentation.Practice.Controls;

/// <summary>
/// Displays the answer text after reveal.
/// Shows empty placeholder until revealed, then shows the inflected form.
/// </summary>
public static class AnswerDisplay
{
    /// <summary>
    /// Builds the answer display area with bindings.
    /// </summary>
    /// <param name="bindAnswer">Action to bind the answer text</param>
    /// <param name="bindVisibility">Action to bind visibility (shown when revealed)</param>
    public static Border Build<TDC>(
        Action<TextBlock> bindAnswer,
        Action<Border> bindVisibility)
    {
        var container = new Border()
            .MinHeight(60)
            .Background(ThemeResource.Get<Brush>("SurfaceVariantBrush"))
            .CornerRadius(8)
            .Padding(16, 12)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MinWidth(200);

        bindVisibility(container);

        var answerText = new TextBlock()
            .FontSize(32)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"));

        bindAnswer(answerText);

        container.Child(answerText);

        return container;
    }
}
