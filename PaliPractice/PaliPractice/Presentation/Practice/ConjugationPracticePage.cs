using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.Controls;

namespace PaliPractice.Presentation.Practice;

public sealed partial class ConjugationPracticePage : Page
{
    public ConjugationPracticePage()
    {
        // Build content with element references for responsive sizing
        var elements = new PracticeContentLayout.ResponsiveElements();
        var contentGrid = BuildPracticeContent(elements);
        var containerGrid = PracticeContentLayout.BuildContainer(contentGrid);

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

                    // Row 1: Fixed-height content area (no scrolling)
                    containerGrid.Grid(row: 1),

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

        // Attach height-responsive behavior after page is built
        PracticeContentLayout.AttachHeightResponsive(containerGrid, elements);
    }

    static Grid BuildPracticeContent(PracticeContentLayout.ResponsiveElements elements)
    {
        // Build WordCard with element references
        WordCard.Build<ConjugationPracticeViewModel>(
            cardPath: vm => vm.WordCard,
            carouselPath: vm => vm.ExampleCarousel,
            rankPrefix: "V",
            out var cardBorder,
            out var wordTextBlock);

        elements.CardBorder = cardBorder;
        elements.WordTextBlock = wordTextBlock;

        // Build answer TextBlock with reference
        var answerTextBlock = new TextBlock()
            .FontSize(32)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            .Text<ConjugationPracticeViewModel>(vm => vm.Flashcard.Answer);
        elements.AnswerTextBlock = answerTextBlock;

        // Build answer border with reference
        var answerBorder = new Border()
            .MinHeight(60)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .CornerRadius(8)
            .Padding(16, 12)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MinWidth(200)
            .BoolToVisibility<Border, ConjugationPracticeViewModel>(vm => vm.Flashcard.IsRevealed)
            .Child(answerTextBlock);
        elements.AnswerBorder = answerBorder;

        // Build badges with references
        var (badgesPanel, tenseBadge, tenseText, tenseIcon,
             personBadge, personText, personIcon,
             numberBadge, numberText, numberIcon) = BuildBadgesRow();

        elements.BadgesPanel = badgesPanel;
        elements.BadgeBorders.AddRange([tenseBadge, personBadge, numberBadge]);
        elements.BadgeTextBlocks.AddRange([tenseText, personText, numberText]);
        elements.BadgeIcons.AddRange([tenseIcon, personIcon, numberIcon]);

        // Build the content Grid with flexible spacers
        var contentGrid = new Grid()
            .RowDefinitions("Auto,*,Auto,Auto,*,Auto")
            .Padding(LayoutConstants.Spacing.ContentPaddingTall)
            .Children(
                // Row 0: Loading/Error indicators
                new StackPanel()
                    .Spacing(8)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Children(
                        new ProgressRing()
                            .IsActive<ConjugationPracticeViewModel>(vm => vm.WordCard.IsLoading)
                            .BoolToVisibility<ProgressRing, ConjugationPracticeViewModel>(vm => vm.WordCard.IsLoading)
                            .HorizontalAlignment(HorizontalAlignment.Center),
                        new TextBlock()
                            .Text<ConjugationPracticeViewModel>(vm => vm.WordCard.ErrorMessage)
                            .StringToVisibility<TextBlock, ConjugationPracticeViewModel>(vm => vm.WordCard.ErrorMessage)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .TextAlignment(TextAlignment.Center)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                    )
                    .Grid(row: 0),

                // Row 1: Flexible top spacer (compresses first)
                new Border().Grid(row: 1),

                // Row 2: WordCard
                cardBorder.Grid(row: 2),

                // Row 3: Badges (no hint for conjugation, just badges)
                badgesPanel.Grid(row: 3),

                // Row 4: Flexible bottom spacer (compresses)
                new Border().Grid(row: 4),

                // Row 5: Answer + Translation
                new StackPanel()
                    .Spacing(LayoutConstants.Spacing.SectionSpacingTall)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Children(
                        answerBorder,
                        TranslationDisplay.Build<ConjugationPracticeViewModel>(
                            carouselPath: vm => vm.ExampleCarousel,
                            isRevealedPath: vm => vm.Flashcard.IsRevealed)
                    )
                    .Grid(row: 5)
            );

        elements.ContentGrid = contentGrid;
        return contentGrid;
    }

    static (StackPanel panel,
            Border tenseBadge, TextBlock tenseText, FontIcon tenseIcon,
            Border personBadge, TextBlock personText, FontIcon personIcon,
            Border numberBadge, TextBlock numberText, FontIcon numberIcon) BuildBadgesRow()
    {
        // Tense badge (first)
        var tenseIcon = new FontIcon()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .GlyphWithVisibility<ConjugationPracticeViewModel>(vm => vm.TenseGlyph);
        var tenseText = new TextBlock()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Text<ConjugationPracticeViewModel>(vm => vm.TenseLabel);
        var tenseBadge = new Border()
            .CornerRadius(16)
            .Padding(12, 6)
            .Background<ConjugationPracticeViewModel>(vm => vm.TenseBrush)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(tenseIcon, tenseText));

        // Person badge
        var personIcon = new FontIcon()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .GlyphWithVisibility<ConjugationPracticeViewModel>(vm => vm.PersonGlyph);
        var personText = new TextBlock()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Text<ConjugationPracticeViewModel>(vm => vm.PersonLabel);
        var personBadge = new Border()
            .CornerRadius(16)
            .Padding(12, 6)
            .Background<ConjugationPracticeViewModel>(vm => vm.PersonBrush)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(personIcon, personText));

        // Number badge
        var numberIcon = new FontIcon()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .GlyphWithVisibility<ConjugationPracticeViewModel>(vm => vm.NumberGlyph);
        var numberText = new TextBlock()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Text<ConjugationPracticeViewModel>(vm => vm.NumberLabel);
        var numberBadge = new Border()
            .CornerRadius(16)
            .Padding(12, 6)
            .Background<ConjugationPracticeViewModel>(vm => vm.NumberBrush)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(numberIcon, numberText));

        var panel = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(LayoutConstants.Spacing.BadgeSpacingTall)
            .Children(tenseBadge, personBadge, numberBadge);

        return (panel, tenseBadge, tenseText, tenseIcon, personBadge, personText, personIcon, numberBadge, numberText, numberIcon);
    }
}
