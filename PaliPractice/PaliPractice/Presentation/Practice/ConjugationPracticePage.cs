using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Practice;

public sealed partial class ConjugationPracticePage : Page
{
    public ConjugationPracticePage()
    {
        var elements = new ResponsiveElements();
        var heightClass = LayoutConstants.GetCurrentHeightClass();

        ConjugationPracticePageMarkup.DataContext<ViewModels.ConjugationPracticeViewModel>(this, (page, _) => page
            .NavigationCacheMode<ConjugationPracticePage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(BuildPageLayout(elements, heightClass))
        );

        HeightResponsiveHelper.AttachResponsiveHandler(
            hc => PracticePageBuilder.ApplyResponsiveValues(elements, hc));
    }

    static Grid BuildPageLayout(ResponsiveElements elements, HeightClass heightClass)
    {
        var config = new PracticePageConfig<ViewModels.ConjugationPracticeViewModel>(
            Title: "Conjugation Practice",
            RankPrefix: "V",
            FlashCardPath: vm => vm.FlashCard,
            AnswerStemPath: vm => vm.FlashCard.AnswerStem,
            AnswerEndingPath: vm => vm.FlashCard.AnswerEnding,
            AlternativeFormsPath: vm => vm.AlternativeForms,
            IsRevealedPath: vm => vm.FlashCard.IsRevealed,
            CarouselPath: vm => vm.ExampleCarousel,
            GoBackCommandPath: vm => vm.GoBackCommand,
            GoToHistoryCommandPath: vm => vm.GoToHistoryCommand,
            RevealCommandPath: vm => vm.RevealCommand,
            HardCommandPath: vm => vm.HardCommand,
            EasyCommandPath: vm => vm.EasyCommand,
            DailyGoalTextPath: vm => vm.DailyGoal.DailyGoalText,
            DailyProgressPath: vm => vm.DailyGoal.DailyProgress
        );

        var badges = PracticePageBuilder.CreateBadgeSet(heightClass,
            PracticePageBuilder.BuildBadge<ViewModels.ConjugationPracticeViewModel>(heightClass,
                vm => vm.TenseGlyph, vm => vm.TenseLabel, vm => vm.TenseColor),
            PracticePageBuilder.BuildBadge<ViewModels.ConjugationPracticeViewModel>(heightClass,
                vm => vm.PersonGlyph, vm => vm.PersonLabel, vm => vm.PersonColor),
            PracticePageBuilder.BuildBadge<ViewModels.ConjugationPracticeViewModel>(heightClass,
                vm => vm.NumberGlyph, vm => vm.NumberLabel, vm => vm.NumberColor)
        );

        // No hint for conjugation
        return PracticePageBuilder.BuildPage(config, badges, hintElement: null, elements, heightClass);
    }
}
