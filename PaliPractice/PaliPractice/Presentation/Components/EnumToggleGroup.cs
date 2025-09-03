using PaliPractice.Presentation.ViewModels;
using PaliPractice.Presentation.Helpers;

namespace PaliPractice.Presentation.Components;

public static class EnumToggleGroup
{
    // Single row layout - original method
    public static StackPanel Build<T>(
        EnumOption<T>[] options,
        Func<EnumChoiceViewModel<T>> getViewModel) where T : struct, Enum
    {
        return BuildWithLayout(options, getViewModel, null);
    }

    // Multi-row layout with specified row sizes
    public static StackPanel BuildWithLayout<T>(
        EnumOption<T>[] options,
        Func<EnumChoiceViewModel<T>> getViewModel,
        int[]? rowSizes = null) where T : struct, Enum
    {
        // Build all buttons first
        var buttons = options.Select(option =>
        {
            var button = new ToggleButton()
                .Padding(12, 6);

            var chipColor = OptionPresentation.GetChipColor(option.Value);
            button.Background(new SolidColorBrush(chipColor));

            var contentChildren = new List<UIElement>();
            var glyph = OptionPresentation.GetGlyph(option.Value);
            if (glyph != null)
            {
                contentChildren.Add(new FontIcon().Glyph(glyph).FontSize(14));
            }
            contentChildren.Add(new TextBlock().Text(option.Label));

            button.Content(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(8)
                .Children(contentChildren.ToArray())
            );

            // Use lambda to defer VM access until runtime
            button
                .IsChecked(() => EqualityComparer<T>.Default.Equals(getViewModel().Selected, option.Value))
                .Command(() => new RelayCommand(() => getViewModel().SelectCommand.Execute(option.Value)));

            return button;
        }).ToArray();

        // If no row sizes specified, single horizontal row
        if (rowSizes == null || rowSizes.Length == 0)
        {
            var singleRow = new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(8);
            singleRow.Children(buttons);
            return singleRow;
        }

        // Multi-row layout
        var container = new StackPanel()
            .Orientation(Orientation.Vertical)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(8);

        var buttonIndex = 0;
        foreach (var rowSize in rowSizes)
        {
            if (buttonIndex >= buttons.Length) break;

            var row = new StackPanel()
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(8);

            var buttonsForRow = buttons.Skip(buttonIndex).Take(rowSize).ToArray();
            row.Children(buttonsForRow);
            container.Children(row);
            
            buttonIndex += rowSize;
        }

        return container;
    }
}