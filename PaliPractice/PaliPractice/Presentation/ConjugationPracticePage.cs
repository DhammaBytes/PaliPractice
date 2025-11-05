using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Controls;
using PaliPractice.Presentation.ViewModels;

namespace PaliPractice.Presentation;

public sealed partial class ConjugationPracticePage : Page
{
    public ConjugationPracticePage()
    {
        this.DataContext<ConjugationPracticeViewModel>((page, _) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,Auto,*,Auto,Auto")
                .Children(
                    AppTitleBar.Build<ConjugationPracticeViewModel>("Conjugation Practice", vm => vm.GoBackCommand),
                    new ScrollViewer().Grid(row: 1).Content(
                        new Grid()
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .VerticalAlignment(VerticalAlignment.Center)
                            .Children(
                                new Grid().MaxWidth(600).Children(
                                    new StackPanel()
                                        .Padding(20)
                                        .Spacing(24)
                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                        .Children(
                                            new ProgressRing()
                                                .IsActive<ConjugationPracticeViewModel>(vm => vm.Card.IsLoading)
                                                .BoolToVisibility<ProgressRing, ConjugationPracticeViewModel>(vm => vm.Card.IsLoading)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .VerticalAlignment(VerticalAlignment.Center),
                                            new TextBlock()
                                                .Text<ConjugationPracticeViewModel>(vm => vm.Card.ErrorMessage)
                                                .StringToVisibility<TextBlock, ConjugationPracticeViewModel>(vm => vm.Card.ErrorMessage)
                                                .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                                .TextAlignment(TextAlignment.Center)
                                                .HorizontalAlignment(HorizontalAlignment.Center),
                                            WordCard.Build<ConjugationPracticeViewModel>(
                                                cardPath: vm => vm.Card,
                                                rankPrefix: "V")
                                        )
                                )
                            )
                    ),
                    new StackPanel()
                        .Grid(row: 2)
                        .Padding(20)
                        .Spacing(16)
                        .Children(
                            EnumToggleGroup.Build<ConjugationPracticeViewModel, Number>(
                                [
                                    new EnumOption<Number>(Number.Singular, "Singular"),
                                    new EnumOption<Number>(Number.Plural, "Plural")
                                ],
                                vm => vm.Number),
                            EnumToggleGroup.Build<ConjugationPracticeViewModel, Person>(
                                [
                                    new EnumOption<Person>(Person.First, "1st"),
                                    new EnumOption<Person>(Person.Second, "2nd"),
                                    new EnumOption<Person>(Person.Third, "3rd")
                                ],
                                vm => vm.Person),
                            EnumToggleGroup.Build<ConjugationPracticeViewModel, Voice>(
                                [
                                    new EnumOption<Voice>(Voice.Normal, "Active"),
                                    new EnumOption<Voice>(Voice.Reflexive, "Reflexive")
                                ],
                                vm => vm.Voice),
                            EnumToggleGroup.BuildWithLayout<ConjugationPracticeViewModel, Tense>(
                                [
                                    new EnumOption<Tense>(Tense.Present, "Present"),
                                    new EnumOption<Tense>(Tense.Imperative, "Imper."),
                                    new EnumOption<Tense>(Tense.Aorist, "Aorist"),
                                    new EnumOption<Tense>(Tense.Optative, "Opt."),
                                    new EnumOption<Tense>(Tense.Future, "Future")
                                ],
                                vm => vm.Tense,
                                [3, 2])
                        ),
                    CardNavigation.Build<ConjugationPracticeViewModel>(
                        hardCommand: vm => vm.HardCommand,
                        easyCommand: vm => vm.EasyCommand)
                        .Grid(row: 3),
                    DailyGoalBar.Build<ConjugationPracticeViewModel>(
                            dailyGoalText: vm => vm.Card.DailyGoalText,
                            dailyProgress: vm => vm.Card.DailyProgress)
                        .Grid(row: 4)
                )
            )
        );
    }
}
