using Microsoft.UI;
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
                .RowDefinitions("Auto,*,Auto,Auto")
                .Children(
                    AppTitleBar.Build("Declension Practice", btn => btn.Command(() => vm.Nav.GoBackCommand)),
                    new ScrollViewer().Grid(row:1).Content(
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
                    new StackPanel().Grid(row:2).Padding(20).Spacing(16)
                        .Visibility(() => vm.Card.IsLoading, l => !l ? Visibility.Visible : Visibility.Collapsed)
                        .Children(
                            NumberSelector.Build(() => vm.Number),
                            GenderSelector.Build(() => vm.Gender),
                            CaseSelector.Build(() => vm.Cases)
                        ),
                    DailyGoalBar.Build(
                        bindDailyGoalText: tb => tb.Text(() => vm.Card.DailyGoalText),
                        bindDailyProgress: pb => pb.Value(() => vm.Card.DailyProgress)).Grid(row:3)
                )
            )
        );
    }
}