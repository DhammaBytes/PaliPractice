using System.ComponentModel;

namespace PaliPractice.Presentation.ViewModels.ButtonGroups;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class VoiceButtonGroupViewModel : ButtonGroupViewModel<Voice>
{
    public VoiceButtonGroupViewModel() : base(Voice.None) 
    {
        SelectNormalCommand = new RelayCommand(() => Select(Voice.Normal));
        SelectReflexiveCommand = new RelayCommand(() => Select(Voice.Reflexive));
        PropertyChanged += OnSelectionPropertyChanged;
    }

    void OnSelectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Selected))
        {
            OnPropertyChanged(nameof(IsNormalSelected));
            OnPropertyChanged(nameof(IsReflexiveSelected));
        }
    }

    public bool IsNormalSelected => IsSelected(Voice.Normal);
    public bool IsReflexiveSelected => IsSelected(Voice.Reflexive);

    public ICommand SelectNormalCommand { get; }
    public ICommand SelectReflexiveCommand { get; }
}