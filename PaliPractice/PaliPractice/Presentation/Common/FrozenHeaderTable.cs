using PaliPractice.Presentation.Practice.ViewModels;
using static PaliPractice.Presentation.Common.TextHelpers;

namespace PaliPractice.Presentation.Common;

/// <summary>
/// A table control with frozen row and column headers that remain visible
/// during scrolling (Excel-style sticky headers).
///
/// Architecture:
/// - Column headers stay frozen at top with horizontal scroll sync
/// - Row headers and content share the same Grid (ensuring matching row heights)
/// - Outer vertical ScrollViewer scrolls both row headers and content together
/// - Inner horizontal ScrollViewer scrolls only the content columns
/// - Row headers are overlaid to stay visible during horizontal scroll
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
        // Build the full body grid with row headers in column 0 and content in columns 1-N
        var (bodyGrid, rowHeaderWidth) = BuildBodyGrid(rowHeaders, cells, columnHeaders.Count);

        // Build column headers panel
        var columnHeadersPanel = BuildColumnHeaders(columnHeaders);

        // ScrollViewer for column headers (horizontal only, synced with content)
        var columnHeadersScrollViewer = new ScrollViewer()
            .HorizontalScrollMode(ScrollMode.Auto)
            .VerticalScrollMode(ScrollMode.Disabled)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Hidden)
            .VerticalScrollBarVisibility(ScrollBarVisibility.Hidden)
            .Content(columnHeadersPanel);

        // Main content ScrollViewer (scrolls both ways)
        var bodyScrollViewer = new ScrollViewer()
            .HorizontalScrollMode(ScrollMode.Auto)
            .VerticalScrollMode(ScrollMode.Auto)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Auto)
            .VerticalScrollBarVisibility(ScrollBarVisibility.Auto)
            .Content(bodyGrid);

        // Synchronize horizontal scrolling for column headers
        bodyScrollViewer.ViewChanged += (s, e) =>
        {
            columnHeadersScrollViewer.ChangeView(bodyScrollViewer.HorizontalOffset, null, null, disableAnimation: true);
        };

        // Corner cell
        var cornerCell = new Border()
            .Width(rowHeaderWidth)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .BorderBrush(ThemeResource.Get<Brush>("SystemControlForegroundBaseMediumBrush"))
            .BorderThickness(0, 0, 1, 1);

        // Main layout: 2 rows (header row, body row)
        return new Grid()
            .RowDefinitions("Auto,*")
            .ColumnDefinitions($"{rowHeaderWidth},*")
            .Children(
                // [0,0] Corner cell
                cornerCell.Grid(row: 0, column: 0),

                // [0,1] Column headers (scrollable horizontally)
                columnHeadersScrollViewer.Grid(row: 0, column: 1),

                // [1,0-1] Body with row headers and content (spans both columns)
                bodyScrollViewer.Grid(row: 1, column: 0, columnSpan: 2)
            );
    }

    /// <summary>
    /// Builds the body grid with row headers in column 0 and data cells in columns 1-N.
    /// Returns the grid and the row header column width.
    /// </summary>
    static (Grid grid, double rowHeaderWidth) BuildBodyGrid(
        IReadOnlyList<string> rowHeaders,
        IReadOnlyList<IReadOnlyList<TableCell>> cells,
        int columnCount)
    {
        int rowCount = rowHeaders.Count;

        // Row definitions: one per data row
        var rowDefs = string.Join(",", Enumerable.Repeat("Auto", rowCount));

        // Column definitions: Auto for row header, then fixed width for each data column
        var colDefs = $"{RowHeaderWidth}," + string.Join(",", Enumerable.Repeat($"{CellWidth}", columnCount));

        var grid = new Grid()
            .RowDefinitions(rowDefs)
            .ColumnDefinitions(colDefs);

        // Add row headers (column 0)
        for (int row = 0; row < rowCount; row++)
        {
            var headerCell = BuildRowHeaderCell(rowHeaders[row]);
            headerCell.Grid(row: row, column: 0);
            grid.Children.Add(headerCell);
        }

        // Add data cells (columns 1-N)
        for (int row = 0; row < cells.Count; row++)
        {
            var rowCells = cells[row];
            for (int col = 0; col < rowCells.Count; col++)
            {
                var cell = BuildDataCell(rowCells[col]);
                cell.Grid(row: row, column: col + 1); // +1 because column 0 is row headers
                grid.Children.Add(cell);
            }
        }

        return (grid, RowHeaderWidth);
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

    static Border BuildRowHeaderCell(string header)
    {
        return new Border()
            .Width(RowHeaderWidth)
            .MinHeight(CellMinHeight)
            .Padding(8, 6)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .BorderBrush(ThemeResource.Get<Brush>("SystemControlForegroundBaseMediumBrush"))
            .BorderThickness(0, 0, 1, 1)
            .Child(
                RegularText()
                    .Text(header)
                    .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                    .Foreground(ThemeResource.Get<Brush>("PrimaryBrush"))
                    .HorizontalAlignment(HorizontalAlignment.Right)
                    .VerticalAlignment(VerticalAlignment.Center)
            );
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
    const double RowHeaderWidth = 70;
    const double CellMinHeight = 36;
}
