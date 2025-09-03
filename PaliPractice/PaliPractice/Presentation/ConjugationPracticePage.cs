using PaliPractice.Presentation.Components;
using PaliPractice.Presentation.ViewModels;

namespace PaliPractice.Presentation;

public sealed partial class ConjugationPracticePage : Page
{
    public ConjugationPracticePage()
    {
        this.DataContext<ConjugationPracticeViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,Auto,*,Auto,Auto")
                .Children(
                    AppTitleBar.Build("Conjugation Practice", btn => btn.Command(() => vm.GoBackCommand)),
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
                                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush")).TextAlignment(TextAlignment.Center)
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
                        .Children(
                            EnumToggleGroup.Build(
                                [
                                    new EnumOption<Number>(Number.Singular, "Singular"),
                                    new EnumOption<Number>(Number.Plural, "Plural")
                                ],
                                () => vm.Number),
                            EnumToggleGroup.Build(
                                [
                                    new EnumOption<Person>(Person.First, "1st"),
                                    new EnumOption<Person>(Person.Second, "2nd"),
                                    new EnumOption<Person>(Person.Third, "3rd")
                                ],
                                () => vm.Person),
                            EnumToggleGroup.Build(
                                [
                                    new EnumOption<Voice>(Voice.Normal, "Active"),
                                    new EnumOption<Voice>(Voice.Reflexive, "Reflexive")
                                ],
                                () => vm.Voice),
                            EnumToggleGroup.BuildWithLayout(
                                [
                                    new EnumOption<Tense>(Tense.Present, "Present"),
                                    new EnumOption<Tense>(Tense.Imperative, "Imper."),
                                    new EnumOption<Tense>(Tense.Aorist, "Aorist"),
                                    new EnumOption<Tense>(Tense.Optative, "Opt."),
                                    new EnumOption<Tense>(Tense.Future, "Future")
                                ],
                                () => vm.Tense,
                                [3, 2])
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
