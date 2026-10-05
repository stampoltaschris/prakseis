using System.Windows;
using System.Windows.Controls;
using ExpressionSolverApp.Core.Models;

namespace ExpressionSolverApp.UI.Controls;

public partial class VirtualKeyboardControl : UserControl
{
    public event Action<string>? KeyPressed;
    public event Action? BackspacePressed;
    public event Action? ClearPressed;

    public VirtualKeyboardControl()
    {
        InitializeComponent();
    }

    public void UpdateMode(PedagogyMode mode)
    {
        bool isLogical = mode == PedagogyMode.Logical;
        var visibility = isLogical ? Visibility.Visible : Visibility.Collapsed;

        BtnEqual.Visibility = visibility;
        BtnNotEqual.Visibility = visibility;
        BtnLess.Visibility = visibility;
        BtnLessEq.Visibility = visibility;
        BtnGreater.Visibility = visibility;
        BtnGreaterEq.Visibility = visibility;
        BtnNot.Visibility = visibility;
        BtnAnd.Visibility = visibility;
        BtnOr.Visibility = visibility;
        BtnTrue.Visibility = visibility;
        BtnFalse.Visibility = visibility;
    }

    private void OnKeyClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string text)
        {
            KeyPressed?.Invoke(text);
        }
    }

    private void OnBackspaceClick(object sender, RoutedEventArgs e)
    {
        BackspacePressed?.Invoke();
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        ClearPressed?.Invoke();
    }
}
