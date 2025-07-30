namespace PaliPractice.Presentation;

[Bindable]
public class StartViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public StartViewModel(INavigator navigator)
    {
        _navigator = navigator;
        
        GoToDeclensionCommand = new AsyncRelayCommand(GoToDeclension);
        GoToConjugationCommand = new AsyncRelayCommand(GoToConjugation);
    }

    public string Title => "Pali Practice";
    
    public ICommand GoToDeclensionCommand { get; }
    public ICommand GoToConjugationCommand { get; }

    async Task GoToDeclension()
    {
        await _navigator.NavigateViewModelAsync<DeclensionPracticeViewModel>(this);
    }

    async Task GoToConjugation()
    {
        await _navigator.NavigateViewModelAsync<ConjugationPracticeViewModel>(this);
    }
}