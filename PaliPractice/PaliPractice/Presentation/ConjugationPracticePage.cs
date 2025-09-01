using PaliPractice.Presentation.Components;
using PaliPractice.Presentation.ViewModels;

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
                            EnumToggleGroup.Build(
                                new[] {
                                    new EnumOption<Number>(Models.Number.Singular, "Singular", "\uE77B", Color.FromArgb(255, 230, 230, 255)),
                                    new EnumOption<Number>(Models.Number.Plural, "Plural", "\uE716", Color.FromArgb(255, 230, 230, 255))
                                },
                                () => vm.Number),
                            EnumToggleGroup.Build(
                                new[] {
                                    new EnumOption<Person>(Models.Person.First, "1st", "\uE77B", Color.FromArgb(255, 230, 255, 230)),
                                    new EnumOption<Person>(Models.Person.Second, "2nd", "\uE748", Color.FromArgb(255, 230, 255, 230)),
                                    new EnumOption<Person>(Models.Person.Third, "3rd", "\uE716", Color.FromArgb(255, 230, 255, 230))
                                },
                                () => vm.Person),
                            EnumToggleGroup.Build(
                                new[] {
                                    new EnumOption<Voice>(Models.Voice.Normal, "Active", "\uE768", Color.FromArgb(255, 255, 230, 230)),
                                    new EnumOption<Voice>(Models.Voice.Reflexive, "Reflexive", "\uE74C", Color.FromArgb(255, 255, 230, 230))
                                },
                                () => vm.Voice),
                            EnumToggleGroup.Build(
                                new[] {
                                    new EnumOption<Tense>(Models.Tense.Present, "Present", null, Color.FromArgb(255, 240, 240, 255)),
                                    new EnumOption<Tense>(Models.Tense.Imperative, "Imper.", null, Color.FromArgb(255, 240, 240, 255)),
                                    new EnumOption<Tense>(Models.Tense.Aorist, "Aorist", null, Color.FromArgb(255, 240, 240, 255)),
                                    new EnumOption<Tense>(Models.Tense.Optative, "Opt.", null, Color.FromArgb(255, 240, 240, 255)),
                                    new EnumOption<Tense>(Models.Tense.Future, "Future", null, Color.FromArgb(255, 240, 240, 255))
                                },
                                () => vm.Tense)
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
