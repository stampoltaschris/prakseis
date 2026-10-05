namespace ExpressionSolverApp.Core.Models;

public enum PedagogyMode
{
    Arithmetic,
    Logical
}

public enum TokenType
{
    Number,
    Boolean,
    Operator,
    LeftParen,
    RightParen
}

public enum OperatorType
{
    // Arithmetic
    Power,          // ^
    Multiply,       // *
    Divide,         // /
    IntDivide,      // div / DIV
    Modulo,         // mod / MOD
    Add,            // +
    Subtract,       // -
    UnaryMinus,     // - (unary)

    // Relational
    Equal,          // =
    NotEqual,       // <>
    LessThan,       // <
    LessOrEqual,    // <=
    GreaterThan,    // >
    GreaterOrEqual, // >=

    // Logical
    Not,            // NOT
    And,            // AND
    Or              // OR
}

public class Token
{
    public TokenType Type { get; }
    public string Text { get; }
    public double NumberValue { get; }
    public bool BoolValue { get; }
    public OperatorType? OpType { get; }
    public int Position { get; }

    public Token(TokenType type, string text, int position, double numberValue = 0, bool boolValue = false, OperatorType? opType = null)
    {
        Type = type;
        Text = text;
        Position = position;
        NumberValue = numberValue;
        BoolValue = boolValue;
        OpType = opType;
    }

    public override string ToString() => $"{Type}({Text})";
}
