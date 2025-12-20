using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.Controls;

namespace PaliPractice.Presentation.Practice;

public sealed partial class DeclensionPracticePage : Page
{
    public DeclensionPracticePage()
    {
        // Build content with element references for responsive sizing
        var elements = new PracticeContentLayout.ResponsiveElements();
        var contentGrid = BuildPracticeContent(elements);
        var containerGrid = PracticeContentLayout.BuildContainer(contentGrid);

        DeclensionPracticePageMarkup.DataContext<DeclensionPracticeViewModel>(this, (page, _) => page
            .NavigationCacheMode<DeclensionPracticePage>(NavigationCacheMode.Required)
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

                    // Row 1: Fixed-height content area (no scrolling)
                    containerGrid.Grid(row: 1),

                    // Row 2: Navigation (Reveal button or Easy/Hard buttons)
                    PracticeNavigation.BuildWithReveal<DeclensionPracticeViewModel>(
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

        // Attach height-responsive behavior after page is built
        PracticeContentLayout.AttachHeightResponsive(containerGrid, elements);
    }

    static Grid BuildPracticeContent(PracticeContentLayout.ResponsiveElements elements)
    {
        // Build WordCard with element references
        WordCard.Build<DeclensionPracticeViewModel>(
            cardPath: vm => vm.WordCard,
            carouselPath: vm => vm.ExampleCarousel,
            rankPrefix: "N",
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
            .Text<DeclensionPracticeViewModel>(vm => vm.Flashcard.Answer);
        elements.AnswerTextBlock = answerTextBlock;

        // Build answer border with reference
        var answerBorder = new Border()
            .MinHeight(60)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .CornerRadius(8)
            .Padding(16, 12)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .MinWidth(200)
            .BoolToVisibility<Border, DeclensionPracticeViewModel>(vm => vm.Flashcard.IsRevealed)
            .Child(answerTextBlock);
        elements.AnswerBorder = answerBorder;

        // Build badges with references
        var (badgesPanel, caseBadge, caseText, caseIcon,
             genderBadge, genderText, genderIcon,
             numberBadge, numberText, numberIcon) = BuildBadgesRow();

        elements.BadgesPanel = badgesPanel;
        elements.BadgeBorders.AddRange([caseBadge, genderBadge, numberBadge]);
        elements.BadgeTextBlocks.AddRange([caseText, genderText, numberText]);
        elements.BadgeIcons.AddRange([caseIcon, genderIcon, numberIcon]);

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
                            .IsActive<DeclensionPracticeViewModel>(vm => vm.WordCard.IsLoading)
                            .BoolToVisibility<ProgressRing, DeclensionPracticeViewModel>(vm => vm.WordCard.IsLoading)
                            .HorizontalAlignment(HorizontalAlignment.Center),
                        new TextBlock()
                            .Text<DeclensionPracticeViewModel>(vm => vm.WordCard.ErrorMessage)
                            .StringToVisibility<TextBlock, DeclensionPracticeViewModel>(vm => vm.WordCard.ErrorMessage)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .TextAlignment(TextAlignment.Center)
                            .HorizontalAlignment(HorizontalAlignment.Center)
                    )
                    .Grid(row: 0),

                // Row 1: Flexible top spacer (compresses first)
                new Border().Grid(row: 1),

                // Row 2: WordCard
                cardBorder.Grid(row: 2),

                // Row 3: Badges + Case hint
                new StackPanel()
                    .Spacing(LayoutConstants.Spacing.SectionSpacingTall)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Children(
                        badgesPanel,
                        // Case hint (e.g., "to whom? to what?")
                        new TextBlock()
                            .Text<DeclensionPracticeViewModel>(vm => vm.CaseHint)
                            .FontSize(14)
                            .FontStyle(Windows.UI.Text.FontStyle.Italic)
                            .Foreground(ThemeResource.Get<Brush>("OnBackgroundMediumBrush"))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .TextAlignment(TextAlignment.Center)
                    )
                    .Grid(row: 3),

                // Row 4: Flexible bottom spacer (compresses)
                new Border().Grid(row: 4),

                // Row 5: Answer + Translation
                new StackPanel()
                    .Spacing(LayoutConstants.Spacing.SectionSpacingTall)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Children(
                        answerBorder,
                        TranslationDisplay.Build<DeclensionPracticeViewModel>(
                            carouselPath: vm => vm.ExampleCarousel,
                            isRevealedPath: vm => vm.Flashcard.IsRevealed)
                    )
                    .Grid(row: 5)
            );

        elements.ContentGrid = contentGrid;
        return contentGrid;
    }

    static (StackPanel panel,
            Border caseBadge, TextBlock caseText, FontIcon caseIcon,
            Border genderBadge, TextBlock genderText, FontIcon genderIcon,
            Border numberBadge, TextBlock numberText, FontIcon numberIcon) BuildBadgesRow()
    {
        // Case badge
        var caseIcon = new FontIcon()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .GlyphWithVisibility<DeclensionPracticeViewModel>(vm => vm.CaseGlyph);
        var caseText = new TextBlock()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Text<DeclensionPracticeViewModel>(vm => vm.CaseLabel);
        var caseBadge = new Border()
            .CornerRadius(16)
            .Padding(12, 6)
            .Background<DeclensionPracticeViewModel>(vm => vm.CaseBrush)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(caseIcon, caseText));

        // Gender badge
        var genderIcon = new FontIcon()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .GlyphWithVisibility<DeclensionPracticeViewModel>(vm => vm.GenderGlyph);
        var genderText = new TextBlock()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Text<DeclensionPracticeViewModel>(vm => vm.GenderLabel);
        var genderBadge = new Border()
            .CornerRadius(16)
            .Padding(12, 6)
            .Background<DeclensionPracticeViewModel>(vm => vm.GenderBrush)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(genderIcon, genderText));

        // Number badge
        var numberIcon = new FontIcon()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .GlyphWithVisibility<DeclensionPracticeViewModel>(vm => vm.NumberGlyph);
        var numberText = new TextBlock()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Text<DeclensionPracticeViewModel>(vm => vm.NumberLabel);
        var numberBadge = new Border()
            .CornerRadius(16)
            .Padding(12, 6)
            .Background<DeclensionPracticeViewModel>(vm => vm.NumberBrush)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(numberIcon, numberText));

        var panel = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(LayoutConstants.Spacing.BadgeSpacingTall)
            .Children(caseBadge, genderBadge, numberBadge);

        return (panel, caseBadge, caseText, caseIcon, genderBadge, genderText, genderIcon, numberBadge, numberText, numberIcon);
    }
}
