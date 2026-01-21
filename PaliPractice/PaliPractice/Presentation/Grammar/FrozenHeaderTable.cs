using PaliPractice.Presentation.Grammar.ViewModels;
using static PaliPractice.Presentation.Common.Text.TextHelpers;

namespace PaliPractice.Presentation.Grammar;

/// <summary>
/// Result of building a frozen header table.
/// </summary>
public record FrozenHeaderTableResult(Grid Table, bool HasNonCorpusForms);

/// <summary>
/// A table control with frozen row and column headers that remain visible
/// during scrolling (Excel-style sticky headers).
///
/// Architecture:
/// - Column headers stay frozen at top with horizontal scroll sync
/// - Row headers in body Grid are transparent "ghosts" that determine row heights
/// - Visible row headers are cloned and overlaid, synced vertically only
/// - Clone heights are synchronized via SizeChanged events from ghost cells
/// </summary>
public static class FrozenHeaderTable
{
    /// <summary>
    /// Builds a frozen header table from the given data.
    /// Returns the table and whether any cells contain non-corpus forms.
    /// </summary>
    public static FrozenHeaderTableResult Build(
        IReadOnlyList<string> columnHeaders,
        IReadOnlyList<string> rowHeaders,
        IReadOnlyList<IReadOnlyList<TableCell>> cells)
    {
        // Build cloned (visible) row headers first - we'll sync their heights later
        var clonedRowHeaders = new List<Border>();
        var clonedRowHeadersPanel = BuildClonedRowHeaders(rowHeaders, clonedRowHeaders);

        // Build the body grid with ghost row headers and content cells
        var (bodyGrid, rowHeaderWidth, hasNonCorpusForms) = BuildBodyGrid(rowHeaders, cells, columnHeaders.Count, clonedRowHeaders);

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

        // Cloned row headers ScrollViewer (vertical only, synced with body)
        var clonedHeadersScrollViewer = new ScrollViewer()
            .HorizontalScrollMode(ScrollMode.Disabled)
            .VerticalScrollMode(ScrollMode.Auto)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Hidden)
            .VerticalScrollBarVisibility(ScrollBarVisibility.Hidden)
            .IsHitTestVisible(false) // Let touches pass through to body
            .Content(clonedRowHeadersPanel);

        // Synchronize scrolling
        bodyScrollViewer.ViewChanged += (_, _) =>
        {
            // Sync column headers horizontally
            columnHeadersScrollViewer.ChangeView(bodyScrollViewer.HorizontalOffset, null, null, disableAnimation: true);
            // Sync cloned row headers vertically
            clonedHeadersScrollViewer.ChangeView(null, bodyScrollViewer.VerticalOffset, null, disableAnimation: true);
        };

        // Corner cell
        var cornerCell = new Border()
            .Width(rowHeaderWidth)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .BorderBrush(ThemeResource.Get<Brush>("NeutralGrayBrush"))
            .BorderThickness(0, 0, 1, 1);

        // Body area: overlay cloned row headers on top of scrollable content
        var bodyArea = new Grid()
            .Children(
                // Layer 1 (back): Body with ghost row headers + content
                bodyScrollViewer,
                // Layer 2 (front): Cloned row headers (frozen horizontally)
                clonedHeadersScrollViewer
                    .Width(rowHeaderWidth)
                    .HorizontalAlignment(HorizontalAlignment.Left)
            );

        // Main layout: 2 rows (header row, body row)
        var table = new Grid()
            .RowDefinitions("Auto,*")
            .ColumnDefinitions($"{rowHeaderWidth},*")
            .Children(
                // [0,0] Corner cell
                cornerCell.Grid(row: 0, column: 0),

                // [0,1] Column headers (scrollable horizontally)
                columnHeadersScrollViewer.Grid(row: 0, column: 1),

                // [1,0-1] Body area with overlay (spans both columns)
                bodyArea.Grid(row: 1, column: 0, columnSpan: 2)
            );

        return new FrozenHeaderTableResult(table, hasNonCorpusForms);
    }

    /// <summary>
    /// Builds cloned (visible) row headers in a StackPanel.
    /// Populates the clonedCells list for height synchronization.
    /// </summary>
    static StackPanel BuildClonedRowHeaders(IReadOnlyList<string> headers, List<Border> clonedCells)
    {
        var panel = new StackPanel()
            .Orientation(Orientation.Vertical);

        for (int row = 0; row < headers.Count; row++)
        {
            bool isLastRow = row == headers.Count - 1;
            var cell = BuildRowHeaderCell(headers[row], isGhost: false, isLastRow: isLastRow);
            clonedCells.Add(cell);
            panel.Children.Add(cell);
        }

        return panel;
    }

    /// <summary>
    /// Builds the body grid with ghost row headers (transparent) in column 0 and data cells in columns 1-N.
    /// Ghost cells sync their heights to cloned cells via SizeChanged.
    /// </summary>
    static (Grid grid, double rowHeaderWidth, bool hasNonCorpusForms) BuildBodyGrid(
        IReadOnlyList<string> rowHeaders,
        IReadOnlyList<IReadOnlyList<TableCell>> cells,
        int columnCount,
        List<Border> clonedCells)
    {
        int rowCount = rowHeaders.Count;
        bool hasNonCorpusForms = false;

        // Row definitions: one per data row
        var rowDefs = string.Join(",", Enumerable.Repeat("Auto", rowCount));

        // Column definitions: fixed width for ghost row header, then fixed width for each data column
        var colDefs = $"{RowHeaderWidth}," + string.Join(",", Enumerable.Repeat($"{CellWidth}", columnCount));

        var grid = new Grid()
            .RowDefinitions(rowDefs)
            .ColumnDefinitions(colDefs);

        // Add ghost row headers (column 0) - transparent but participate in layout
        for (int row = 0; row < rowCount; row++)
        {
            bool isLastRow = row == rowCount - 1;
            var ghostCell = BuildRowHeaderCell(rowHeaders[row], isGhost: true, isLastRow: isLastRow);
            ghostCell.Grid(row: row, column: 0);
            grid.Children.Add(ghostCell);

            // Sync height from ghost to cloned cell
            var clonedCell = clonedCells[row];
            ghostCell.SizeChanged += (_, e) =>
            {
                if (e.NewSize.Height > 0)
                {
                    clonedCell.Height(e.NewSize.Height);
                }
            };
        }

        // Add data cells (columns 1-N)
        for (int row = 0; row < cells.Count; row++)
        {
            bool isLastRow = row == cells.Count - 1;
            var rowCells = cells[row];
            for (int col = 0; col < rowCells.Count; col++)
            {
                bool isLastColumn = col == rowCells.Count - 1;

                // Check for non-corpus forms
                if (rowCells[col].Forms.Any(f => !f.InCorpus))
                    hasNonCorpusForms = true;

                var cell = BuildDataCell(rowCells[col], isLastRow, isLastColumn);
                cell.Grid(row: row, column: col + 1); // +1 because column 0 is row headers
                grid.Children.Add(cell);
            }
        }

        return (grid, RowHeaderWidth, hasNonCorpusForms);
    }

    static StackPanel BuildColumnHeaders(IReadOnlyList<string> headers)
    {
        var panel = new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"));

        for (int col = 0; col < headers.Count; col++)
        {
            bool isLastColumn = col == headers.Count - 1;
            var cell = new Border()
                .Width(CellWidth)
                .Padding(8, 6)
                .BorderBrush(ThemeResource.Get<Brush>("NeutralGrayBrush"))
                // Bottom border always, right border only between columns (not on last)
                .BorderThickness(0, 0, isLastColumn ? 0 : 1, 1)
                .Child(
                    RegularText()
                        .Text(headers[col])
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .Foreground(ThemeResource.Get<Brush>("AccentBrush"))
                        .TextAlignment(TextAlignment.Center)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                );
            panel.Children.Add(cell);
        }

        return panel;
    }

    static Border BuildRowHeaderCell(string header, bool isGhost, bool isLastRow)
    {
        var border = new Border()
            .Width(RowHeaderWidth)
            .MinHeight(CellMinHeight)
            .Padding(8, 6);

        if (isGhost)
        {
            // Ghost cell: transparent, no content, just occupies space for layout
            border.Background(new SolidColorBrush(Colors.Transparent));
        }
        else
        {
            // Visible cell: has background, border, and content
            // Right border always (to separate from data), bottom border only between rows (not on last)
            border
                .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
                .BorderBrush(ThemeResource.Get<Brush>("NeutralGrayBrush"))
                .BorderThickness(0, 0, 1, isLastRow ? 0 : 1)
                .Child(
                    RegularText()
                        .Text(header)
                        .FontWeight(Microsoft.UI.Text.FontWeights.SemiBold)
                        .Foreground(ThemeResource.Get<Brush>("AccentBrush"))
                        .HorizontalAlignment(HorizontalAlignment.Right)
                        .VerticalAlignment(VerticalAlignment.Center)
                );
        }

        return border;
    }

    static Border BuildDataCell(TableCell cell, bool isLastRow, bool isLastColumn)
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
                textBlock.Foreground(ThemeResource.Get<Brush>("NeutralGrayBrush"));
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

        // Right border only between columns (not on last), bottom border only between rows (not on last)
        return new Border()
            .MinHeight(CellMinHeight)
            .Width(CellWidth)
            .Padding(8, 4)
            .BorderBrush(ThemeResource.Get<Brush>("NeutralGrayBrush"))
            .BorderThickness(0, 0, isLastColumn ? 0 : 1, isLastRow ? 0 : 1)
            .Background(ThemeResource.Get<Brush>("SurfaceBrush"))
            .Child(panel);
    }

    const double CellWidth = 120;
    const double RowHeaderWidth = 70;
    const double CellMinHeight = 36;
}
