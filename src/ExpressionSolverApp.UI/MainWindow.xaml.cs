using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ExpressionSolverApp.Core.Evaluation;
using ExpressionSolverApp.Core.Models;
using ExpressionSolverApp.Core.Parsing;

namespace ExpressionSolverApp.UI;

public partial class MainWindow : Window
{
    private PedagogyMode _currentMode = PedagogyMode.Arithmetic;
    private List<StepSnapshot> _steps = new();
    private AstNode? _initialAst;
    private int _currentStepIndex = -1;
    private DispatcherTimer _playTimer;

    public MainWindow()
    {
        InitializeComponent();

        _playTimer = new DispatcherTimer();
        _playTimer.Interval = TimeSpan.FromSeconds(1.5);
        _playTimer.Tick += OnPlayTimerTick;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        PopulatePresets();
        UpdateModeUI();
        TxtExpression.Text = "3 + 5 * 2 ^ 3";
        SolveExpression();
    }

    private void PopulatePresets()
    {
        CmbPresets.Items.Clear();
        if (_currentMode == PedagogyMode.Arithmetic)
        {
            CmbPresets.Items.Add("3 + 5 * 2 ^ 3");
            CmbPresets.Items.Add("(12 - 4) / (2 * 2)");
            CmbPresets.Items.Add("10 DIV 3 + 10 MOD 3");
            CmbPresets.Items.Add("(3 + 5 * (2 + 4)) ^ 2");
            CmbPresets.Items.Add("100 / 5 / 2");
        }
        else
        {
            CmbPresets.Items.Add("5 > 3 AND 2 < 4");
            CmbPresets.Items.Add("10 DIV 3 = 3 OR NOT (5 > 2)");
            CmbPresets.Items.Add("3 + 2 * 4 >= 10 AND NOT FALSE");
            CmbPresets.Items.Add("(5 <> 2) AND (10 MOD 3 = 1)");
            CmbPresets.Items.Add("NOT (TRUE OR FALSE) AND 5 > 2");
        }
        CmbPresets.SelectedIndex = 0;
    }

    private void OnModeChanged(object sender, RoutedEventArgs e)
    {
        if (RbArithmeticMode == null || RbLogicalMode == null) return;

        _currentMode = RbArithmeticMode.IsChecked == true ? PedagogyMode.Arithmetic : PedagogyMode.Logical;
        UpdateModeUI();
        PopulatePresets();
    }

    private void UpdateModeUI()
    {
        if (VirtualKeyboard != null)
        {
            VirtualKeyboard.UpdateMode(_currentMode);
        }
    }

    private void OnPresetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPresets.SelectedItem is string preset)
        {
            TxtExpression.Text = preset;
            SolveExpression();
        }
    }

    private void OnSolveClick(object sender, RoutedEventArgs e)
    {
        SolveExpression();
    }

    private void OnExpressionKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SolveExpression();
        }
    }

    private void SolveExpression()
    {
        StopAnimation();
        string input = TxtExpression.Text.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            SetValidationStatus("⚠️ Εισάγετε μια παράσταση.", false);
            return;
        }

        try
        {
            var tokens = Tokenizer.Tokenize(input, _currentMode);
            var ast = ShuntingYardParser.Parse(tokens);
            var (finalResult, steps, originalAst) = ExpressionEvaluator.EvaluateStepByStep(ast);

            _initialAst = originalAst;
            _steps = steps;

            SetValidationStatus($"✓ Έγκυρη παράσταση! Αποτέλεσμα: {FormatResult(finalResult)}", true);

            SldSteps.Maximum = _steps.Count;
            SldSteps.Value = 0;

            LstSteps.ItemsSource = _steps;

            if (_steps.Count > 0)
            {
                GoToStep(0);
            }
            else
            {
                // Single node expression (e.g. "42")
                AstCanvasControl.AstRoot = originalAst;
                AstCanvasControl.ActiveNodeId = null;
                TxtStepCounter.Text = "(Αρχική Τιμή)";
                TxtRuleCategory.Text = "Τελική Τιμή";
                TxtExplanation.Text = $"Η παράσταση είναι ήδη σε τελική μορφή: {FormatResult(finalResult)}";
                TxtCurrentExpr.Text = $"Αποτέλεσμα: {FormatResult(finalResult)}";
            }
        }
        catch (Exception ex)
        {
            _steps.Clear();
            _initialAst = null;
            AstCanvasControl.AstRoot = null;
            LstSteps.ItemsSource = null;
            TxtStepCounter.Text = "(Σφάλμα)";
            TxtRuleCategory.Text = "Συντακτικό / Λογικό Σφάλμα";
            TxtExplanation.Text = ex.Message;
            TxtCurrentExpr.Text = "Παράσταση: Σφάλμα";
            SetValidationStatus($"❌ Σφάλμα: {ex.Message}", false);
        }
    }

    private string FormatResult(object res)
    {
        if (res is bool b) return b ? "TRUE" : "FALSE";
        return Convert.ToDouble(res).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void SetValidationStatus(string message, bool isValid)
    {
        TxtValidationStatus.Text = message;
        if (isValid)
        {
            BadgeValidation.Background = new SolidColorBrush(Color.FromRgb(6, 78, 59));
            TxtValidationStatus.Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153));
        }
        else
        {
            BadgeValidation.Background = new SolidColorBrush(Color.FromRgb(127, 29, 29));
            TxtValidationStatus.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
        }
    }

    private void GoToStep(int index)
    {
        if (_steps == null || _steps.Count == 0) return;

        if (index < 0) index = 0;
        if (index >= _steps.Count) index = _steps.Count - 1;

        _currentStepIndex = index;
        SldSteps.Value = index;

        var step = _steps[index];
        AstCanvasControl.AstRoot = step.CurrentAstRoot;
        AstCanvasControl.ActiveNodeId = step.ActiveNodeId;

        TxtStepCounter.Text = $"(Βήμα {index + 1} / {_steps.Count})";
        TxtRuleCategory.Text = step.RuleCategory;
        TxtExplanation.Text = step.ExplanationText;
        TxtCurrentExpr.Text = $"Μορφή Παράστασης: {step.ExpressionText}";

        LstSteps.SelectedIndex = index;
    }

    private void OnStepSelected(object sender, SelectionChangedEventArgs e)
    {
        if (LstSteps.SelectedIndex >= 0 && LstSteps.SelectedIndex != _currentStepIndex)
        {
            GoToStep(LstSteps.SelectedIndex);
        }
    }

    private void OnStepSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        int targetStep = (int)e.NewValue;
        if (targetStep != _currentStepIndex && _steps != null && targetStep >= 0 && targetStep < _steps.Count)
        {
            GoToStep(targetStep);
        }
    }

    private void OnFirstStepClick(object sender, RoutedEventArgs e)
    {
        StopAnimation();
        GoToStep(0);
    }

    private void OnPrevStepClick(object sender, RoutedEventArgs e)
    {
        StopAnimation();
        GoToStep(_currentStepIndex - 1);
    }

    private void OnNextStepClick(object sender, RoutedEventArgs e)
    {
        StopAnimation();
        GoToStep(_currentStepIndex + 1);
    }

    private void OnLastStepClick(object sender, RoutedEventArgs e)
    {
        StopAnimation();
        GoToStep(_steps.Count - 1);
    }

    private void OnPlayPauseClick(object sender, RoutedEventArgs e)
    {
        if (_playTimer.IsEnabled)
        {
            StopAnimation();
        }
        else
        {
            StartAnimation();
        }
    }

    private void StartAnimation()
    {
        if (_steps == null || _steps.Count == 0) return;

        if (_currentStepIndex >= _steps.Count - 1)
        {
            _currentStepIndex = -1;
        }

        double speedFactor = SldSpeed != null ? SldSpeed.Value : 1.0;
        _playTimer.Interval = TimeSpan.FromSeconds(1.8 / speedFactor);
        _playTimer.Start();
        BtnPlayPause.Content = "⏸ Παύση";
    }

    private void StopAnimation()
    {
        _playTimer.Stop();
        BtnPlayPause.Content = "▶ Αναπαραγωγή";
    }

    private void OnPlayTimerTick(object? sender, EventArgs e)
    {
        if (_steps == null || _steps.Count == 0)
        {
            StopAnimation();
            return;
        }

        if (_currentStepIndex < _steps.Count - 1)
        {
            GoToStep(_currentStepIndex + 1);
        }
        else
        {
            StopAnimation();
        }
    }

    private void OnToggleKeyboardClick(object sender, RoutedEventArgs e)
    {
        if (GridVirtualKeyboard.Visibility == Visibility.Visible)
        {
            GridVirtualKeyboard.Visibility = Visibility.Collapsed;
        }
        else
        {
            GridVirtualKeyboard.Visibility = Visibility.Visible;
        }
    }

    private void OnVirtualKeyPressed(string text)
    {
        TxtExpression.Text += text;
        TxtExpression.CaretIndex = TxtExpression.Text.Length;
        TxtExpression.Focus();
    }

    private void OnVirtualBackspacePressed()
    {
        if (!string.IsNullOrEmpty(TxtExpression.Text))
        {
            TxtExpression.Text = TxtExpression.Text[..^1];
            TxtExpression.CaretIndex = TxtExpression.Text.Length;
            TxtExpression.Focus();
        }
    }

    private void OnVirtualClearPressed()
    {
        TxtExpression.Text = "";
        TxtExpression.Focus();
    }
}
