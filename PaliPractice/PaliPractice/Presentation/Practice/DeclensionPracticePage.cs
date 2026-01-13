using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.ViewModels;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Practice;

public sealed partial class DeclensionPracticePage : Page
{
    DeclensionPracticeViewModel? _viewModel;

    public DeclensionPracticePage()
    {
        var elements = new ResponsiveElements();
        var heightClass = LayoutConstants.GetCurrentHeightClass();

        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;

        DeclensionPracticePageMarkup.DataContext<DeclensionPracticeViewModel>(this, (page, _) => page
            .NavigationCacheMode<DeclensionPracticePage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(BuildPageLayout(elements, heightClass))
        );

        HeightResponsiveHelper.AttachResponsiveHandler(
            hc => PracticePageBuilder.ApplyResponsiveValues(elements, hc));
    }

    void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.QueueExhausted -= OnQueueExhausted;
            _viewModel = null;
        }
    }

    void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_viewModel != null)
            _viewModel.QueueExhausted -= OnQueueExhausted;

        if (args.NewValue is DeclensionPracticeViewModel vm)
        {
            _viewModel = vm;
            _viewModel.QueueExhausted += OnQueueExhausted;
        }
    }

    async void OnQueueExhausted(object? sender, EventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "All forms completed",
            Content = "You've practiced all available forms. Adjust your settings to add more, or come back later when reviews are due.",
            PrimaryButtonText = "Exit",
            SecondaryButtonText = "Settings",
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
            _viewModel?.GoToSettingsCommand.Execute(null);
        else
            _viewModel?.GoBackCommand.Execute(null);
    }

    static Grid BuildPageLayout(ResponsiveElements elements, HeightClass heightClass)
    {
        var config = new PracticePageConfig<ViewModels.DeclensionPracticeViewModel>(
            Title: "Declension Practice",
            FlashCardPath: vm => vm.FlashCard,
            AnswerStemPath: vm => vm.FlashCard.AnswerStem,
            AnswerEndingPath: vm => vm.FlashCard.AnswerEnding,
            AlternativeFormsPath: vm => vm.AlternativeForms,
            IsRevealedPath: vm => vm.FlashCard.IsRevealed,
            CarouselPath: vm => vm.ExampleCarousel,
            GoBackCommandPath: vm => vm.GoBackCommand,
            GoToHistoryCommandPath: vm => vm.GoToHistoryCommand,
            GoToInflectionTableCommandPath: vm => vm.GoToInflectionTableCommand,
            RevealCommandPath: vm => vm.RevealCommand,
            HardCommandPath: vm => vm.HardCommand,
            EasyCommandPath: vm => vm.EasyCommand,
            DailyGoalTextPath: vm => vm.DailyGoal.DailyGoalText,
            DailyProgressPath: vm => vm.DailyGoal.DailyProgress
        );

        var badges = PracticePageBuilder.CreateBadgeSet(heightClass,
            PracticePageBuilder.BuildBadge<ViewModels.DeclensionPracticeViewModel>(heightClass,
                vm => vm.CaseIconPath, vm => vm.CaseLabel, vm => vm.CaseColor),
            PracticePageBuilder.BuildBadge<ViewModels.DeclensionPracticeViewModel>(heightClass,
                vm => vm.GenderIconPath, vm => vm.GenderLabel, vm => vm.GenderColor),
            PracticePageBuilder.BuildBadge<ViewModels.DeclensionPracticeViewModel>(heightClass,
                vm => vm.NumberIconPath, vm => vm.NumberLabel, vm => vm.NumberColor)
        );

        var fonts = LayoutConstants.PracticeFontSizes.Get(heightClass);
        var hint = RegularText()
            .Text<ViewModels.DeclensionPracticeViewModel>(vm => vm.CaseHint)
            .FontSize(fonts.BadgeHint)
            .FontStyle(Windows.UI.Text.FontStyle.Italic)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .Margin(0, -4, 0, 0); // Reduce top spacing by ~30%

        // Track hint for responsive sizing
        elements.BadgeHintTextBlock = hint;

        return PracticePageBuilder.BuildPage(config, badges, hint, elements, heightClass);
    }
}
