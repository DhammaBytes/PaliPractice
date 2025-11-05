using System.Linq.Expressions;

namespace PaliPractice.Presentation.Bindings;

/// <summary>
/// Provides compile-time-safe binding helpers that turn expression trees into binding paths.
/// This defers VM resolution to runtime DataContext without capturing VM instances.
/// </summary>
public static class Bind
{
    /// <summary>
    /// Creates a binding from an expression like "vm => vm.Card.RankText" to path "Card.RankText"
    /// </summary>
    public static Microsoft.UI.Xaml.Data.Binding Path<TDC, TProp>(Expression<Func<TDC, TProp>> expr)
    {
        var path = string.Join(".", GetMemberPath(expr.Body).Reverse());

#if DEBUG
        if (string.IsNullOrEmpty(path))
        {
            System.Diagnostics.Debug.WriteLine($"[Bind.Path] Warning: Non-member expression detected: {expr.Body}");
            System.Diagnostics.Debug.WriteLine($"  Expression type: {expr.Body.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"  This may result in incorrect bindings. Use converters for complex expressions.");
        }
#endif

        return new Microsoft.UI.Xaml.Data.Binding
        {
            Path = new PropertyPath(path),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay
        };
    }

    /// <summary>
    /// Creates a two-way binding from an expression
    /// </summary>
    public static Microsoft.UI.Xaml.Data.Binding TwoWayPath<TDC, TProp>(Expression<Func<TDC, TProp>> expr)
    {
        var path = string.Join(".", GetMemberPath(expr.Body).Reverse());
        return new Microsoft.UI.Xaml.Data.Binding
        {
            Path = new PropertyPath(path),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay
        };
    }

    private static IEnumerable<string> GetMemberPath(Expression expr)
    {
        while (true)
        {
            // Unwrap Convert expressions (e.g., value-type to object)
            if (expr is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                expr = unary.Operand;
            }
            else if (expr is MemberExpression memberExpr)
            {
                yield return memberExpr.Member.Name;
                expr = memberExpr.Expression!;
            }
            else
            {
                break;
            }
        }
    }
}
