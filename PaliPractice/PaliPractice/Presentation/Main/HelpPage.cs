using PaliPractice.Presentation.Common;
using PaliPractice.Presentation.Main.ViewModels;
using static PaliPractice.Presentation.Common.RichTextHelper;

namespace PaliPractice.Presentation.Main;

public sealed partial class HelpPage : Page
{
    const string HowToPractice = """
This app helps you memorize Pāli noun and verb forms. You will see a Pāli word along with labels indicating the required form — such as case and number for nouns, or tense and person for verbs.

Try to recall the correct inflected form and its meaning. Tap **Reveal** to see the answer along with possible translations. Then mark **Easy** or **Hard** based on how well you knew both.
""";

    const string SpacedRepetition = """
The app tracks how well you know each form. Every form has a progress level from 1 to 10:

• **Level 1–3:** Still learning — reviewed within days
• **Level 4–6:** Becoming familiar — reviewed every week or two
• **Level 7–9:** Well known — reviewed monthly or less
• **Level 10:** Mastered — reviewed after about 8 months

New forms start at level 4. Marking **Easy** increases the level by 1; marking **Hard** decreases it by 1. This ensures you spend more time on forms you find difficult.
""";

    const string Faq = """
**Where can I learn the grammar?**

This app is focused on memorizing, so it does not replace textbooks or courses. See the [Learn Pali: Best way to start?](https://palistudies.blogspot.com/2018/05/learn-pali-where-to-begin.html) guide and its author's [Learn Pali Language series](https://www.youtube.com/playlist?list=PLf6RXFuRpeLRYj-wWs4KFrTPrzvAWWMpo) on YouTube.

**Why do some answers show multiple forms?**

Certain cases and tenses in Pāli have more than one valid ending. The app displays all correct forms for the given grammatical context.

**Why do sutta examples use different forms?**

Example sentences come from the [Digital Pāli Dictionary](https://digitalpalidictionary.github.io/), which provides 1–2 examples per word meaning rather than per inflected form. These examples illustrate how the word is used in context, not necessarily the specific form being practiced.
""";

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
                                .Padding(20)
                                .Spacing(24)
                                .MaxWidth(600)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Children(
                                    // How to Practice
                                    BuildSection("How to Practice", HowToPractice, isMainTitle: true),

                                    // Spaced Repetition
                                    BuildSection("Spaced Repetition", SpacedRepetition),

                                    // FAQ
                                    BuildSection("Questions", Faq)
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
            .Spacing(12)
            .Children(
                new TextBlock()
                    .Text(title)
                    .FontSize(isMainTitle ? 24 : 20)
                    .FontWeight(isMainTitle
                        ? Microsoft.UI.Text.FontWeights.Bold
                        : Microsoft.UI.Text.FontWeights.SemiBold)
                    .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),
                CreateRichText(content)
                    .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
            );
    }
}
