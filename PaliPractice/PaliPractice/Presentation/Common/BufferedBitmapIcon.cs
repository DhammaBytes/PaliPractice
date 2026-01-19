using Microsoft.UI.Dispatching;

namespace PaliPractice.Presentation.Common;

/// <summary>
/// A double-buffered BitmapIcon that prevents visual blinking during icon transitions.
/// Maintains two BitmapIcon elements and swaps visibility after allowing a frame to render.
/// </summary>
public sealed class BufferedBitmapIcon : Grid
{
    readonly BitmapIcon _iconA;
    readonly BitmapIcon _iconB;
    bool _aIsActive = true;
    bool _swapPending;
    string? _currentSource;

    public BufferedBitmapIcon()
    {
        _iconA = CreateIcon();
        _iconB = CreateIcon();
        _iconB.Opacity = 0;

        Children.Add(_iconA);
        Children.Add(_iconB);

        Unloaded += (_, _) => _swapPending = false;  // Cancel pending swap on unload
    }

    static BitmapIcon CreateIcon() => new BitmapIcon()
        .ShowAsMonochrome(true)
        .HorizontalAlignment(HorizontalAlignment.Left)
        .VerticalAlignment(VerticalAlignment.Center);

    #region Source Property

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(string),
        typeof(BufferedBitmapIcon),
        new PropertyMetadata(null, OnSourceChanged));

    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BufferedBitmapIcon self)
            self.UpdateSource((string?)e.NewValue);
    }

    #endregion

    #region IconHeight Property

    public static readonly DependencyProperty IconHeightProperty = DependencyProperty.Register(
        nameof(IconHeight),
        typeof(double),
        typeof(BufferedBitmapIcon),
        new PropertyMetadata(16.0, OnIconHeightChanged));

    public double IconHeight
    {
        get => (double)GetValue(IconHeightProperty);
        set => SetValue(IconHeightProperty, value);
    }

    static void OnIconHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BufferedBitmapIcon self && e.NewValue is double height)
        {
            self._iconA.Height = height;
            self._iconB.Height = height;
        }
    }

    #endregion

    #region IconForeground Property

    public static readonly DependencyProperty IconForegroundProperty = DependencyProperty.Register(
        nameof(IconForeground),
        typeof(Brush),
        typeof(BufferedBitmapIcon),
        new PropertyMetadata(null, OnIconForegroundChanged));

    public Brush? IconForeground
    {
        get => (Brush?)GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    static void OnIconForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BufferedBitmapIcon self && e.NewValue is Brush brush)
        {
            self._iconA.Foreground = brush;
            self._iconB.Foreground = brush;
        }
    }

    #endregion

    void UpdateSource(string? newSource)
    {
        // Handle null/empty - hide current icon
        if (string.IsNullOrEmpty(newSource))
        {
            var current = _aIsActive ? _iconA : _iconB;
            current.Opacity = 0;
            _currentSource = null;
            _swapPending = false;
            return;
        }

        // Same source - no change needed
        if (newSource == _currentSource)
            return;

        _currentSource = newSource;

        // Load new icon into back buffer
        var backIcon = _aIsActive ? _iconB : _iconA;

        try
        {
            backIcon.UriSource = new Uri(newSource);

            // Queue swap only if not already pending (avoids duplicate callbacks)
            if (!_swapPending)
            {
                _swapPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, SwapBuffers);
            }
        }
        catch
        {
            // Invalid URI - hide current icon
            var current = _aIsActive ? _iconA : _iconB;
            current.Opacity = 0;
        }
    }

    void SwapBuffers()
    {
        _swapPending = false;

        // Guard: skip if unloaded or no valid source
        if (_currentSource is null)
            return;

        var frontIcon = _aIsActive ? _iconA : _iconB;
        var backIcon = _aIsActive ? _iconB : _iconA;

        // Only swap if back icon has the current source (not stale)
        if (backIcon.UriSource?.OriginalString != _currentSource)
            return;

        backIcon.Opacity = 1;
        frontIcon.Opacity = 0;
        _aIsActive = !_aIsActive;
    }
}
