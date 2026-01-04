using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Main.ViewModels;
using static PaliPractice.Presentation.Common.RichTextHelper;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Main;

public sealed partial class HelpPage : Page
{
    // Styling constants
    const double BodyFontSize = 15;
    const double TitleFontSize = 18;
    const double MainTitleFontSize = 22;
    const double TitleToContentSpacing = 12;
    const double FaqBlockSpacing = 16;
    const double ContentPadding = 20;

    public HelpPage()
    {
        this.DataContext<HelpViewModel>((page, _) => page
            .NavigationCacheMode<HelpPage>(NavigationCacheMode.Required)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    // Row 0: Title bar
                    AppTitleBar.Build<HelpViewModel>("Help", vm => vm.GoBackCommand),

                    // Row 1: Content
                    new ScrollViewer()
                        .Grid(row: 1)
                        .Content(
                            new StackPanel()
                                .MaxWidth(LayoutConstants.ContentMaxWidth + ContentPadding * 2)
                                .Padding(ContentPadding)
                                .Spacing(24)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Children(
                                    // How to Practice
                                    BuildSection(HelpViewModel.HowToPracticeTitle, HelpViewModel.HowToPractice, isMainTitle: true),

                                    // Spaced Repetition
                                    BuildSection(HelpViewModel.SpacedRepetitionTitle, HelpViewModel.SpacedRepetition),

                                    // FAQ
                                    BuildFaqSection(HelpViewModel.QuestionsTitle,
                                        HelpViewModel.FaqGrammar,
                                        HelpViewModel.FaqMultipleForms,
                                        HelpViewModel.FaqExamples,
                                        HelpViewModel.FaqMissingWords,
                                        HelpViewModel.FaqMissingForms,
                                        HelpViewModel.FaqAboutApp)
                                )
                        )
                )
            )
        );
    }

    /// <summary>
    /// Builds a section with title and rich markdown content.
    /// </summary>
    static StackPanel BuildSection(string title, string content, bool isMainTitle = false)
    {
        return new StackPanel()
            .Spacing(TitleToContentSpacing)
            .Children(
                RegularText()
                    .Text(title)
                    .FontSize(isMainTitle ? MainTitleFontSize : TitleFontSize)
                    .FontWeight(isMainTitle
                        ? Microsoft.UI.Text.FontWeights.Bold
                        : Microsoft.UI.Text.FontWeights.SemiBold)
                    .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),
                CreateRichContent(content, fontSize: BodyFontSize)
            );
    }

    /// <summary>
    /// Builds a FAQ section with title and multiple Q-A blocks.
    /// Each Q-A block has tight internal spacing, but larger spacing between blocks.
    /// </summary>
    static StackPanel BuildFaqSection(string title, params string[] qaBlocks)
    {
        var children = new List<UIElement>
        {
            RegularText()
                .Text(title)
                .FontSize(TitleFontSize)
                .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                .Foreground(ThemeResource.Get<Brush>("PrimaryBrush"))
                .Margin(0, 0, 0, TitleToContentSpacing)
        };

        for (int i = 0; i < qaBlocks.Length; i++)
        {
            var qaPanel = CreateRichContent(qaBlocks[i], fontSize: BodyFontSize);
            // Add spacing after each Q-A block except the last
            if (i < qaBlocks.Length - 1)
                qaPanel.Margin(0, 0, 0, FaqBlockSpacing);
            children.Add(qaPanel);
        }

        return new StackPanel()
            .Children(children.ToArray());
    }
}
