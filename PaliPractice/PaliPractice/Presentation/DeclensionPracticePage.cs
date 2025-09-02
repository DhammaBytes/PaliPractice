using PaliPractice.Presentation.Components;
using PaliPractice.Presentation.ViewModels;

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
                    AppTitleBar.Build("Declension Practice", btn => btn.Command(() => vm.GoBackCommand)),
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
                        .Children(
                            EnumToggleGroup.Build(
                                [
                                    new EnumOption<Number>(Number.Singular, "Singular"),
                                    new EnumOption<Number>(Number.Plural, "Plural")
                                ],
                                () => vm.Number),
                            EnumToggleGroup.Build(
                                [
                                    new EnumOption<Gender>(Gender.Masculine, "Masculine"),
                                    new EnumOption<Gender>(Gender.Neuter, "Neuter"),
                                    new EnumOption<Gender>(Gender.Feminine, "Feminine")
                                ],
                                () => vm.Gender),
                            EnumToggleGroup.BuildWithLayout(
                                [
                                    new EnumOption<NounCase>(NounCase.Nominative, "Nom"),
                                    new EnumOption<NounCase>(NounCase.Accusative, "Acc"),
                                    new EnumOption<NounCase>(NounCase.Instrumental, "Ins"),
                                    new EnumOption<NounCase>(NounCase.Dative, "Dat"),
                                    new EnumOption<NounCase>(NounCase.Ablative, "Abl"),
                                    new EnumOption<NounCase>(NounCase.Genitive, "Gen"),
                                    new EnumOption<NounCase>(NounCase.Locative, "Loc"),
                                    new EnumOption<NounCase>(NounCase.Vocative, "Voc")
                                ],
                                () => vm.Cases,
                                [4, 4])
                        ),
                    CardNavigation.Build(
                        bindPreviousCommand: btn => btn.Command(() => vm.PreviousCommand),
                        bindNextCommand: btn => btn.Command(() => vm.NextCommand))
                        .Grid(row:4)
                )
            )
        );
    }
}
