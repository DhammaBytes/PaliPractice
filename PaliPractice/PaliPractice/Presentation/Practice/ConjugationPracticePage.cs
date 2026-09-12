using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.Common;
using PaliPractice.Presentation.Practice.ViewModels;
using PaliPractice.Localization;

namespace PaliPractice.Presentation.Practice;

public sealed partial class ConjugationPracticePage : Page
{
    ConjugationPracticeViewModel? _viewModel;
    CancellationTokenSource? _activation;

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _activation = new CancellationTokenSource();
        AttachViewModel(DataContext as ConjugationPracticeViewModel);
    }

    public ConjugationPracticePage()
    {
        var elements = new ResponsiveElements();
        var heightClass = LayoutConstants.GetCurrentHeightClass();

        DataContextChanged += OnDataContextChanged;

        ConjugationPracticePageMarkup.DataContext<ConjugationPracticeViewModel>(this, (page, _) => page
            .NavigationCacheMode<ConjugationPracticePage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(PageFadeIn.Wrap(page, BuildPageLayout(elements, heightClass)))
        );

        HeightResponsiveHelper.AttachResponsiveHandler(
            hc => PracticePageBuilder.ApplyResponsiveValues(elements, hc));
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _activation?.Cancel();
        _activation?.Dispose();
        _activation = null;
        AttachViewModel(null);
        base.OnNavigatedFrom(e);
    }

    void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (_activation != null)
            AttachViewModel(args.NewValue as ConjugationPracticeViewModel);
    }

    void AttachViewModel(ConjugationPracticeViewModel? vm)
    {
        if (_viewModel != null)
        {
            _viewModel.QueueExhausted -= OnQueueExhausted;
            _viewModel.DailyGoalReached -= OnDailyGoalReached;
        }
        _viewModel = vm;
        if (vm == null || _activation == null) return;

        vm.QueueExhausted += OnQueueExhausted;
        vm.DailyGoalReached += OnDailyGoalReached;
        var ct = _activation.Token;
        DispatcherQueue.TryEnqueue(async () =>
        {
            if (!ct.IsCancellationRequested && ReferenceEquals(_viewModel, vm))
                await vm.StartAsync(ct);
        });
    }

    async void OnQueueExhausted(object? sender, EventArgs e)
    {
        // Pool completely exhausted - no due or new forms available
        var dialog = new ContentDialog
        {
            Title = AppText.Get("Practice.Dialog.AllFormsPracticed.Title"),
            Content = AppText.Get("Practice.Dialog.AllFormsPracticed.Content"),
            PrimaryButtonText = AppText.Get("Common.Exit"),
            SecondaryButtonText = AppText.Get("Settings.Title"),
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
            Title = AppText.Get("Practice.Dialog.DailyGoalReached.Title"),
            Content = AppText.Get("Practice.Dialog.DailyGoalReached.Content"),
            PrimaryButtonText = AppText.Get("Common.Continue"),
            SecondaryButtonText = AppText.Get("Common.Exit"),
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
        var config = new PracticePageConfig<ConjugationPracticeViewModel>(
            Title: AppText.Get("Practice.Title.Conjugation"),
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
        var voiceBadge = PracticePageBuilder.BuildBadge<ConjugationPracticeViewModel>(heightClass,
            vm => vm.VoiceIconPath, vm => vm.VoiceLabel, vm => vm.VoiceColor);
        voiceBadge.badge.BoolToVisibility<SquircleBorder, ConjugationPracticeViewModel>(
            vm => vm.IsReflexive);

        var badges = PracticePageBuilder.CreateBadgeSet(heightClass,
            PracticePageBuilder.BuildBadge<ConjugationPracticeViewModel>(heightClass,
                vm => vm.TenseIconPath, vm => vm.TenseLabel, vm => vm.TenseColor),
            PracticePageBuilder.BuildBadge<ConjugationPracticeViewModel>(heightClass,
                vm => vm.PersonIconPath, vm => vm.PersonLabel, vm => vm.PersonColor),
            PracticePageBuilder.BuildBadge<ConjugationPracticeViewModel>(heightClass,
                vm => vm.NumberIconPath, vm => vm.NumberLabel, vm => vm.NumberColor),
            voiceBadge
        );

        // No hint for conjugation
        return PracticePageBuilder.BuildPage(config, badges, hintElement: null, elements, heightClass);
    }
}
