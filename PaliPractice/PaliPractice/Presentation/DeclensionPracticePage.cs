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
                .RowDefinitions("Auto,*,Auto,Auto")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.BuildWithHistory<DeclensionPracticeViewModel>(
                        "Declension Practice",
                        vm => vm.GoBackCommand,
                        vm => vm.GoToHistoryCommand),

                    // Row 1: Scrollable content area
                    new ScrollViewer().Grid(row: 1).Content(
                        new StackPanel()
                            .Padding(20)
                            .Spacing(16)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .MaxWidth(600)
                            .Children(
                                // Loading indicator
                                new ProgressRing()
                                    .IsActive<DeclensionPracticeViewModel>(vm => vm.Card.IsLoading)
                                    .BoolToVisibility<ProgressRing, DeclensionPracticeViewModel>(vm => vm.Card.IsLoading)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center),

                                // Error message
                                new TextBlock()
                                    .Text<DeclensionPracticeViewModel>(vm => vm.Card.ErrorMessage)
                                    .StringToVisibility<TextBlock, DeclensionPracticeViewModel>(vm => vm.Card.ErrorMessage)
                                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                    .TextAlignment(TextAlignment.Center)
                                    .HorizontalAlignment(HorizontalAlignment.Center),

                                // Word card (with translation, dictionary form, example)
                                WordCard.Build<DeclensionPracticeViewModel>(
                                    cardPath: vm => vm.Card,
                                    rankPrefix: "N"),

                                // Badge row: [Gender] [Number] [Case]
                                new StackPanel()
                                    .Orientation(Orientation.Horizontal)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .Spacing(12)
                                    .Children(
                                        // Gender badge
                                        new Border()
                                            .CornerRadius(16)
                                            .Padding(12, 6)
                                            .Background<DeclensionPracticeViewModel>(vm => vm.GenderBrush)
                                            .Child(
                                                new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon()
                                                            .FontSize(14)
                                                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                            .GlyphWithVisibility<DeclensionPracticeViewModel>(vm => vm.GenderGlyph),
                                                        new TextBlock()
                                                            .FontSize(14)
                                                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                            .Text<DeclensionPracticeViewModel>(vm => vm.GenderLabel)
                                                    )
                                            ),

                                        // Number badge
                                        new Border()
                                            .CornerRadius(16)
                                            .Padding(12, 6)
                                            .Background<DeclensionPracticeViewModel>(vm => vm.NumberBrush)
                                            .Child(
                                                new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon()
                                                            .FontSize(14)
                                                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                            .GlyphWithVisibility<DeclensionPracticeViewModel>(vm => vm.NumberGlyph),
                                                        new TextBlock()
                                                            .FontSize(14)
                                                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                            .Text<DeclensionPracticeViewModel>(vm => vm.NumberLabel)
                                                    )
                                            ),

                                        // Case badge (no glyph)
                                        new Border()
                                            .CornerRadius(16)
                                            .Padding(12, 6)
                                            .Background<DeclensionPracticeViewModel>(vm => vm.CaseBrush)
                                            .Child(
                                                new TextBlock()
                                                    .FontSize(14)
                                                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                    .Text<DeclensionPracticeViewModel>(vm => vm.CaseLabel)
                                            )
                                    ),

                                // Answer display (visible only after reveal)
                                new Border()
                                    .MinHeight(60)
                                    .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                                    .CornerRadius(8)
                                    .Padding(16, 12)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .MinWidth(200)
                                    .BoolToVisibility<Border, DeclensionPracticeViewModel>(vm => vm.Flashcard.IsRevealed)
                                    .Child(
                                        new TextBlock()
                                            .FontSize(32)
                                            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .TextAlignment(TextAlignment.Center)
                                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                                            .Text<DeclensionPracticeViewModel>(vm => vm.Flashcard.Answer)
                                    )
                            )
                    ),

                    // Row 2: Navigation (Reveal button or Easy/Hard buttons)
                    CardNavigation.BuildWithReveal<DeclensionPracticeViewModel>(
                        revealCommand: vm => vm.RevealCommand,
                        hardCommand: vm => vm.HardCommand,
                        easyCommand: vm => vm.EasyCommand,
                        isRevealedPath: vm => vm.Flashcard.IsRevealed)
                        .Grid(row: 2),

                    // Row 3: Daily goal bar
                    DailyGoalBar.Build<DeclensionPracticeViewModel>(
                            dailyGoalText: vm => vm.DailyGoal.DailyGoalText,
                            dailyProgress: vm => vm.DailyGoal.DailyProgress)
                        .Grid(row: 3)
                )
            )
        );
    }
}
