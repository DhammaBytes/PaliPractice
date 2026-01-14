using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.ViewModels;

namespace PaliPractice.Presentation.Practice;

public sealed partial class ConjugationPracticePage : Page
{
    ConjugationPracticeViewModel? _viewModel;

    public ConjugationPracticePage()
    {
        var elements = new ResponsiveElements();
        var heightClass = LayoutConstants.GetCurrentHeightClass();

        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;

        ConjugationPracticePageMarkup.DataContext<ConjugationPracticeViewModel>(this, (page, _) => page
            .NavigationCacheMode<ConjugationPracticePage>(NavigationCacheMode.Required)
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
            _viewModel.DailyGoalReached -= OnDailyGoalReached;
            _viewModel = null;
        }
    }

    void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_viewModel != null)
        {
            _viewModel.QueueExhausted -= OnQueueExhausted;
            _viewModel.DailyGoalReached -= OnDailyGoalReached;
        }

        if (args.NewValue is ConjugationPracticeViewModel vm)
        {
            _viewModel = vm;
            _viewModel.QueueExhausted += OnQueueExhausted;
            _viewModel.DailyGoalReached += OnDailyGoalReached;
        }
    }

    async void OnQueueExhausted(object? sender, EventArgs e)
    {
        // Pool completely exhausted - no due or new forms available
        var dialog = new ContentDialog
        {
            Title = "All forms practiced",
            Content = "Great work! You've practiced all available forms. Come back later when reviews are due, or adjust your settings to add more.",
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

    async void OnDailyGoalReached(object? sender, EventArgs e)
    {
        // Daily goal reached but more forms available
        var dialog = new ContentDialog
        {
            Title = "Daily goal reached!",
            Content = "Congratulations! You've completed your daily practice goal.",
            PrimaryButtonText = "Continue",
            SecondaryButtonText = "Exit",
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
            _viewModel?.ContinuePracticeCommand.Execute(null);
        else
            _viewModel?.GoBackCommand.Execute(null);
    }

    static Grid BuildPageLayout(ResponsiveElements elements, HeightClass heightClass)
    {
        var config = new PracticePageConfig<ViewModels.ConjugationPracticeViewModel>(
            Title: "Conjugation Practice",
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

        // Build the 4th (optional) voice badge - only visible for reflexive
        var voiceBadge = PracticePageBuilder.BuildBadge<ViewModels.ConjugationPracticeViewModel>(heightClass,
            vm => vm.VoiceIconPath, vm => vm.VoiceLabel, vm => vm.VoiceColor);
        voiceBadge.badge.BoolToVisibility<SquircleBorder, ViewModels.ConjugationPracticeViewModel>(
            vm => vm.IsReflexive);

        var badges = PracticePageBuilder.CreateBadgeSet(heightClass,
            PracticePageBuilder.BuildBadge<ViewModels.ConjugationPracticeViewModel>(heightClass,
                vm => vm.TenseIconPath, vm => vm.TenseLabel, vm => vm.TenseColor),
            PracticePageBuilder.BuildBadge<ViewModels.ConjugationPracticeViewModel>(heightClass,
                vm => vm.PersonIconPath, vm => vm.PersonLabel, vm => vm.PersonColor),
            PracticePageBuilder.BuildBadge<ViewModels.ConjugationPracticeViewModel>(heightClass,
                vm => vm.NumberIconPath, vm => vm.NumberLabel, vm => vm.NumberColor),
            voiceBadge
        );

        // No hint for conjugation
        return PracticePageBuilder.BuildPage(config, badges, hintElement: null, elements, heightClass);
    }
}
