using PaliPractice.Presentation.Components;
using PaliPractice.Presentation.Components.Selectors;

namespace PaliPractice.Presentation;

public sealed partial class DeclensionPracticePage : Page
{
    public DeclensionPracticePage()
    {
        this.DataContext<DeclensionPracticeViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(Theme.Brushes.Background.Default)
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,Auto,*,Auto,Auto")
                .Children(
                    AppTitleBar.Build("Declension Practice", btn => btn.Command(() => vm.Nav.GoBackCommand)),
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
                                    rankPrefix: "N")
                                )
                            )
                        )),
                    new StackPanel().Grid(row:3).Padding(20).Spacing(16)
                        .Visibility(() => vm.Card.IsLoading, l => !l ? Visibility.Visible : Visibility.Collapsed)
                        .Children(
                            NumberSelector.Build(
                                bindSingular: btn => btn.IsChecked(x => x.Binding(() => vm.Number.IsSingularSelected).TwoWay()).Command(() => vm.Number.SelectSingularCommand),
                                bindPlural: btn => btn.IsChecked(x => x.Binding(() => vm.Number.IsPluralSelected).TwoWay()).Command(() => vm.Number.SelectPluralCommand)
                            ),
                            GenderSelector.Build(
                                bindMasculine: btn => btn.IsChecked(x => x.Binding(() => vm.Gender.IsMasculineSelected).TwoWay()).Command(() => vm.Gender.SelectMasculineCommand),
                                bindNeuter: btn => btn.IsChecked(x => x.Binding(() => vm.Gender.IsNeuterSelected).TwoWay()).Command(() => vm.Gender.SelectNeuterCommand),
                                bindFeminine: btn => btn.IsChecked(x => x.Binding(() => vm.Gender.IsFeminineSelected).TwoWay()).Command(() => vm.Gender.SelectFeminineCommand)
                            ),
                            CaseSelector.Build(
                                bindNominative: btn => btn.IsChecked(x => x.Binding(() => vm.Cases.IsNominativeSelected).TwoWay()).Command(() => vm.Cases.SelectNominativeCommand),
                                bindAccusative: btn => btn.IsChecked(x => x.Binding(() => vm.Cases.IsAccusativeSelected).TwoWay()).Command(() => vm.Cases.SelectAccusativeCommand),
                                bindInstrumental: btn => btn.IsChecked(x => x.Binding(() => vm.Cases.IsInstrumentalSelected).TwoWay()).Command(() => vm.Cases.SelectInstrumentalCommand),
                                bindDative: btn => btn.IsChecked(x => x.Binding(() => vm.Cases.IsDativeSelected).TwoWay()).Command(() => vm.Cases.SelectDativeCommand),
                                bindAblative: btn => btn.IsChecked(x => x.Binding(() => vm.Cases.IsAblativeSelected).TwoWay()).Command(() => vm.Cases.SelectAblativeCommand),
                                bindGenitive: btn => btn.IsChecked(x => x.Binding(() => vm.Cases.IsGenitiveSelected).TwoWay()).Command(() => vm.Cases.SelectGenitiveCommand),
                                bindLocative: btn => btn.IsChecked(x => x.Binding(() => vm.Cases.IsLocativeSelected).TwoWay()).Command(() => vm.Cases.SelectLocativeCommand),
                                bindVocative: btn => btn.IsChecked(x => x.Binding(() => vm.Cases.IsVocativeSelected).TwoWay()).Command(() => vm.Cases.SelectVocativeCommand)
                            )
                        ),
                    CardNavigationSelector.Build(
                        bindPreviousCommand: btn => btn.Command(() => vm.CardNavigation.PreviousCommand),
                        bindNextCommand: btn => btn.Command(() => vm.CardNavigation.NextCommand))
                        .Grid(row:4)
                )
            )
        );
    }
}
