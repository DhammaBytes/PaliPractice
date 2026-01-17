using static PaliPractice.Presentation.Common.Text.TextHelpers;

namespace PaliPractice.Presentation.Settings.Controls;

/// <summary>
/// A settings row for a gender with a label and wrapping pattern checkboxes.
/// Layout: [Gender Label]  [x] a  [x] i  [x] ī  [x] u ...
///                         [x] as [x] ar ...
/// </summary>
public static class GenderPatternSection
{
    const int CheckboxesPerRow = 4;

    /// <summary>
    /// Builds a gender row with label left, wrapping pattern checkboxes right.
    /// </summary>
    /// <param name="genderLabel">Display name (e.g., "Masculine")</param>
    /// <param name="patterns">Array of (label, bindCheckBox, bindIsEnabled) for each pattern</param>
    public static Grid Build(
        string genderLabel,
        (string label, Action<CheckBox> bindCheckBox, Action<CheckBox> bindIsEnabled)[] patterns)
    {
        // Create rows of checkboxes (4 per row)
        var checkboxContainer = new StackPanel()
            .Spacing(4);

        var chunks = patterns
            .Select((p, i) => new { Pattern = p, Index = i })
            .GroupBy(x => x.Index / CheckboxesPerRow)
            .Select(g => g.Select(x => x.Pattern).ToArray())
            .ToList();

        foreach (var chunk in chunks)
        {
            var row = new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(4);

            foreach (var (label, bindCheckBox, bindIsEnabled) in chunk)
            {
                var checkBox = new CheckBox()
                    .VerticalContentAlignment(VerticalAlignment.Center)
                    .Content(
                        PaliText()
                            .Text(label)
                            .FontSize(18)
                            .Margin(4, 0, 0, 0)  // Left margin for spacing from checkbox box
                    )
                    .MinWidth(60)
                    .Padding(4, 0);

                bindCheckBox(checkBox);
                bindIsEnabled(checkBox);

                row.Children.Add(checkBox);
            }

            checkboxContainer.Children.Add(row);
        }

        return new Grid()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .ColumnDefinitions("Auto,*")
            .Padding(16, 12)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Children(
                // Gender label, top-aligned to first row
                RegularText()
                    .Text(genderLabel)
                    .FontSize(16)
                    .VerticalAlignment(VerticalAlignment.Top)
                    .Margin(0, 6, 16, 0)  // Top margin to align with checkbox content
                    .Grid(column: 0),
                // Rows of checkboxes, right-aligned
                checkboxContainer
                    .HorizontalAlignment(HorizontalAlignment.Right)
                    .Grid(column: 1)
            );
    }
}
