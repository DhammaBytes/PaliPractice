using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Practice;

public sealed partial class ConjugationPracticePage : Page
{
    public ConjugationPracticePage()
    {
        var elements = new ResponsiveElements();

        ConjugationPracticePageMarkup.DataContext<ViewModels.ConjugationPracticeViewModel>(this, (page, _) => page
            .NavigationCacheMode<ConjugationPracticePage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(BuildPageLayout(elements))
        );

        if (elements.ContentArea is not null)
        {
            HeightResponsiveHelper.AttachResponsiveHandler(elements.ContentArea, heightClass =>
                PracticePageBuilder.ApplyResponsiveValues(elements, heightClass));
        }
    }

    static Grid BuildPageLayout(ResponsiveElements elements)
    {
        var config = new PracticePageConfig<ViewModels.ConjugationPracticeViewModel>(
            Title: "Conjugation Practice",
            RankPrefix: "V",
            WordCardPath: vm => vm.WordCard,
            AnswerStemPath: vm => vm.Flashcard.AnswerStem,
            AnswerEndingPath: vm => vm.Flashcard.AnswerEnding,
            AlternativeFormsPath: vm => vm.AlternativeForms,
            IsRevealedPath: vm => vm.Flashcard.IsRevealed,
            CarouselPath: vm => vm.ExampleCarousel,
            GoBackCommandPath: vm => vm.GoBackCommand,
            GoToHistoryCommandPath: vm => vm.GoToHistoryCommand,
            RevealCommandPath: vm => vm.RevealCommand,
            HardCommandPath: vm => vm.HardCommand,
            EasyCommandPath: vm => vm.EasyCommand,
            DailyGoalTextPath: vm => vm.DailyGoal.DailyGoalText,
            DailyProgressPath: vm => vm.DailyGoal.DailyProgress
        );

        var badges = PracticePageBuilder.CreateBadgeSet(
            PracticePageBuilder.BuildBadge<ViewModels.ConjugationPracticeViewModel>(
                vm => vm.TenseGlyph, vm => vm.TenseLabel, vm => vm.TenseBrush),
            PracticePageBuilder.BuildBadge<ViewModels.ConjugationPracticeViewModel>(
                vm => vm.PersonGlyph, vm => vm.PersonLabel, vm => vm.PersonBrush),
            PracticePageBuilder.BuildBadge<ViewModels.ConjugationPracticeViewModel>(
                vm => vm.NumberGlyph, vm => vm.NumberLabel, vm => vm.NumberBrush)
        );

        // No hint for conjugation
        return PracticePageBuilder.BuildPage(config, badges, hintElement: null, elements);
    }
}
