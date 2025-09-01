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
                                    new EnumOption<Gender>(Models.Gender.Masculine, "Masculine", "\uE71A", Color.FromArgb(255, 220, 255, 220)),
                                    new EnumOption<Gender>(Models.Gender.Neuter, "Neuter", "\uE734", Color.FromArgb(255, 220, 255, 220)),
                                    new EnumOption<Gender>(Models.Gender.Feminine, "Feminine", "\uE716", Color.FromArgb(255, 220, 255, 220))
                                },
                                () => vm.Gender),
                            EnumToggleGroup.Build(
                                new[] {
                                    new EnumOption<NounCase>(NounCase.Nominative, "Nom", null, Color.FromArgb(255, 255, 243, 224)),
                                    new EnumOption<NounCase>(NounCase.Accusative, "Acc", null, Color.FromArgb(255, 255, 243, 224)),
                                    new EnumOption<NounCase>(NounCase.Instrumental, "Ins", null, Color.FromArgb(255, 255, 243, 224)),
                                    new EnumOption<NounCase>(NounCase.Dative, "Dat", null, Color.FromArgb(255, 255, 243, 224)),
                                    new EnumOption<NounCase>(NounCase.Ablative, "Abl", null, Color.FromArgb(255, 255, 243, 224)),
                                    new EnumOption<NounCase>(NounCase.Genitive, "Gen", null, Color.FromArgb(255, 255, 243, 224)),
                                    new EnumOption<NounCase>(NounCase.Locative, "Loc", null, Color.FromArgb(255, 255, 243, 224)),
                                    new EnumOption<NounCase>(NounCase.Vocative, "Voc", null, Color.FromArgb(255, 255, 243, 224))
                                },
                                () => vm.Cases)
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
