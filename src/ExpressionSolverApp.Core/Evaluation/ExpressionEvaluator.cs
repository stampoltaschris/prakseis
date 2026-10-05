using System.Globalization;
using ExpressionSolverApp.Core.Models;

namespace ExpressionSolverApp.Core.Evaluation;

public class ExpressionEvaluator
{
    public static (object FinalResult, List<StepSnapshot> Steps, AstNode OriginalAst) EvaluateStepByStep(AstNode root)
    {
        var steps = new List<StepSnapshot>();
        AstNode currentTree = DeepCopy(root);
        int stepCounter = 1;

        while (!IsValueNode(currentTree))
        {
            // Find lowest reducible sub-expression node (post-order traversal)
            AstNode? targetNode = FindTargetNode(currentTree);
            if (targetNode == null) break;

            object result = EvaluateSingleNode(targetNode);
            AstNode replacementNode = result is bool b
                ? new BooleanNode(b, id: targetNode.Id)
                : new NumberNode((double)result, id: targetNode.Id);

            string explanation = GeneratePedagogicalExplanation(targetNode, result);
            string ruleCategory = GetRuleCategory(targetNode);

            // Replace targetNode in currentTree with replacementNode
            currentTree = ReplaceNode(currentTree, targetNode.Id, replacementNode);
            string currentExprText = FormatExpression(currentTree);

            steps.Add(new StepSnapshot(
                stepIndex: stepCounter++,
                activeNodeId: targetNode.Id,
                ruleCategory: ruleCategory,
                explanationText: explanation,
                expressionText: currentExprText,
                evaluatedResult: result,
                currentAstRoot: DeepCopy(currentTree)
            ));
        }

        object finalVal = GetValue(currentTree);
        return (finalVal, steps, root);
    }

    private static bool IsValueNode(AstNode node)
    {
        return node is NumberNode || node is BooleanNode;
    }

    private static object GetValue(AstNode node)
    {
        if (node is NumberNode nn) return nn.Value;
        if (node is BooleanNode bn) return bn.Value;
        throw new Exception("Ο κόμβος δεν είναι τελική τιμή.");
    }

    private static AstNode? FindTargetNode(AstNode node)
    {
        if (node is UnaryOpNode u)
        {
            if (IsValueNode(u.Operand)) return u;
            return FindTargetNode(u.Operand);
        }
        if (node is BinaryOpNode b)
        {
            if (!IsValueNode(b.Left)) return FindTargetNode(b.Left);
            if (!IsValueNode(b.Right)) return FindTargetNode(b.Right);
            return b;
        }
        return null;
    }

    private static object EvaluateSingleNode(AstNode node)
    {
        if (node is UnaryOpNode u)
        {
            object val = GetValue(u.Operand);
            if (u.Operator == OperatorType.UnaryMinus)
            {
                if (val is double d) return -d;
                throw new Exception("Το αρνητικό πρόσημο (-) εφαρμόζεται μόνο σε αριθμούς.");
            }
            if (u.Operator == OperatorType.Not)
            {
                if (val is bool bVal) return !bVal;
                if (val is double dVal) return dVal == 0;
                throw new Exception("Ο λογικός τελεστής NOT εφαρμόζεται σε λογικές τιμές.");
            }
        }

        if (node is BinaryOpNode binNode)
        {
            object leftVal = GetValue(binNode.Left);
            object rightVal = GetValue(binNode.Right);

            // Arithmetic operators
            switch (binNode.Operator)
            {
                case OperatorType.Power:
                    EnsureNumbers(leftVal, rightVal, "^");
                    return Math.Pow((double)leftVal, (double)rightVal);

                case OperatorType.Multiply:
                    EnsureNumbers(leftVal, rightVal, "*");
                    return (double)leftVal * (double)rightVal;

                case OperatorType.Divide:
                    EnsureNumbers(leftVal, rightVal, "/");
                    double d2 = (double)rightVal;
                    if (d2 == 0) throw new DivideByZeroException("Σφάλμα: Διαίρεση με το μηδέν (/) !");
                    return (double)leftVal / d2;

                case OperatorType.IntDivide:
                    EnsureNumbers(leftVal, rightVal, "DIV");
                    long div2 = Convert.ToInt64((double)rightVal);
                    if (div2 == 0) throw new DivideByZeroException("Σφάλμα: Ακέραια διαίρεση (DIV) με το μηδέν!");
                    return (double)(Convert.ToInt64((double)leftVal) / div2);

                case OperatorType.Modulo:
                    EnsureNumbers(leftVal, rightVal, "MOD");
                    long mod2 = Convert.ToInt64((double)rightVal);
                    if (mod2 == 0) throw new DivideByZeroException("Σφάλμα: Υπόλοιπο διαίρεσης (MOD) με το μηδέν!");
                    return (double)(Convert.ToInt64((double)leftVal) % mod2);

                case OperatorType.Add:
                    EnsureNumbers(leftVal, rightVal, "+");
                    return (double)leftVal + (double)rightVal;

                case OperatorType.Subtract:
                    EnsureNumbers(leftVal, rightVal, "-");
                    return (double)leftVal - (double)rightVal;

                // Relational operators
                case OperatorType.Equal:
                    return Compare(leftVal, rightVal) == 0;

                case OperatorType.NotEqual:
                    return Compare(leftVal, rightVal) != 0;

                case OperatorType.LessThan:
                    EnsureNumbers(leftVal, rightVal, "<");
                    return (double)leftVal < (double)rightVal;

                case OperatorType.LessOrEqual:
                    EnsureNumbers(leftVal, rightVal, "<=");
                    return (double)leftVal <= (double)rightVal;

                case OperatorType.GreaterThan:
                    EnsureNumbers(leftVal, rightVal, ">");
                    return (double)leftVal > (double)rightVal;

                case OperatorType.GreaterOrEqual:
                    EnsureNumbers(leftVal, rightVal, ">=");
                    return (double)leftVal >= (double)rightVal;

                // Logical operators
                case OperatorType.And:
                    bool bLeft1 = ToBool(leftVal);
                    bool bRight1 = ToBool(rightVal);
                    return bLeft1 && bRight1;

                case OperatorType.Or:
                    bool bLeft2 = ToBool(leftVal);
                    bool bRight2 = ToBool(rightVal);
                    return bLeft2 || bRight2;
            }
        }

        throw new Exception("Άγνωστος τύπος κόμβου για υπολογισμό.");
    }

    private static void EnsureNumbers(object l, object r, string op)
    {
        if (l is double && r is double) return;
        throw new Exception($"Ο τελεστής '{op}' απαιτεί αριθμητικούς τελεστέους.");
    }

    private static bool ToBool(object obj)
    {
        if (obj is bool b) return b;
        if (obj is double d) return d != 0;
        throw new Exception("Αδυναμία μετατροπής σε λογική τιμή.");
    }

    private static int Compare(object l, object r)
    {
        if (l is double d1 && r is double d2) return d1.CompareTo(d2);
        if (l is bool b1 && r is bool b2) return b1.CompareTo(b2);
        if (l is double d3 && r is bool b3) return d3.CompareTo(b3 ? 1.0 : 0.0);
        if (l is bool b4 && r is double d4) return (b4 ? 1.0 : 0.0).CompareTo(d4);
        return 0;
    }

    private static string GetRuleCategory(AstNode node)
    {
        if (node is UnaryOpNode u)
        {
            return u.Operator == OperatorType.UnaryMinus ? "Μοναδιαίο Πρόσημο (-)" : "Λογική Άρνηση (NOT)";
        }
        if (node is BinaryOpNode b)
        {
            return b.Operator switch
            {
                OperatorType.Power => "1. Υψωση σε Δύναμη (^)",
                OperatorType.Multiply or OperatorType.Divide or OperatorType.IntDivide or OperatorType.Modulo => "2. Πολλαπλασιασμός / Διαίρεση / DIV / MOD",
                OperatorType.Add or OperatorType.Subtract => "3. Πρόσθεση / Αφαίρεση (+, -)",
                OperatorType.Equal or OperatorType.NotEqual or OperatorType.LessThan or OperatorType.LessOrEqual or OperatorType.GreaterThan or OperatorType.GreaterOrEqual => "4. Συγκριτικοί Τελεστές (=, <>, <, <=, >, >=)",
                OperatorType.And => "5. Λογικό AND (Και)",
                OperatorType.Or => "6. Λογικό OR (Ή)",
                _ => "Κανόνας Προτεραιότητας"
            };
        }
        return "Κανόνας Προτεραιότητας";
    }

    private static string GeneratePedagogicalExplanation(AstNode node, object result)
    {
        string resStr = result is bool bRes ? (bRes ? "TRUE" : "FALSE") : Convert.ToDouble(result).ToString(CultureInfo.InvariantCulture);

        if (node is UnaryOpNode u)
        {
            string opText = u.Operator == OperatorType.UnaryMinus ? "-" : "NOT";
            return $"Εκτελέστηκε ο μοναδιαίος τελεστής {opText} στο {FormatExpression(u.Operand)} -> Αποτέλεσμα: {resStr}";
        }
        if (node is BinaryOpNode b)
        {
            string lText = FormatExpression(b.Left);
            string rText = FormatExpression(b.Right);
            string opText = b.DisplayText;

            switch (b.Operator)
            {
                case OperatorType.Power:
                    return $"1. Προτεραιότητα Δυνάμεων: Υπολογίστηκε η δύναμη {lText} ^ {rText} = {resStr}";
                case OperatorType.Multiply:
                case OperatorType.Divide:
                case OperatorType.IntDivide:
                case OperatorType.Modulo:
                    return $"2. Προτεραιότητα Πολλαπλασιασμού/Διαίρεσης/DIV/MOD (από αριστερά προς δεξιά): Υπολογίστηκε {lText} {opText} {rText} = {resStr}";
                case OperatorType.Add:
                case OperatorType.Subtract:
                    return $"3. Προτεραιότητα Πρόσθεσης/Αφαίρεσης (από αριστερά προς δεξιά): Υπολογίστηκε {lText} {opText} {rText} = {resStr}";
                case OperatorType.Equal:
                case OperatorType.NotEqual:
                case OperatorType.LessThan:
                case OperatorType.LessOrEqual:
                case OperatorType.GreaterThan:
                case OperatorType.GreaterOrEqual:
                    return $"4. Προτεραιότητα Σύγκρισης: Αξιολογήθηκε η σχέση {lText} {opText} {rText} -> {resStr}";
                case OperatorType.And:
                    return $"5. Λογική Πράξη AND: Αξιολογήθηκε {lText} AND {rText} -> {resStr}";
                case OperatorType.Or:
                    return $"6. Λογική Πράξη OR: Αξιολογήθηκε {lText} OR {rText} -> {resStr}";
            }
        }
        return $"Εκτέλεση υπολογισμού: {FormatExpression(node)} = {resStr}";
    }

    public static string FormatExpression(AstNode node)
    {
        if (node is NumberNode nn) return nn.Value.ToString(CultureInfo.InvariantCulture);
        if (node is BooleanNode bn) return bn.Value ? "TRUE" : "FALSE";
        if (node is UnaryOpNode u)
        {
            return $"{u.DisplayText}({FormatExpression(u.Operand)})";
        }
        if (node is BinaryOpNode b)
        {
            return $"({FormatExpression(b.Left)} {b.DisplayText} {FormatExpression(b.Right)})";
        }
        return "";
    }

    private static AstNode ReplaceNode(AstNode tree, string targetId, AstNode replacement)
    {
        if (tree.Id == targetId) return replacement;

        if (tree is UnaryOpNode u)
        {
            var newOperand = ReplaceNode(u.Operand, targetId, replacement);
            return new UnaryOpNode(u.Operator, newOperand, id: u.Id);
        }
        if (tree is BinaryOpNode b)
        {
            var newLeft = ReplaceNode(b.Left, targetId, replacement);
            var newRight = ReplaceNode(b.Right, targetId, replacement);
            return new BinaryOpNode(b.Operator, newLeft, newRight, id: b.Id);
        }
        return tree;
    }

    private static AstNode DeepCopy(AstNode node)
    {
        if (node is NumberNode nn)
        {
            return new NumberNode(nn.Value, id: nn.Id);
        }
        if (node is BooleanNode bn)
        {
            return new BooleanNode(bn.Value, id: bn.Id);
        }
        if (node is UnaryOpNode u)
        {
            return new UnaryOpNode(u.Operator, DeepCopy(u.Operand), id: u.Id);
        }
        if (node is BinaryOpNode b)
        {
            return new BinaryOpNode(b.Operator, DeepCopy(b.Left), DeepCopy(b.Right), id: b.Id);
        }
        return node;
    }
}
