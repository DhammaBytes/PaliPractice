namespace PaliPractice.Presentation.Shared.ViewModels;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class Selection<T> : ObservableObject where T : struct, Enum
{
    [ObservableProperty] private T _selected;

    public Selection(T defaultValue = default)
    {
        _selected = defaultValue;
        SelectCommand = new RelayCommand<T>(Select);
    }

    public bool IsSelected(T value) => EqualityComparer<T>.Default.Equals(Selected, value);
    public void Select(T value) => Selected = value;

    public IRelayCommand<T> SelectCommand { get; }
    
    // Helper methods for specific enum values - will need to be implemented by derived classes
    public virtual ICommand GetSelectCommand(T value) => new RelayCommand(() => Select(value));
}