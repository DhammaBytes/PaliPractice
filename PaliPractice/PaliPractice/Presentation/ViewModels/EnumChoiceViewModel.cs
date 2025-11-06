namespace PaliPractice.Presentation.ViewModels;

[Bindable]
public sealed partial class EnumChoiceViewModel<T> : ObservableObject, IValidatableChoice
    where T : struct, Enum
{
    [ObservableProperty] T _selected;
    [ObservableProperty] T _expected;
    [ObservableProperty] ValidationState _validation = ValidationState.Unknown;

    public IReadOnlyList<EnumOption<T>> Options { get; }
    public IRelayCommand<T> SelectCommand { get; }

    public EnumChoiceViewModel(IEnumerable<EnumOption<T>> options, T initial = default)
    {
        Options = options.ToList();
        _selected = initial;
        SelectCommand = new RelayCommand<T>(value => Selected = value);
    }

    public bool HasSelection => !EqualityComparer<T>.Default.Equals(Selected, default!);

    partial void OnSelectedChanged(T value)
    {
        // Unknown if nothing selected; otherwise check against expected
        Validation = !HasSelection
            ? ValidationState.Unknown
            : EqualityComparer<T>.Default.Equals(Selected, Expected)
                ? ValidationState.Correct
                : ValidationState.Incorrect;

        // Debug logging
        System.Diagnostics.Debug.WriteLine(
            $"EnumChoice<{typeof(T).Name}>: Selected={Selected}, Expected={Expected}, HasSelection={HasSelection}, Validation={Validation}");

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when a new card appears to set the expected answer.
    /// </summary>
    public void SetExpected(T expected)
    {
        Expected = expected;
        // Re-evaluate current selection against new expected
        OnSelectedChanged(Selected);
    }

    /// <summary>
    /// Resets the choice to default state.
    /// </summary>
    public void Reset()
    {
        Selected = default!;
        Expected = default!;
        Validation = ValidationState.Unknown;
    }

    public event EventHandler? SelectionChanged;
}

public sealed record EnumOption<T>(T Value, string Label) where T : struct, Enum;