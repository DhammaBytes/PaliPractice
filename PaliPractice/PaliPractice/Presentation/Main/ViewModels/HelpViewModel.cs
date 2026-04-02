using PaliPractice.Localization;

namespace PaliPractice.Presentation.Main.ViewModels;

[Bindable]
public partial class HelpViewModel : ObservableObject
{
    readonly INavigator _navigator;

    public HelpViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    public ICommand GoBackCommand => new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));

    // Section titles
    public static string HowToPracticeTitle => AppText.Get("Help.HowToPractice.Title");
    public static string SpacedRepetitionTitle => AppText.Get("Help.SpacedRepetition.Title");
    public static string QuestionsTitle => AppText.Get("Help.Questions.Title");

    // Section content
    public static string HowToPractice => AppText.Get("Help.HowToPractice.Content");

    public static string SpacedRepetition => AppText.Get("Help.SpacedRepetition.Content");

    // FAQ - individual questions
    public static string FaqGrammar => AppText.Get("Help.Faq.Grammar");

    public static string FaqMultipleForms => AppText.Get("Help.Faq.MultipleForms");

    public static string FaqExamples => AppText.Get("Help.Faq.Examples");

    public static string FaqMissingWords => AppText.Get("Help.Faq.MissingWords");

    public static string FaqMissingForms => AppText.Get("Help.Faq.MissingForms");

    public static string FaqAboutApp => AppText.Get("Help.Faq.AboutApp");

    public static string FaqDataStorage => AppText.Get("Help.Faq.DataStorage");
}
