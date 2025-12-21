using PaliPractice.Presentation.Bindings;
using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Practice.Controls;

namespace PaliPractice.Presentation.Practice;

public sealed partial class DeclensionPracticePage : Page
{
    public DeclensionPracticePage()
    {
        // Element references for responsive sizing
        var elements = new ResponsiveElements();

        DeclensionPracticePageMarkup.DataContext<DeclensionPracticeViewModel>(this, (page, _) => page
            .NavigationCacheMode<DeclensionPracticePage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(BuildPageLayout(elements))
        );

        // Attach height-responsive behavior
        if (elements.ContentArea is not null)
        {
            HeightResponsiveHelper.AttachResponsiveHandler(elements.ContentArea, heightClass =>
                ApplyResponsiveValues(elements, heightClass));
        }
    }

    static Grid BuildPageLayout(ResponsiveElements elements)
    {
        // Build the card elements
        var (wordTextBlock, badgesPanel, badgeBorders, badgeTextBlocks, badgeIcons) = BuildCardElements();
        elements.WordTextBlock = wordTextBlock;
        elements.BadgesPanel = badgesPanel;
        elements.BadgeBorders.AddRange(badgeBorders);
        elements.BadgeTextBlocks.AddRange(badgeTextBlocks);
        elements.BadgeIcons.AddRange(badgeIcons);

        // Build answer section - shown when revealed
        var answerTextBlock = new TextBlock()
            .FontSize(32)
            .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceBrush"))
            .Text<DeclensionPracticeViewModel>(vm => vm.Flashcard.Answer);
        elements.AnswerTextBlock = answerTextBlock;

        // Alternative forms (smaller, medium weight) - shown when revealed and non-empty
        var alternativeFormsTextBlock = new TextBlock()
            .FontSize(20)
            .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .TextAlignment(TextAlignment.Center)
            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .Text<DeclensionPracticeViewModel>(vm => vm.AlternativeForms)
            .StringToVisibility<TextBlock, DeclensionPracticeViewModel>(vm => vm.AlternativeForms);

        // Invisible spacer - always reserves height for 2 lines (prevents layout jump)
        var answerSpacer = new StackPanel()
            .Spacing(4)
            .Opacity(0)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Children(
                new TextBlock()
                    .FontSize(32)
                    .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                    .Text("X"), // Single char to reserve line height
                new TextBlock()
                    .FontSize(20)
                    .FontWeight(Microsoft.UI.Text.FontWeights.Medium)
                    .Text("X")  // Second line reservation
            );

        // Actual answer - centered in reserved space
        var answerContent = new StackPanel()
            .Spacing(4)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center)
            .BoolToVisibility<StackPanel, DeclensionPracticeViewModel>(vm => vm.Flashcard.IsRevealed)
            .Children(answerTextBlock, alternativeFormsTextBlock);

        // Dotted line placeholder - centered in reserved space
        var answerPlaceholder = new Border()
            .Height(2)
            .Margin(40, 0, 40, 0)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .VerticalAlignment(VerticalAlignment.Center)
            .BorderBrush(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
            .BorderThickness(0, 0, 0, 2)
            .Opacity(0.5)
            .BoolToVisibility<Border, DeclensionPracticeViewModel>(vm => vm.Flashcard.IsRevealed, invert: true);

        // Container that stacks spacer, content, and placeholder in same cell
        var answerContainer = new Grid()
            .Margin(0, 8, 0, 0)
            .Children(answerSpacer, answerContent, answerPlaceholder);

        // Build card border containing everything
        var cardBorder = new Border()
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .CornerRadius(12)
            .Padding(24)
            .Child(
                new StackPanel()
                    .Spacing(12)
                    .Children(
                        // Header (rank + anki)
                        CardHeader.Build<DeclensionPracticeViewModel>(vm => vm.WordCard, "N"),
                        // Word
                        wordTextBlock,
                        // Badges
                        badgesPanel,
                        // Case hint
                        new TextBlock()
                            .Text<DeclensionPracticeViewModel>(vm => vm.CaseHint)
                            .FontSize(14)
                            .FontStyle(Windows.UI.Text.FontStyle.Italic)
                            .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                            .HorizontalAlignment(HorizontalAlignment.Center)
                            .TextAlignment(TextAlignment.Center),
                        // Answer area (fixed height via invisible spacer)
                        answerContainer
                    )
            );
        elements.CardBorder = cardBorder;

        // Build the content area (everything between AppBar and PracticeNav)
        var contentArea = new Grid()
            .RowDefinitions("*,Auto,*,Auto,Auto,Auto")
            .MaxWidth(LayoutConstants.ContentMaxWidth)
            .HorizontalAlignment(HorizontalAlignment.Stretch)
            .Padding(LayoutConstants.Spacing.ContentPaddingTall, 0)
            .Children(
                // Row 0: Top spacer
                new Border().Grid(row: 0),

                // Row 1: Card
                cardBorder.Grid(row: 1),

                // Row 2: Bottom spacer
                new Border().Grid(row: 2),

                // Row 3: Translation
                TranslationDisplay.Build<DeclensionPracticeViewModel>(
                    carouselPath: vm => vm.ExampleCarousel,
                    isRevealedPath: vm => vm.Flashcard.IsRevealed)
                    .Grid(row: 3),

                // Row 4: Example + Reference
                ExampleSection.Build<DeclensionPracticeViewModel>(
                    vm => vm.ExampleCarousel)
                    .Margin(0, 12, 0, 0)
                    .Grid(row: 4),

                // Row 5: Carousel paging
                CarouselPaging.Build<DeclensionPracticeViewModel>(
                    vm => vm.ExampleCarousel)
                    .Margin(0, 8, 0, 0)
                    .Grid(row: 5)
            );
        elements.ContentArea = contentArea;

        // Main page grid
        return new Grid()
            .SafeArea(SafeArea.InsetMask.VisibleBounds)
            .RowDefinitions("Auto,*,Auto,Auto")
            .Children(
                // Row 0: Title bar
                AppTitleBar.BuildWithHistory<DeclensionPracticeViewModel>(
                    "Declension Practice",
                    vm => vm.GoBackCommand,
                    vm => vm.GoToHistoryCommand)
                    .Grid(row: 0),

                // Row 1: Content area
                contentArea.Grid(row: 1),

                // Row 2: Practice navigation
                PracticeNavigation.BuildWithReveal<DeclensionPracticeViewModel>(
                    revealCommand: vm => vm.RevealCommand,
                    hardCommand: vm => vm.HardCommand,
                    easyCommand: vm => vm.EasyCommand,
                    isRevealedPath: vm => vm.Flashcard.IsRevealed)
                    .Grid(row: 2),

                // Row 3: Daily goal
                DailyGoalBar.Build<DeclensionPracticeViewModel>(
                    dailyGoalText: vm => vm.DailyGoal.DailyGoalText,
                    dailyProgress: vm => vm.DailyGoal.DailyProgress)
                    .Grid(row: 3)
            );
    }

    static (TextBlock wordTextBlock, StackPanel badgesPanel,
            Border[] badgeBorders, TextBlock[] badgeTexts, FontIcon[] badgeIcons) BuildCardElements()
    {
        // Word TextBlock
        var wordTextBlock = CardWord.Build<DeclensionPracticeViewModel>(vm => vm.WordCard);

        // Build badges
        var (caseIcon, caseText, caseBadge) = BuildBadge<DeclensionPracticeViewModel>(
            vm => vm.CaseGlyph, vm => vm.CaseLabel, vm => vm.CaseBrush);
        var (genderIcon, genderText, genderBadge) = BuildBadge<DeclensionPracticeViewModel>(
            vm => vm.GenderGlyph, vm => vm.GenderLabel, vm => vm.GenderBrush);
        var (numberIcon, numberText, numberBadge) = BuildBadge<DeclensionPracticeViewModel>(
            vm => vm.NumberGlyph, vm => vm.NumberLabel, vm => vm.NumberBrush);

        var badgesPanel = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Spacing(LayoutConstants.Spacing.BadgeSpacingTall)
            .Children(caseBadge, genderBadge, numberBadge);

        return (wordTextBlock, badgesPanel,
                [caseBadge, genderBadge, numberBadge],
                [caseText, genderText, numberText],
                [caseIcon, genderIcon, numberIcon]);
    }

    static (FontIcon icon, TextBlock text, Border badge) BuildBadge<TDC>(
        System.Linq.Expressions.Expression<Func<TDC, string?>> glyphPath,
        System.Linq.Expressions.Expression<Func<TDC, string>> labelPath,
        System.Linq.Expressions.Expression<Func<TDC, Brush>> brushPath)
    {
        var icon = new FontIcon()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .GlyphWithVisibility<TDC>(glyphPath);
        var text = new TextBlock()
            .FontSize(14)
            .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            .Text<TDC>(labelPath);
        var badge = new Border()
            .CornerRadius(16)
            .Padding(12, 6)
            .Background<TDC>(brushPath)
            .Child(new StackPanel()
                .Orientation(Orientation.Horizontal)
                .Spacing(6)
                .Children(icon, text));

        return (icon, text, badge);
    }

    static void ApplyResponsiveValues(ResponsiveElements elements, HeightResponsiveHelper.HeightClass heightClass)
    {
        // Content padding
        if (elements.ContentArea is not null)
        {
            var padding = HeightResponsiveHelper.GetContentPadding(heightClass);
            elements.ContentArea.Padding(new Thickness(padding, 0, padding, 0));
        }

        // Badges panel spacing
        if (elements.BadgesPanel is not null)
        {
            elements.BadgesPanel.Spacing = HeightResponsiveHelper.GetBadgeSpacing(heightClass);
        }

        // Main word font size
        if (elements.WordTextBlock is not null)
        {
            elements.WordTextBlock.FontSize = HeightResponsiveHelper.GetWordFontSize(heightClass);
        }

        // Answer font size
        if (elements.AnswerTextBlock is not null)
        {
            elements.AnswerTextBlock.FontSize = HeightResponsiveHelper.GetAnswerFontSize(heightClass);
        }

        // Card padding
        if (elements.CardBorder is not null)
        {
            var cardPad = HeightResponsiveHelper.GetCardPadding(heightClass);
            elements.CardBorder.Padding(new Thickness(cardPad));
        }

        // Badge styling
        var badgePadding = HeightResponsiveHelper.GetBadgePadding(heightClass);
        var badgeFontSize = HeightResponsiveHelper.GetBadgeFontSize(heightClass);

        foreach (var border in elements.BadgeBorders)
        {
            border.Padding(badgePadding);
        }

        foreach (var textBlock in elements.BadgeTextBlocks)
        {
            textBlock.FontSize = badgeFontSize;
        }

        foreach (var icon in elements.BadgeIcons)
        {
            icon.FontSize = badgeFontSize;
        }
    }

    /// <summary>
    /// Holds references to elements for responsive sizing.
    /// </summary>
    class ResponsiveElements
    {
        public Grid? ContentArea { get; set; }
        public Border? CardBorder { get; set; }
        public StackPanel? BadgesPanel { get; set; }
        public TextBlock? WordTextBlock { get; set; }
        public TextBlock? AnswerTextBlock { get; set; }
        public List<Border> BadgeBorders { get; } = [];
        public List<TextBlock> BadgeTextBlocks { get; } = [];
        public List<FontIcon> BadgeIcons { get; } = [];
    }
}
