using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.Controls;

namespace PaliPractice.Presentation.Practice;

public sealed partial class ConjugationPracticePage : Page
{
    public ConjugationPracticePage()
    {
        var elements = new ResponsiveElements();

        ConjugationPracticePageMarkup.DataContext<ConjugationPracticeViewModel>(this, (page, _) => page
            .NavigationCacheMode<ConjugationPracticePage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(BuildPageLayout(elements))
        );

        if (elements.ContentArea is not null)
        {
            HeightResponsiveHelper.AttachResponsiveHandler(elements.ContentArea, heightClass =>
                PracticeLayoutHelper.ApplyResponsiveValues(elements, heightClass));
        }
    }

    static Grid BuildPageLayout(ResponsiveElements elements)
    {
        var config = new PracticePageConfig<ConjugationPracticeViewModel>(
            Title: "Conjugation Practice",
            RankPrefix: "V",
            WordCardPath: vm => vm.WordCard,
            AnswerPath: vm => vm.Flashcard.Answer,
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

        var badges = PracticeLayoutHelper.CreateBadgeSet(
            PracticeLayoutHelper.BuildBadge<ConjugationPracticeViewModel>(
                vm => vm.TenseGlyph, vm => vm.TenseLabel, vm => vm.TenseBrush),
            PracticeLayoutHelper.BuildBadge<ConjugationPracticeViewModel>(
                vm => vm.PersonGlyph, vm => vm.PersonLabel, vm => vm.PersonBrush),
            PracticeLayoutHelper.BuildBadge<ConjugationPracticeViewModel>(
                vm => vm.NumberGlyph, vm => vm.NumberLabel, vm => vm.NumberBrush)
        );

        // No hint for conjugation
        return PracticeLayoutHelper.BuildPageLayout(config, badges, hintElement: null, elements);
    }
}
