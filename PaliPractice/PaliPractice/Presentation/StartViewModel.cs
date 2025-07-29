using System.Windows.Input;

namespace PaliPractice.Presentation;

public partial class StartViewModel : ObservableObject
{
    private readonly INavigator _navigator;

    public StartViewModel(INavigator navigator)
    {
        _navigator = navigator;
        
        GoToDeclensionCommand = new AsyncRelayCommand(GoToDeclension);
        GoToConjugationCommand = new AsyncRelayCommand(GoToConjugation);
    }

    public string Title => "Pali Practice";
    
    public ICommand GoToDeclensionCommand { get; }
    public ICommand GoToConjugationCommand { get; }

    private async Task GoToDeclension()
    {
        await _navigator.NavigateViewModelAsync<DeclensionPracticeViewModel>(this);
    }

    private async Task GoToConjugation()
    {
        // TODO: Implement conjugation practice navigation
        // For now, this will be implemented later
        await Task.CompletedTask;
    }
}