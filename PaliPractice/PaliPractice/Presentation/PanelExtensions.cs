namespace PaliPractice.Presentation;

/// <summary>
/// Panel composition without Uno C# Markup's ResourceParent back references.
/// Uno 6.7's theme-enter walk follows those references into sibling controls,
/// causing excessive traversal while the controls are detached.
/// Theme bindings continue to resolve through the normal visual tree.
/// </summary>
public static class PanelExtensions
{
    public static TPanel AddChildren<TPanel>(this TPanel panel, params UIElement?[] children)
        where TPanel : Panel
    {
        foreach (var child in children)
        {
            if (child is not null)
                panel.Children.Add(child);
        }

        return panel;
    }
}
