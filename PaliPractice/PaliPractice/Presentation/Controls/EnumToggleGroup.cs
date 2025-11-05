using System.Linq.Expressions;
using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Bindings.Converters;
using PaliPractice.Presentation.ViewModels;
using PaliPractice.Themes;

namespace PaliPractice.Presentation.Controls;

public static class EnumToggleGroup
{
    // Single row layout
    public static StackPanel Build<TDC, T>(
        EnumOption<T>[] options,
        Expression<Func<TDC, EnumChoiceViewModel<T>>> viewModelPath) where T : struct, Enum
    {
        return BuildWithLayout(options, viewModelPath, null);
    }

    // Multi-row layout with specified row sizes
    public static StackPanel BuildWithLayout<TDC, T>(
        EnumOption<T>[] options,
        Expression<Func<TDC, EnumChoiceViewModel<T>>> viewModelPath,
        int[]? rowSizes = null) where T : struct, Enum
    {
        // Create container and set its DataContext to the EnumChoiceViewModel
        var container = new StackPanel()
            .Scope<StackPanel, TDC, EnumChoiceViewModel<T>>(viewModelPath);

        // Build all buttons using relative bindings within the scoped DataContext
        var buttons = options.Select(option => CreateButton<T>(option)).ToArray();

        // Layout buttons based on row sizes
        if (rowSizes == null || rowSizes.Length == 0)
        {
            // Single horizontal row
            container
                .Orientation(Orientation.Horizontal)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Spacing(8)
                .Children(buttons);
        }
        else
        {
            // Multi-row layout
            container
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
        }

        return container;
    }

    private static ToggleButton CreateButton<T>(EnumOption<T> option) where T : struct, Enum
    {
        var button = new ToggleButton()
            .Padding(12, 6)
            .Background(OptionPresentation.GetChipBrush(option.Value))
            .CommandParameter(option.Value)
            .CommandWithin<ToggleButton, EnumChoiceViewModel<T>>(vm => vm.SelectCommand);

        // Bind IsChecked using relative path + EqualityConverter
        var isCheckedBinding = BindExtensions.Relative<EnumChoiceViewModel<T>, T>(vm => vm.Selected);
        isCheckedBinding.Converter = EqualityConverter<T>.For(option.Value);
        button.SetBinding(ToggleButton.IsCheckedProperty, isCheckedBinding);

        // Build content
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

        return button;
    }
}