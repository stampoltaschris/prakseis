using ExpressionSolverApp.Core.Evaluation;
using ExpressionSolverApp.Core.Models;
using ExpressionSolverApp.Core.Parsing;
using Xunit;

namespace ExpressionSolverApp.Core.Tests;

public class EvaluatorAndParserTests
{
    [Theory]
    [InlineData("3 + 5 * 2", 13)]
    [InlineData("(3 + 5) * 2", 16)]
    [InlineData("2 ^ 3 ^ 2", 512)] // Right-associative: 2 ^ (3 ^ 2) = 2 ^ 9 = 512
    [InlineData("10 DIV 3", 3)]
    [InlineData("10 MOD 3", 1)]
    [InlineData("10 / 4", 2.5)]
    [InlineData("-5 + 3", -2)]
    [InlineData("- - 5", 5)]
    public void TestArithmeticExpressions(string expr, double expected)
    {
        var tokens = Tokenizer.Tokenize(expr, PedagogyMode.Arithmetic);
        var ast = ShuntingYardParser.Parse(tokens);
        var (result, steps, _) = ExpressionEvaluator.EvaluateStepByStep(ast);

        Assert.Equal(expected, Convert.ToDouble(result));
        Assert.NotEmpty(steps);
    }

    [Theory]
    [InlineData("5 > 3 AND 2 < 4", true)]
    [InlineData("5 = 5 OR 3 <> 3", true)]
    [InlineData("NOT (5 <= 2)", true)]
    [InlineData("NOT NOT TRUE", true)]
    [InlineData("3 + 2 = 5 AND NOT FALSE", true)]
    public void TestLogicalExpressions(string expr, bool expected)
    {
        var tokens = Tokenizer.Tokenize(expr, PedagogyMode.Logical);
        var ast = ShuntingYardParser.Parse(tokens);
        var (result, steps, _) = ExpressionEvaluator.EvaluateStepByStep(ast);

        Assert.Equal(expected, Convert.ToBoolean(result));
        Assert.NotEmpty(steps);
    }

    [Fact]
    public void TestStepSnapshotActiveNodeIdPresentInAst()
    {
        var tokens = Tokenizer.Tokenize("3 + 5 * 2", PedagogyMode.Arithmetic);
        var ast = ShuntingYardParser.Parse(tokens);
        var (_, steps, _) = ExpressionEvaluator.EvaluateStepByStep(ast);

        Assert.NotEmpty(steps);
        foreach (var step in steps)
        {
            Assert.NotNull(step.ActiveNodeId);
            bool found = NodeExistsInTree(step.CurrentAstRoot, step.ActiveNodeId);
            Assert.True(found, $"ActiveNodeId {step.ActiveNodeId} should exist in CurrentAstRoot for step {step.StepIndex}");
        }
    }

    private bool NodeExistsInTree(AstNode node, string id)
    {
        if (node == null) return false;
        if (node.Id == id) return true;
        if (node is UnaryOpNode u) return NodeExistsInTree(u.Operand, id);
        if (node is BinaryOpNode b) return NodeExistsInTree(b.Left, id) || NodeExistsInTree(b.Right, id);
        return false;
    }

    [Fact]
    [Trait("Category", "ArithmeticValidation")]
    public void TestArithmeticModeRejectsLogicalOperators()
    {
        Assert.Throws<Exception>(() => Tokenizer.Tokenize("5 > 3", PedagogyMode.Arithmetic));
        Assert.Throws<Exception>(() => Tokenizer.Tokenize("TRUE AND FALSE", PedagogyMode.Arithmetic));
    }

    [Fact]
    [Trait("Category", "SyntaxValidation")]
    public void TestUnbalancedParentheses()
    {
        var tokens = Tokenizer.Tokenize("(3 + 5", PedagogyMode.Arithmetic);
        Assert.Throws<Exception>(() => ShuntingYardParser.Parse(tokens));
    }

    [Fact]
    [Trait("Category", "RuntimeValidation")]
    public void TestDivisionByZero()
    {
        var tokens = Tokenizer.Tokenize("10 / 0", PedagogyMode.Arithmetic);
        var ast = ShuntingYardParser.Parse(tokens);
        Assert.Throws<DivideByZeroException>(() => ExpressionEvaluator.EvaluateStepByStep(ast));
    }
}
