using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.Controls;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Practice;

public sealed partial class DeclensionPracticePage : Page
{
    public DeclensionPracticePage()
    {
        var elements = new ResponsiveElements();

        DeclensionPracticePageMarkup.DataContext<ViewModels.DeclensionPracticeViewModel>(this, (page, _) => page
            .NavigationCacheMode<DeclensionPracticePage>(NavigationCacheMode.Required)
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
        var config = new PracticePageConfig<ViewModels.DeclensionPracticeViewModel>(
            Title: "Declension Practice",
            RankPrefix: "N",
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
            PracticeLayoutHelper.BuildBadge<ViewModels.DeclensionPracticeViewModel>(
                vm => vm.CaseGlyph, vm => vm.CaseLabel, vm => vm.CaseBrush),
            PracticeLayoutHelper.BuildBadge<ViewModels.DeclensionPracticeViewModel>(
                vm => vm.GenderGlyph, vm => vm.GenderLabel, vm => vm.GenderBrush),
            PracticeLayoutHelper.BuildBadge<ViewModels.DeclensionPracticeViewModel>(
                vm => vm.NumberGlyph, vm => vm.NumberLabel, vm => vm.NumberBrush)
        );

        var hint = RegularText()
            .Text<ViewModels.DeclensionPracticeViewModel>(vm => vm.CaseHint)
            .FontSize(14)
            .FontStyle(Windows.UI.Text.FontStyle.Italic)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center);

        return PracticeLayoutHelper.BuildPageLayout(config, badges, hint, elements);
    }
}
