namespace PaliPractice.Presentation.ViewModels;

[Microsoft.UI.Xaml.Data.Bindable]
public sealed partial class EnumChoiceViewModel<T> : ObservableObject where T : struct, Enum
{
    [ObservableProperty] private T _selected;
    public IReadOnlyList<EnumOption<T>> Options { get; }
    public IRelayCommand<T> SelectCommand { get; }

    public EnumChoiceViewModel(IEnumerable<EnumOption<T>> options, T initial = default)
    {
        Options = options.ToList();
        _selected = initial;
        SelectCommand = new RelayCommand<T>(value => Selected = value);
    }
}

public sealed record EnumOption<T>(T Value, string Label, string? Glyph = null, Color? ChipColor = null) where T : struct, Enum;