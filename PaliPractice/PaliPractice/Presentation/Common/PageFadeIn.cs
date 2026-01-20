using Microsoft.UI.Xaml.Navigation;

namespace PaliPractice.Presentation.Common;

/// <summary>
/// Helper to fade in page content on forward navigation only.
/// Eliminates UI flickering by starting content invisible and fading it in
/// once the layout has stabilized.
/// </summary>
/// <remarks>
/// <para><b>Usage:</b></para>
/// <code>
/// .Content(PageFadeIn.Wrap(page, yourContent))
/// </code>
/// </remarks>
public static class PageFadeIn
{
    const int FadeDurationMs = 100;
    const int AnimationSteps = 5;
    const int BindingSettleDelayMs = 50;

    /// <summary>
    /// Wraps content in a container that fades in on forward navigation only.
    /// Waits for DataContext bindings to apply before showing.
    /// Back navigation shows content immediately without animation.
    /// </summary>
    public static Grid Wrap(Page page, UIElement content)
    {
        var container = new Grid()
            .Opacity(0)
            .Children(content);

        // Controller handles all event subscriptions and cleanup
        var controller = new FadeController(page, container);
        controller.Attach();

        return container;
    }

    /// <summary>
    /// Manages fade-in state and event subscriptions with proper cleanup.
    /// </summary>
    sealed class FadeController
    {
        readonly Page _page;
        readonly Grid _container;
        CancellationTokenSource? _cts;
        bool _hasAnimated;
        bool _isBackNav;
        bool _isLoaded;
        bool _hasDataContext;

        public FadeController(Page page, Grid container)
        {
            _page = page;
            _container = container;
        }

        public void Attach()
        {
            _page.Loaded += OnPageLoaded;
            _page.Unloaded += OnPageUnloaded;
            _page.DataContextChanged += OnDataContextChanged;
            _container.Loaded += OnContainerLoaded;
        }

        void Detach()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _page.Loaded -= OnPageLoaded;
            _page.Unloaded -= OnPageUnloaded;
            _page.DataContextChanged -= OnDataContextChanged;
            _container.Loaded -= OnContainerLoaded;

            if (_page.Frame != null)
                _page.Frame.Navigated -= OnFrameNavigated;
        }

        void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (_page.Frame != null)
                _page.Frame.Navigated += OnFrameNavigated;
        }

        void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            Detach();
        }

        void OnFrameNavigated(object sender, NavigationEventArgs e)
        {
            if (e.Content != _page) return;

            _isBackNav = e.NavigationMode == NavigationMode.Back;
            if (_isBackNav)
            {
                _container.Opacity = 1;
                _hasAnimated = true;
            }
        }

        async void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                _hasDataContext = true;
                await TryFadeInAsync();
            }
        }

        async void OnContainerLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            await TryFadeInAsync();
        }

        async Task TryFadeInAsync()
        {
            if (_hasAnimated || _isBackNav || !_isLoaded || !_hasDataContext)
                return;

            _hasAnimated = true;
            _cts = new CancellationTokenSource();

            try
            {
                // Wait for bindings to settle (ToggleSwitch, ComboBox, etc.)
                await Task.Delay(BindingSettleDelayMs, _cts.Token);
                await AnimateFadeInAsync(_container, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Page navigated away - just show immediately
                _container.Opacity = 1;
            }
        }

        static async Task AnimateFadeInAsync(UIElement element, CancellationToken ct)
        {
            var stepDelay = FadeDurationMs / AnimationSteps;

            for (var i = 1; i <= AnimationSteps; i++)
            {
                ct.ThrowIfCancellationRequested();
                element.Opacity = (double)i / AnimationSteps;
                await Task.Delay(stepDelay, ct);
            }

            element.Opacity = 1;
        }
    }
}
