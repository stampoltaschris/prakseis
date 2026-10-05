using ExpressionSolverApp.Core.Models;

namespace ExpressionSolverApp.Core.Parsing;

public class ShuntingYardParser
{
    public static AstNode Parse(List<Token> tokens)
    {
        if (tokens == null || tokens.Count == 0)
        {
            throw new Exception("Η παράσταση είναι κενή.");
        }

        var outputStack = new Stack<AstNode>();
        var operatorStack = new Stack<Token>();

        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                    outputStack.Push(new NumberNode(token.NumberValue));
                    break;

                case TokenType.Boolean:
                    outputStack.Push(new BooleanNode(token.BoolValue));
                    break;

                case TokenType.LeftParen:
                    operatorStack.Push(token);
                    break;

                case TokenType.RightParen:
                    while (operatorStack.Count > 0 && operatorStack.Peek().Type != TokenType.LeftParen)
                    {
                        PopOperatorToAst(operatorStack, outputStack);
                    }

                    if (operatorStack.Count == 0)
                    {
                        throw new Exception("Μη ισορροπημένες παρενθέσεις: Λείπει η αριστερή παρένθεση '(' .");
                    }

                    // Pop left paren
                    operatorStack.Pop();
                    break;

                case TokenType.Operator:
                    var op1 = token.OpType!.Value;

                    while (operatorStack.Count > 0 && operatorStack.Peek().Type == TokenType.Operator)
                    {
                        var op2 = operatorStack.Peek().OpType!.Value;

                        if (ShouldPopOperator(op1, op2))
                        {
                            PopOperatorToAst(operatorStack, outputStack);
                        }
                        else
                        {
                            break;
                        }
                    }

                    operatorStack.Push(token);
                    break;
            }
        }

        while (operatorStack.Count > 0)
        {
            var top = operatorStack.Peek();
            if (top.Type == TokenType.LeftParen || top.Type == TokenType.RightParen)
            {
                throw new Exception("Μη ισορροπημένες παρενθέσεις: Λείπει δεξιά παρένθεση ')' .");
            }

            PopOperatorToAst(operatorStack, outputStack);
        }

        if (outputStack.Count != 1)
        {
            throw new Exception("Συντακτικό σφάλμα στην παράσταση: Λανθασμένη διάταξη τελεστών και τελεστέων.");
        }

        return outputStack.Pop();
    }

    private static bool ShouldPopOperator(OperatorType op1, OperatorType op2)
    {
        int prec1 = GetPrecedence(op1);
        int prec2 = GetPrecedence(op2);

        if (IsRightAssociative(op1))
        {
            return prec1 < prec2;
        }
        else
        {
            return prec1 <= prec2;
        }
    }

    public static int GetPrecedence(OperatorType op)
    {
        return op switch
        {
            // Unary operators highest (except paren)
            OperatorType.UnaryMinus => 7,
            OperatorType.Power => 6,
            OperatorType.Multiply or OperatorType.Divide or OperatorType.IntDivide or OperatorType.Modulo => 5,
            OperatorType.Add or OperatorType.Subtract => 4,
            OperatorType.Equal or OperatorType.NotEqual or OperatorType.LessThan or OperatorType.LessOrEqual or OperatorType.GreaterThan or OperatorType.GreaterOrEqual => 3,
            OperatorType.Not => 2,
            OperatorType.And => 1,
            OperatorType.Or => 0,
            _ => 0
        };
    }

    public static bool IsRightAssociative(OperatorType op)
    {
        return op == OperatorType.Power || op == OperatorType.UnaryMinus || op == OperatorType.Not;
    }

    public static bool IsUnary(OperatorType op)
    {
        return op == OperatorType.UnaryMinus || op == OperatorType.Not;
    }

    private static void PopOperatorToAst(Stack<Token> operatorStack, Stack<AstNode> outputStack)
    {
        var opToken = operatorStack.Pop();
        var opType = opToken.OpType!.Value;

        if (IsUnary(opType))
        {
            if (outputStack.Count < 1)
            {
                throw new Exception($"Ο μοναδιαίος τελεστής '{opToken.Text}' στη θέση {opToken.Position + 1} δεν έχει ορισμένο τελεστέο.");
            }
            var operand = outputStack.Pop();
            outputStack.Push(new UnaryOpNode(opType, operand));
        }
        else
        {
            if (outputStack.Count < 2)
            {
                throw new Exception($"Ο δυαδικός τελεστής '{opToken.Text}' στη θέση {opToken.Position + 1} δεν έχει επαρκείς τελεστέους.");
            }
            var right = outputStack.Pop();
            var left = outputStack.Pop();
            outputStack.Push(new BinaryOpNode(opType, left, right));
        }
    }
}
