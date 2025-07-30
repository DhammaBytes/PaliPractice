namespace PaliPractice.Presentation.Behaviors;

[Bindable]
public partial class VoiceSelectionBehavior : ObservableObject
{
    [ObservableProperty] bool isNormalSelected = true;
    [ObservableProperty] bool isReflexiveSelected;

    public ICommand SelectNormalCommand { get; }
    public ICommand SelectReflexiveCommand { get; }

    public VoiceSelectionBehavior()
    {
        SelectNormalCommand = new RelayCommand(() => Set(true, false));
        SelectReflexiveCommand = new RelayCommand(() => Set(false, true));
    }

    void Set(bool normal, bool reflexive)
    {
        IsNormalSelected = normal;
        IsReflexiveSelected = reflexive;
    }
}
