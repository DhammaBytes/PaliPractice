using PaliPractice.Presentation.Common;

namespace PaliPractice.Presentation.Main;

public sealed partial class HelpPage : Page
{
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
                                    // Getting Started
                                    new StackPanel()
                                        .Spacing(8)
                                        .Children(
                                            new TextBlock()
                                                .Text("Getting Started")
                                                .FontSize(24)
                                                .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                                .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),
                                            new TextBlock()
                                                .Text("Pali Practice helps you learn Pali noun declensions and verb conjugations through flashcard-style exercises.")
                                                .TextWrapping(TextWrapping.Wrap)
                                                .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                        ),

                                    // Declension Practice
                                    new StackPanel()
                                        .Spacing(8)
                                        .Children(
                                            new TextBlock()
                                                .Text("Declension Practice")
                                                .FontSize(20)
                                                .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),
                                            new TextBlock()
                                                .Text("Practice noun declensions by case (nominative, accusative, etc.) and number (singular, plural). You'll see the dictionary form and meaning, then try to recall the correct inflected form.")
                                                .TextWrapping(TextWrapping.Wrap)
                                                .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                        ),

                                    // Conjugation Practice
                                    new StackPanel()
                                        .Spacing(8)
                                        .Children(
                                            new TextBlock()
                                                .Text("Conjugation Practice")
                                                .FontSize(20)
                                                .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),
                                            new TextBlock()
                                                .Text("Practice verb conjugations by person (1st, 2nd, 3rd) and tense. You'll see the root verb and meaning, then recall the correct conjugated form.")
                                                .TextWrapping(TextWrapping.Wrap)
                                                .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                        ),

                                    // How to Use
                                    new StackPanel()
                                        .Spacing(8)
                                        .Children(
                                            new TextBlock()
                                                .Text("How to Use")
                                                .FontSize(20)
                                                .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                                                .Foreground(ThemeResource.Get<Brush>("PrimaryBrush")),
                                            new TextBlock()
                                                .Text("1. Tap 'Reveal' to see the answer\n2. Mark 'Easy' if you knew it, 'Hard' if you didn't\n3. Words marked 'Hard' will appear more frequently\n4. Track your daily progress with the goal bar")
                                                .TextWrapping(TextWrapping.Wrap)
                                                .Foreground(ThemeResource.Get<Brush>("OnBackgroundBrush"))
                                        )
                                )
                        )
                )
            )
        );
    }
}
