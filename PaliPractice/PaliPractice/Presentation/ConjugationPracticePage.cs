using PaliPractice.Presentation.Components;
using PaliPractice.Presentation.Components.Selectors;
using PaliPractice.Presentation.Shared.ViewModels;

namespace PaliPractice.Presentation;

public sealed partial class ConjugationPracticePage : Page
{
    public ConjugationPracticePage()
    {
        this.DataContext<ConjugationPracticeViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(Theme.Brushes.Background.Default)
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,Auto,*,Auto,Auto")
                .Children(
                    AppTitleBar.Build("Conjugation Practice", btn => btn.Command(() => vm.Nav.GoBackCommand)),
                    DailyGoalBar.Build(
                        bindDailyGoalText: tb => tb.Text(() => vm.Card.DailyGoalText),
                        bindDailyProgress: pb => pb.Value(() => vm.Card.DailyProgress)).Grid(row:1),
                    new ScrollViewer().Grid(row:2).Content(
                        new Grid().HorizontalAlignment(HorizontalAlignment.Center).VerticalAlignment(VerticalAlignment.Center).Children(
                            new Grid().MaxWidth(600).Children(
                                new StackPanel().Padding(20).Spacing(24).HorizontalAlignment(HorizontalAlignment.Stretch).Children(
                                new ProgressRing().IsActive(() => vm.Card.IsLoading)
                                    .Visibility(() => vm.Card.IsLoading, b => b ? Visibility.Visible : Visibility.Collapsed)
                                    .HorizontalAlignment(HorizontalAlignment.Center).VerticalAlignment(VerticalAlignment.Center),
                                new TextBlock().Text(() => vm.Card.ErrorMessage)
                                    .Visibility(() => vm.Card.ErrorMessage, e => !string.IsNullOrEmpty(e) ? Visibility.Visible : Visibility.Collapsed)
                                    .Foreground(Theme.Brushes.OnBackground.Medium).TextAlignment(TextAlignment.Center)
                                    .HorizontalAlignment(HorizontalAlignment.Center),
                                WordCard.Build(
                                    isLoading: () => vm.Card.IsLoading,
                                    bindRankText: tb => tb.Text(() => vm.Card.RankText),
                                    bindAnkiState: tb => tb.Text(() => vm.Card.AnkiState),
                                    bindCurrentWord: tb => tb.Text(() => vm.Card.CurrentWord),
                                    bindUsageExample: tb => tb.Text(() => vm.Card.UsageExample),
                                    bindSuttaReference: tb => tb.Text(() => vm.Card.SuttaReference),
                                    rankPrefix: "V")
                                )
                            )
                        )),
                    new StackPanel().Grid(row:3).Padding(20).Spacing(16)
                        .Visibility(() => vm.Card.IsLoading, l => !l ? Visibility.Visible : Visibility.Collapsed)
                        .Children(
                            NumberSelector.Build(
                                bindSingular: btn => btn.IsChecked(x => x.Binding(() => vm.Number.IsSingularSelected)).Command(() => vm.Number.SelectSingularCommand),
                                bindPlural: btn => btn.IsChecked(x => x.Binding(() => vm.Number.IsPluralSelected)).Command(() => vm.Number.SelectPluralCommand)
                            ),
                            PersonSelector.Build(
                                bindFirst: btn => btn.IsChecked(x => x.Binding(() => vm.Person.IsFirstPersonSelected)).Command(() => vm.Person.SelectFirstPersonCommand),
                                bindSecond: btn => btn.IsChecked(x => x.Binding(() => vm.Person.IsSecondPersonSelected)).Command(() => vm.Person.SelectSecondPersonCommand),
                                bindThird: btn => btn.IsChecked(x => x.Binding(() => vm.Person.IsThirdPersonSelected)).Command(() => vm.Person.SelectThirdPersonCommand)
                            ),
                            VoiceSelector.Build(
                                bindNormal: btn => btn.IsChecked(x => x.Binding(() => vm.Voice.IsNormalSelected)).Command(() => vm.Voice.SelectNormalCommand),
                                bindReflexive: btn => btn.IsChecked(x => x.Binding(() => vm.Voice.IsReflexiveSelected)).Command(() => vm.Voice.SelectReflexiveCommand)
                            ),
                            TenseSelector.Build(
                                bindPresent: btn => btn.IsChecked(x => x.Binding(() => vm.Tense.IsPresentSelected)).Command(() => vm.Tense.SelectPresentCommand),
                                bindImperative: btn => btn.IsChecked(x => x.Binding(() => vm.Tense.IsImperativeSelected)).Command(() => vm.Tense.SelectImperativeCommand),
                                bindAorist: btn => btn.IsChecked(x => x.Binding(() => vm.Tense.IsAoristSelected)).Command(() => vm.Tense.SelectAoristCommand),
                                bindOptative: btn => btn.IsChecked(x => x.Binding(() => vm.Tense.IsOptativeSelected)).Command(() => vm.Tense.SelectOptativeCommand),
                                bindFuture: btn => btn.IsChecked(x => x.Binding(() => vm.Tense.IsFutureSelected)).Command(() => vm.Tense.SelectFutureCommand)
                            )
                        ),
                    CardNavigationSelector.Build(
                        bindPreviousCommand: btn => btn.Command(() => vm.PreviousCommand),
                        bindNextCommand: btn => btn.Command(() => vm.NextCommand))
                        .Grid(row:4)
                )
            )
        );
    }
}
