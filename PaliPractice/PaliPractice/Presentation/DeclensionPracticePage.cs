using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Controls;
using PaliPractice.Presentation.ViewModels;

namespace PaliPractice.Presentation;

public sealed partial class DeclensionPracticePage : Page
{
    public DeclensionPracticePage()
    {
        this.DataContext<DeclensionPracticeViewModel>((page, _) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,Auto,*,Auto,Auto")
                .Children(
                    AppTitleBar.Build<DeclensionPracticeViewModel>("Declension Practice", vm => vm.GoBackCommand),
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
                                                .IsActive<DeclensionPracticeViewModel>(vm => vm.Card.IsLoading)
                                                .BoolToVisibility<ProgressRing, DeclensionPracticeViewModel>(vm => vm.Card.IsLoading)
                                                .HorizontalAlignment(HorizontalAlignment.Center)
                                                .VerticalAlignment(VerticalAlignment.Center),
                                            new TextBlock()
                                                .Text<DeclensionPracticeViewModel>(vm => vm.Card.ErrorMessage)
                                                .StringToVisibility<TextBlock, DeclensionPracticeViewModel>(vm => vm.Card.ErrorMessage)
                                                .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                                .TextAlignment(TextAlignment.Center)
                                                .HorizontalAlignment(HorizontalAlignment.Center),
                                            WordCard.Build<DeclensionPracticeViewModel>(
                                                cardPath: vm => vm.Card,
                                                rankPrefix: "N")
                                        )
                                )
                            )
                    ),
                    new StackPanel()
                        .Grid(row: 2)
                        .Padding(20)
                        .Spacing(16)
                        .Children(
                            EnumToggleGroup.Build<DeclensionPracticeViewModel, Number>(
                                [
                                    new EnumOption<Number>(Number.Singular, "Singular"),
                                    new EnumOption<Number>(Number.Plural, "Plural")
                                ],
                                vm => vm.Number),
                            EnumToggleGroup.Build<DeclensionPracticeViewModel, Gender>(
                                [
                                    new EnumOption<Gender>(Gender.Masculine, "Masculine"),
                                    new EnumOption<Gender>(Gender.Neuter, "Neuter"),
                                    new EnumOption<Gender>(Gender.Feminine, "Feminine")
                                ],
                                vm => vm.Gender),
                            EnumToggleGroup.BuildWithLayout<DeclensionPracticeViewModel, NounCase>(
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
                                vm => vm.Cases,
                                [4, 4])
                        ),
                    CardNavigation.Build<DeclensionPracticeViewModel>(
                        hardCommand: vm => vm.HardCommand,
                        easyCommand: vm => vm.EasyCommand)
                        .Grid(row: 3),
                    DailyGoalBar.Build<DeclensionPracticeViewModel>(
                            dailyGoalText: vm => vm.Card.DailyGoalText,
                            dailyProgress: vm => vm.Card.DailyProgress)
                        .Grid(row: 4)
                )
            )
        );
    }
}
