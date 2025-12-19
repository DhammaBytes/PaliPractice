using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.Controls;

namespace PaliPractice.Presentation.Practice;

public sealed partial class ConjugationPracticePage : Page
{
    public ConjugationPracticePage()
    {
        ConjugationPracticePageMarkup.DataContext<ConjugationPracticeViewModel>(this, (page, _) => page
            .NavigationCacheMode<ConjugationPracticePage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*,Auto,Auto")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.BuildWithHistory<ConjugationPracticeViewModel>(
                        "Conjugation Practice",
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
                                    .IsActive<ConjugationPracticeViewModel>(vm => vm.WordCard.IsLoading)
                                    .BoolToVisibility<ProgressRing, ConjugationPracticeViewModel>(vm => vm.WordCard.IsLoading)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .VerticalAlignment(VerticalAlignment.Center),

                                // Error message
                                new TextBlock()
                                    .Text<ConjugationPracticeViewModel>(vm => vm.WordCard.ErrorMessage)
                                    .StringToVisibility<TextBlock, ConjugationPracticeViewModel>(vm => vm.WordCard.ErrorMessage)
                                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                                    .TextAlignment(TextAlignment.Center)
                                    .HorizontalAlignment(HorizontalAlignment.Center),

                                // Word card (with rank, lemma, example carousel)
                                WordCard.Build<ConjugationPracticeViewModel>(
                                    cardPath: vm => vm.WordCard,
                                    carouselPath: vm => vm.ExampleCarousel,
                                    rankPrefix: "V"),

                                // Badge row: [Person] [Number] [Tense]
                                new StackPanel()
                                    .Orientation(Orientation.Horizontal)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .Spacing(12)
                                    .Children(
                                        // Person badge
                                        new Border()
                                            .CornerRadius(16)
                                            .Padding(12, 6)
                                            .Background<ConjugationPracticeViewModel>(vm => vm.PersonBrush)
                                            .Child(
                                                new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon()
                                                            .FontSize(14)
                                                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                            .GlyphWithVisibility<ConjugationPracticeViewModel>(vm => vm.PersonGlyph),
                                                        new TextBlock()
                                                            .FontSize(14)
                                                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                            .Text<ConjugationPracticeViewModel>(vm => vm.PersonLabel)
                                                    )
                                            ),

                                        // Number badge
                                        new Border()
                                            .CornerRadius(16)
                                            .Padding(12, 6)
                                            .Background<ConjugationPracticeViewModel>(vm => vm.NumberBrush)
                                            .Child(
                                                new StackPanel()
                                                    .Orientation(Orientation.Horizontal)
                                                    .Spacing(6)
                                                    .Children(
                                                        new FontIcon()
                                                            .FontSize(14)
                                                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                            .GlyphWithVisibility<ConjugationPracticeViewModel>(vm => vm.NumberGlyph),
                                                        new TextBlock()
                                                            .FontSize(14)
                                                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                            .Text<ConjugationPracticeViewModel>(vm => vm.NumberLabel)
                                                    )
                                            ),

                                        // Tense badge (no glyph)
                                        new Border()
                                            .CornerRadius(16)
                                            .Padding(12, 6)
                                            .Background<ConjugationPracticeViewModel>(vm => vm.TenseBrush)
                                            .Child(
                                                new TextBlock()
                                                    .FontSize(14)
                                                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                                    .Text<ConjugationPracticeViewModel>(vm => vm.TenseLabel)
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
                                    .BoolToVisibility<Border, ConjugationPracticeViewModel>(vm => vm.Flashcard.IsRevealed)
                                    .Child(
                                        new TextBlock()
                                            .FontSize(32)
                                            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                            .HorizontalAlignment(HorizontalAlignment.Center)
                                            .TextAlignment(TextAlignment.Center)
                                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
                                            .Text<ConjugationPracticeViewModel>(vm => vm.Flashcard.Answer)
                                    ),

                                // Translation display (visible only after reveal)
                                TranslationDisplay.Build<ConjugationPracticeViewModel>(
                                    carouselPath: vm => vm.ExampleCarousel,
                                    isRevealedPath: vm => vm.Flashcard.IsRevealed)
                            )
                    ),

                    // Row 2: Navigation (Reveal button or Easy/Hard buttons)
                    PracticeNavigation.BuildWithReveal<ConjugationPracticeViewModel>(
                        revealCommand: vm => vm.RevealCommand,
                        hardCommand: vm => vm.HardCommand,
                        easyCommand: vm => vm.EasyCommand,
                        isRevealedPath: vm => vm.Flashcard.IsRevealed)
                        .Grid(row: 2),

                    // Row 3: Daily goal bar
                    DailyGoalBar.Build<ConjugationPracticeViewModel>(
                            dailyGoalText: vm => vm.DailyGoal.DailyGoalText,
                            dailyProgress: vm => vm.DailyGoal.DailyProgress)
                        .Grid(row: 3)
                )
            )
        );
    }
}
