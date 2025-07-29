namespace PaliPractice.Presentation;

public sealed partial class FlashcardPage : Page
{
    public FlashcardPage()
    {
        this.DataContext<FlashcardViewModel>((page, vm) => page
            .NavigationCacheMode(NavigationCacheMode.Required)
            .Background(Theme.Brushes.Background.Default)
            .Content(new Grid()
                .SafeArea(SafeArea.InsetMask.VisibleBounds)
                .RowDefinitions("Auto,*")
                .Children(
                    new NavigationBar().Content(() => vm.Title),
                    
                    // Main content area
                    new Grid()
                        .Grid(row: 1)
                        .Margin(20)
                        .Children(
                            // Loading indicator
                            new ProgressRing()
                                .IsActive(() => vm.IsLoading)
                                .Visibility(() => vm.IsLoading, loading => loading ? Visibility.Visible : Visibility.Collapsed)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Center),
                            
                            // Error message
                            new TextBlock()
                                .Text(() => vm.ErrorMessage)
                                .Visibility(() => vm.ErrorMessage, error => !string.IsNullOrEmpty(error) ? Visibility.Visible : Visibility.Collapsed)
                                .Foreground(Theme.Brushes.OnBackground.Medium)
                                .TextAlignment(TextAlignment.Center)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .VerticalAlignment(VerticalAlignment.Center),
                            
                            // Flashcard content
                            new StackPanel()
                                .Visibility(() => vm.IsLoading, loading => !loading ? Visibility.Visible : Visibility.Collapsed)
                                .VerticalAlignment(VerticalAlignment.Center)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Spacing(24)
                                .MaxWidth(400)
                                .Children(
                                    // Card container
                                    new Border()
                                        .Background(Theme.Brushes.Surface.Default)
                                        .CornerRadius(12)
                                        .Padding(32)
                                        .Child(
                                            new StackPanel()
                                                .Spacing(16)
                                                .Children(
                                                    // Pali noun (always visible)
                                                    new TextBlock()
                                                        .Text(() => vm.CurrentNoun)
                                                        .FontSize(32)
                                                        .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                                        .TextAlignment(TextAlignment.Center)
                                                        .Foreground(Theme.Brushes.Primary.Default),
                                                    
                                                    // POS and frequency info
                                                    new TextBlock()
                                                        .Text(() => vm.PosInfo)
                                                        .FontSize(14)
                                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                                        .TextAlignment(TextAlignment.Center)
                                                        .Foreground(Theme.Brushes.OnBackground.Medium),
                                                    
                                                    // Translation (conditionally visible)
                                                    new TextBlock()
                                                        .Text(() => vm.CurrentTranslation)
                                                        .FontSize(18)
                                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                                        .TextAlignment(TextAlignment.Center)
                                                        .Foreground(Theme.Brushes.OnBackground.Default)
                                                        .Visibility(() => vm.IsTranslationVisible, visible => 
                                                            visible ? Visibility.Visible : Visibility.Collapsed)
                                                )
                                        ),
                                    
                                    // Reveal button (only visible when translation is hidden)
                                    new Button()
                                        .Content("Reveal Translation")
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Command(() => vm.RevealCommand)
                                        .Visibility(() => vm.IsTranslationVisible, visible => 
                                            !visible ? Visibility.Visible : Visibility.Collapsed),
                                    
                                    // Navigation buttons (only visible when translation is shown)
                                    new StackPanel()
                                        .Orientation(Orientation.Horizontal)
                                        .HorizontalAlignment(HorizontalAlignment.Center)
                                        .Spacing(16)
                                        .Visibility(() => vm.IsTranslationVisible, visible => 
                                            visible ? Visibility.Visible : Visibility.Collapsed)
                                        .Children(
                                            new Button()
                                                .Content("Previous")
                                                .Command(() => vm.PreviousCommand),
                                            new Button()
                                                .Content("Next")
                                                .Command(() => vm.NextCommand)
                                        )
                                )
                        )
                )
            )
        );
    }
}