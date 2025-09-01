using PaliPractice.Presentation.ViewModels;

namespace PaliPractice.Presentation.Components;

public static class EnumToggleGroup
{
    // We need to pre-define the structure since we can't access VM at build time
    // This means we need to know the options beforehand
    public static StackPanel Build<T>(
        EnumOption<T>[] options,
        Func<EnumChoiceViewModel<T>> getViewModel) where T : struct, Enum
    {
        var container = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(8);

        // Build the UI structure with the known options
        var buttons = options.Select(option =>
        {
            var button = new ToggleButton()
                .Padding(12, 6);

            if (option.ChipColor != null)
            {
                button.Background(new SolidColorBrush(option.ChipColor.Value));
            }

            var contentChildren = new List<UIElement>();
            if (option.Glyph != null)
            {
                contentChildren.Add(new FontIcon().Glyph(option.Glyph).FontSize(14));
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

        container.Children(buttons);
        return container;
    }
}