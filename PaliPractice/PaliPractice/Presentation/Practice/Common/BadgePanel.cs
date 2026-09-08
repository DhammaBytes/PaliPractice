using Windows.Foundation;

namespace PaliPractice.Presentation.Practice.Common;

/// <summary>
/// Keeps badges at their readable size and centers each row when a narrow card requires wrapping.
/// </summary>
public sealed class BadgePanel : Panel
{
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(BadgePanel),
        new PropertyMetadata(0d, (sender, _) => ((BadgePanel)sender).InvalidateMeasure()));

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        return LayoutRows(availableSize.Width, arrange: false);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        LayoutRows(finalSize.Width, arrange: true);
        return finalSize;
    }

    Size LayoutRows(double width, bool arrange)
    {
        var row = new List<UIElement>();
        double rowWidth = 0, rowHeight = 0, top = 0, widest = 0;

        void FinishRow()
        {
            if (arrange)
            {
                var left = Math.Max(0, (width - rowWidth) / 2);
                foreach (var child in row)
                {
                    child.Arrange(new Rect(left, top, child.DesiredSize.Width, rowHeight));
                    left += child.DesiredSize.Width + Spacing;
                }
            }
            widest = Math.Max(widest, rowWidth);
            top += rowHeight + Spacing;
            row.Clear();
            rowWidth = rowHeight = 0;
        }

        foreach (var child in Children.Where(child => child.Visibility == Visibility.Visible))
        {
            var childWidth = child.DesiredSize.Width;
            if (row.Count > 0 && rowWidth + Spacing + childWidth > width)
                FinishRow();
            rowWidth += (row.Count > 0 ? Spacing : 0) + childWidth;
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            row.Add(child);
        }
        if (row.Count > 0)
            FinishRow();

        return new Size(widest, Math.Max(0, top - Spacing));
    }
}
