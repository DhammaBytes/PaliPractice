using PaliPractice.Presentation.Practice.ViewModels;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Common;

/// <summary>
/// A table control with frozen row and column headers that remain visible
/// during scrolling (Excel-style sticky headers).
///
/// Architecture:
/// - Grid with 4 quadrants: corner, column headers, row headers, content
/// - Content ScrollViewer drives header synchronization via ViewChanged event
/// - Column headers scroll horizontally with content
/// - Row headers scroll vertically with content
/// </summary>
public static class FrozenHeaderTable
{
    /// <summary>
    /// Builds a frozen header table from the given data.
    /// </summary>
    public static Grid Build(
        IReadOnlyList<string> columnHeaders,
        IReadOnlyList<string> rowHeaders,
        IReadOnlyList<IReadOnlyList<TableCell>> cells)
    {
        // Create the content grid with data cells
        var contentGrid = BuildContentGrid(cells, columnHeaders.Count, rowHeaders.Count);

        // Create the column headers row
        var columnHeadersPanel = BuildColumnHeaders(columnHeaders);

        // Create the row headers column
        var rowHeadersPanel = BuildRowHeaders(rowHeaders);

        // Create ScrollViewers for synchronized scrolling
        var contentScrollViewer = new ScrollViewer()
            .HorizontalScrollMode(ScrollMode.Auto)
            .VerticalScrollMode(ScrollMode.Auto)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Auto)
            .VerticalScrollBarVisibility(ScrollBarVisibility.Auto)
            .Content(contentGrid);

        var columnHeadersScrollViewer = new ScrollViewer()
            .HorizontalScrollMode(ScrollMode.Auto)
            .VerticalScrollMode(ScrollMode.Disabled)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Hidden)
            .VerticalScrollBarVisibility(ScrollBarVisibility.Hidden)
            .Content(columnHeadersPanel);

        var rowHeadersScrollViewer = new ScrollViewer()
            .HorizontalScrollMode(ScrollMode.Disabled)
            .VerticalScrollMode(ScrollMode.Auto)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Hidden)
            .VerticalScrollBarVisibility(ScrollBarVisibility.Hidden)
            .Content(rowHeadersPanel);

        // Synchronize scrolling
        contentScrollViewer.ViewChanged += (s, e) =>
        {
            columnHeadersScrollViewer.ChangeView(contentScrollViewer.HorizontalOffset, null, null, disableAnimation: true);
            rowHeadersScrollViewer.ChangeView(null, contentScrollViewer.VerticalOffset, null, disableAnimation: true);
        };

        // Build the main grid with 4 quadrants
        var cornerCell = new Border()
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .BorderBrush(ThemeResource.Get<Brush>("SystemControlForegroundBaseMediumBrush"))
            .BorderThickness(0, 0, 1, 1);

        return new Grid()
            .RowDefinitions("Auto,*")
            .ColumnDefinitions("Auto,*")
            .Children(
                // [0,0] Corner cell
                cornerCell
                    .Grid(row: 0, column: 0),

                // [0,1] Column headers
                columnHeadersScrollViewer
                    .Grid(row: 0, column: 1),

                // [1,0] Row headers
                rowHeadersScrollViewer
                    .Grid(row: 1, column: 0),

                // [1,1] Content
                contentScrollViewer
                    .Grid(row: 1, column: 1)
            );
    }

    static StackPanel BuildColumnHeaders(IReadOnlyList<string> headers)
    {
        var panel = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"));

        foreach (var header in headers)
        {
            var cell = new Border()
                .Width(CellWidth)
                .Padding(8, 6)
                .BorderBrush(ThemeResource.Get<Brush>("SystemControlForegroundBaseMediumBrush"))
                .BorderThickness(0, 0, 1, 1)
                .Child(
                    RegularText()
                        .Text(header)
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .Foreground(ThemeResource.Get<Brush>("PrimaryBrush"))
                        .TextAlignment(TextAlignment.Center)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                );
            panel.Children.Add(cell);
        }

        return panel;
    }

    static StackPanel BuildRowHeaders(IReadOnlyList<string> headers)
    {
        var panel = new StackPanel()
            .Orientation(Orientation.Vertical)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"));

        foreach (var header in headers)
        {
            var cell = new Border()
                .MinHeight(CellMinHeight)
                .Padding(12, 6)
                .BorderBrush(ThemeResource.Get<Brush>("SystemControlForegroundBaseMediumBrush"))
                .BorderThickness(0, 0, 1, 1)
                .Child(
                    RegularText()
                        .Text(header)
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .Foreground(ThemeResource.Get<Brush>("PrimaryBrush"))
                        .VerticalAlignment(VerticalAlignment.Center)
                );
            panel.Children.Add(cell);
        }

        return panel;
    }

    static Grid BuildContentGrid(
        IReadOnlyList<IReadOnlyList<TableCell>> cells,
        int columnCount,
        int rowCount)
    {
        // Create row/column definitions
        var rowDefs = string.Join(",", Enumerable.Repeat("Auto", rowCount));
        var colDefs = string.Join(",", Enumerable.Repeat($"{CellWidth}", columnCount));

        var grid = new Grid()
            .RowDefinitions(rowDefs)
            .ColumnDefinitions(colDefs);

        for (int row = 0; row < cells.Count; row++)
        {
            var rowCells = cells[row];
            for (int col = 0; col < rowCells.Count; col++)
            {
                var cell = BuildDataCell(rowCells[col]);
                cell.Grid(row: row, column: col);
                grid.Children.Add(cell);
            }
        }

        return grid;
    }

    static Border BuildDataCell(TableCell cell)
    {
        var panel = new StackPanel()
            .Spacing(2)
            .VerticalAlignment(VerticalAlignment.Center);

        foreach (var form in cell.Forms)
        {
            var textBlock = PaliText()
                .TextWrapping(TextWrapping.NoWrap);

            // Add stem part (regular)
            if (!string.IsNullOrEmpty(form.Stem))
            {
                textBlock.Inlines.Add(new Run { Text = form.Stem });
            }

            // Add ending part (bold)
            if (!string.IsNullOrEmpty(form.Ending))
            {
                textBlock.Inlines.Add(new Run
                {
                    Text = form.Ending,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                });
            }

            // Gray out non-attested forms
            if (!form.InCorpus)
            {
                textBlock.Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"));
            }

            panel.Children.Add(textBlock);
        }

        // Empty cell placeholder if no forms
        if (cell.Forms.Count == 0)
        {
            panel.Children.Add(RegularText()
                .Text("-")
                .Foreground(ThemeResource.Get<Brush>("OnSurfaceVariantBrush"))
                .HorizontalAlignment(HorizontalAlignment.Center));
        }

        return new Border()
            .MinHeight(CellMinHeight)
            .Width(CellWidth)
            .Padding(8, 4)
            .BorderBrush(ThemeResource.Get<Brush>("SystemControlForegroundBaseLowBrush"))
            .BorderThickness(0, 0, 1, 1)
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
            .Child(panel);
    }

    const double CellWidth = 120;
    const double CellMinHeight = 36;
}
