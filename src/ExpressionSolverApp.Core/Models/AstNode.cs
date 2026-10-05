namespace ExpressionSolverApp.Core.Models;

public enum NodeType
{
    Number,
    Boolean,
    BinaryOp,
    UnaryOp
}

public abstract class AstNode
{
    public string Id { get; set; }

    protected AstNode(string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString("N")[..8];
    }

    public abstract NodeType Type { get; }
    public abstract string DisplayText { get; }
}

public class NumberNode : AstNode
{
    public override NodeType Type => NodeType.Number;
    public double Value { get; }
    public override string DisplayText => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public NumberNode(double value, string? id = null) : base(id)
    {
        Value = value;
    }
}

public class BooleanNode : AstNode
{
    public override NodeType Type => NodeType.Boolean;
    public bool Value { get; }
    public override string DisplayText => Value ? "TRUE" : "FALSE";

    public BooleanNode(bool value, string? id = null) : base(id)
    {
        Value = value;
    }
}

public class UnaryOpNode : AstNode
{
    public override NodeType Type => NodeType.UnaryOp;
    public OperatorType Operator { get; }
    public AstNode Operand { get; }

    public override string DisplayText => Operator switch
    {
        OperatorType.UnaryMinus => "-",
        OperatorType.Not => "NOT",
        _ => Operator.ToString()
    };

    public UnaryOpNode(OperatorType op, AstNode operand, string? id = null) : base(id)
    {
        Operator = op;
        Operand = operand;
    }
}

public class BinaryOpNode : AstNode
{
    public override NodeType Type => NodeType.BinaryOp;
    public OperatorType Operator { get; }
    public AstNode Left { get; }
    public AstNode Right { get; }

    public override string DisplayText => Operator switch
    {
        OperatorType.Power => "^",
        OperatorType.Multiply => "*",
        OperatorType.Divide => "/",
        OperatorType.IntDivide => "DIV",
        OperatorType.Modulo => "MOD",
        OperatorType.Add => "+",
        OperatorType.Subtract => "-",
        OperatorType.Equal => "=",
        OperatorType.NotEqual => "<>",
        OperatorType.LessThan => "<",
        OperatorType.LessOrEqual => "<=",
        OperatorType.GreaterThan => ">",
        OperatorType.GreaterOrEqual => ">=",
        OperatorType.And => "AND",
        OperatorType.Or => "OR",
        _ => Operator.ToString()
    };

    public BinaryOpNode(OperatorType op, AstNode left, AstNode right, string? id = null) : base(id)
    {
        Operator = op;
        Left = left;
        Right = right;
    }
}
