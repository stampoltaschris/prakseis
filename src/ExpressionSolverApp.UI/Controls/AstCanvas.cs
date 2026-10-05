using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ExpressionSolverApp.Core.Models;

namespace ExpressionSolverApp.UI.Controls;

public class AstCanvas : Canvas
{
    public static readonly DependencyProperty AstRootProperty =
        DependencyProperty.Register(nameof(AstRoot), typeof(AstNode), typeof(AstCanvas), new PropertyMetadata(null, OnAstChanged));

    public static readonly DependencyProperty ActiveNodeIdProperty =
        DependencyProperty.Register(nameof(ActiveNodeId), typeof(string), typeof(AstCanvas), new PropertyMetadata(null, OnActiveNodeChanged));

    public AstNode? AstRoot
    {
        get => (AstNode?)GetValue(AstRootProperty);
        set => SetValue(AstRootProperty, value);
    }

    public string? ActiveNodeId
    {
        get => (string?)GetValue(ActiveNodeIdProperty);
        set => SetValue(ActiveNodeIdProperty, value);
    }

    private static void OnAstChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AstCanvas canvas) canvas.Redraw();
    }

    private static void OnActiveNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AstCanvas canvas) canvas.Redraw();
    }

    public void Redraw()
    {
        Children.Clear();
        if (AstRoot == null || ActualWidth <= 0 || ActualHeight <= 0) return;

        // Compute layout
        var layoutMap = new Dictionary<AstNode, (double X, double Y)>();
        int maxDepth = GetDepth(AstRoot);
        double levelHeight = Math.Max(55, Math.Min(80, (ActualHeight - 60) / Math.Max(1, maxDepth)));

        ComputePositions(AstRoot, 0, ActualWidth, 40, levelHeight, layoutMap);

        // First draw connection lines
        DrawLines(AstRoot, layoutMap);

        // Then draw nodes
        DrawNodes(AstRoot, layoutMap);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        Redraw();
    }

    private int GetDepth(AstNode node)
    {
        if (node == null) return 0;
        if (node is UnaryOpNode u) return 1 + GetDepth(u.Operand);
        if (node is BinaryOpNode b) return 1 + Math.Max(GetDepth(b.Left), GetDepth(b.Right));
        return 1;
    }

    private void ComputePositions(AstNode node, double left, double right, double y, double levelHeight, Dictionary<AstNode, (double X, double Y)> map)
    {
        if (node == null) return;
        double x = (left + right) / 2.0;
        map[node] = (x, y);

        if (node is UnaryOpNode u)
        {
            ComputePositions(u.Operand, left, right, y + levelHeight, levelHeight, map);
        }
        else if (node is BinaryOpNode b)
        {
            ComputePositions(b.Left, left, x, y + levelHeight, levelHeight, map);
            ComputePositions(b.Right, x, right, y + levelHeight, levelHeight, map);
        }
    }

    private void DrawLines(AstNode node, Dictionary<AstNode, (double X, double Y)> map)
    {
        if (node == null) return;
        var (x, y) = map[node];

        if (node is UnaryOpNode u)
        {
            var (cx, cy) = map[u.Operand];
            DrawLine(x, y + 18, cx, cy - 18);
            DrawLines(u.Operand, map);
        }
        else if (node is BinaryOpNode b)
        {
            var (lx, ly) = map[b.Left];
            DrawLine(x, y + 18, lx, ly - 18);
            DrawLines(b.Left, map);

            var (rx, ry) = map[b.Right];
            DrawLine(x, y + 18, rx, ry - 18);
            DrawLines(b.Right, map);
        }
    }

    private void DrawLine(double x1, double y1, double x2, double y2)
    {
        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            StrokeThickness = 2
        };
        Children.Add(line);
    }

    private void DrawNodes(AstNode node, Dictionary<AstNode, (double X, double Y)> map)
    {
        if (node == null) return;
        var (x, y) = map[node];
        bool isActive = node.Id == ActiveNodeId;

        double width = Math.Max(50, node.DisplayText.Length * 11 + 18);
        double height = 36;

        var border = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(isActive ? 3 : 1.5),
            BorderBrush = isActive
                ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) // Glowing Amber for active node
                : new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            Background = isActive
                ? new SolidColorBrush(Color.FromArgb(230, 217, 119, 6)) // Bright accent background
                : (node is NumberNode || node is BooleanNode
                    ? new SolidColorBrush(Color.FromRgb(30, 41, 59))
                    : new SolidColorBrush(Color.FromRgb(15, 23, 42)))
        };

        var textBlock = new TextBlock
        {
            Text = node.DisplayText,
            Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
            FontWeight = isActive ? FontWeights.Bold : FontWeights.SemiBold,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        border.Child = textBlock;

        Canvas.SetLeft(border, x - width / 2.0);
        Canvas.SetTop(border, y - height / 2.0);

        Children.Add(border);

        if (node is UnaryOpNode u) DrawNodes(u.Operand, map);
        if (node is BinaryOpNode b)
        {
            DrawNodes(b.Left, map);
            DrawNodes(b.Right, map);
        }
    }
}
