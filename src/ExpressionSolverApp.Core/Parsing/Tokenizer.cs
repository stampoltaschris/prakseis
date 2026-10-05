using System.Globalization;
using System.Text;
using ExpressionSolverApp.Core.Models;

namespace ExpressionSolverApp.Core.Parsing;

public class Tokenizer
{
    public static List<Token> Tokenize(string expression, PedagogyMode mode)
    {
        var tokens = new List<Token>();
        int i = 0;
        int len = expression.Length;

        while (i < len)
        {
            char c = expression[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            // Number
            if (char.IsDigit(c) || (c == '.' && i + 1 < len && char.IsDigit(expression[i + 1])))
            {
                int start = i;
                var sb = new StringBuilder();
                bool hasDot = false;
                while (i < len && (char.IsDigit(expression[i]) || expression[i] == '.'))
                {
                    if (expression[i] == '.')
                    {
                        if (hasDot) break;
                        hasDot = true;
                    }
                    sb.Append(expression[i]);
                    i++;
                }
                string numStr = sb.ToString();
                if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                {
                    tokens.Add(new Token(TokenType.Number, numStr, start, numberValue: val));
                }
                else
                {
                    throw new Exception($"Μη έγκυρος αριθμός: '{numStr}' στη θέση {start + 1}.");
                }
                continue;
            }

            // Identifiers / Keywords (DIV, MOD, AND, OR, NOT, TRUE, FALSE)
            if (char.IsLetter(c))
            {
                int start = i;
                var sb = new StringBuilder();
                while (i < len && char.IsLetterOrDigit(expression[i]))
                {
                    sb.Append(expression[i]);
                    i++;
                }
                string word = sb.ToString();
                string wordUpper = word.ToUpperInvariant();

                switch (wordUpper)
                {
                    case "DIV":
                        tokens.Add(new Token(TokenType.Operator, wordUpper, start, opType: OperatorType.IntDivide));
                        break;
                    case "MOD":
                        tokens.Add(new Token(TokenType.Operator, wordUpper, start, opType: OperatorType.Modulo));
                        break;
                    case "AND":
                        CheckMode(mode, wordUpper, start);
                        tokens.Add(new Token(TokenType.Operator, wordUpper, start, opType: OperatorType.And));
                        break;
                    case "OR":
                        CheckMode(mode, wordUpper, start);
                        tokens.Add(new Token(TokenType.Operator, wordUpper, start, opType: OperatorType.Or));
                        break;
                    case "NOT":
                        CheckMode(mode, wordUpper, start);
                        tokens.Add(new Token(TokenType.Operator, wordUpper, start, opType: OperatorType.Not));
                        break;
                    case "TRUE":
                    case "ALITHIS":
                        tokens.Add(new Token(TokenType.Boolean, wordUpper, start, boolValue: true));
                        break;
                    case "FALSE":
                    case "PSEVDIS":
                        tokens.Add(new Token(TokenType.Boolean, wordUpper, start, boolValue: false));
                        break;
                    default:
                        throw new Exception($"Άγνωστη λέξη-κλειδί ή μεταβλητή: '{word}' στη θέση {start + 1}.");
                }
                continue;
            }

            // Parentheses
            if (c == '(')
            {
                tokens.Add(new Token(TokenType.LeftParen, "(", i));
                i++;
                continue;
            }
            if (c == ')')
            {
                tokens.Add(new Token(TokenType.RightParen, ")", i));
                i++;
                continue;
            }

            // Operators
            int opStart = i;
            if (c == '^')
            {
                tokens.Add(new Token(TokenType.Operator, "^", i, opType: OperatorType.Power));
                i++;
                continue;
            }
            if (c == '*')
            {
                tokens.Add(new Token(TokenType.Operator, "*", i, opType: OperatorType.Multiply));
                i++;
                continue;
            }
            if (c == '/')
            {
                tokens.Add(new Token(TokenType.Operator, "/", i, opType: OperatorType.Divide));
                i++;
                continue;
            }
            if (c == '+')
            {
                tokens.Add(new Token(TokenType.Operator, "+", i, opType: OperatorType.Add));
                i++;
                continue;
            }
            if (c == '-')
            {
                // Check if unary minus
                bool isUnary = tokens.Count == 0 ||
                               tokens[^1].Type == TokenType.LeftParen ||
                               tokens[^1].Type == TokenType.Operator;

                if (isUnary)
                {
                    tokens.Add(new Token(TokenType.Operator, "-", i, opType: OperatorType.UnaryMinus));
                }
                else
                {
                    tokens.Add(new Token(TokenType.Operator, "-", i, opType: OperatorType.Subtract));
                }
                i++;
                continue;
            }

            // Relational operators
            if (c == '<')
            {
                CheckMode(mode, "<", opStart);
                if (i + 1 < len && expression[i + 1] == '>')
                {
                    tokens.Add(new Token(TokenType.Operator, "<>", opStart, opType: OperatorType.NotEqual));
                    i += 2;
                }
                else if (i + 1 < len && expression[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.Operator, "<=", opStart, opType: OperatorType.LessOrEqual));
                    i += 2;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Operator, "<", opStart, opType: OperatorType.LessThan));
                    i++;
                }
                continue;
            }
            if (c == '>')
            {
                CheckMode(mode, ">", opStart);
                if (i + 1 < len && expression[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.Operator, ">=", opStart, opType: OperatorType.GreaterOrEqual));
                    i += 2;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Operator, ">", opStart, opType: OperatorType.GreaterThan));
                    i++;
                }
                continue;
            }
            if (c == '=')
            {
                CheckMode(mode, "=", opStart);
                tokens.Add(new Token(TokenType.Operator, "=", opStart, opType: OperatorType.Equal));
                i++;
                continue;
            }

            throw new Exception($"Μη έγκυρος χαρακτήρας: '{c}' στη θέση {i + 1}.");
        }

        return tokens;
    }

    private static void CheckMode(PedagogyMode mode, string opName, int pos)
    {
        if (mode == PedagogyMode.Arithmetic)
        {
            throw new Exception($"Ο τελεστής '{opName}' στη θέση {pos + 1} δεν επιτρέπεται στην Αριθμητική Λειτουργία. Αλλάξτε σε Πλήρη/Λογική Λειτουργία.");
        }
    }
}
