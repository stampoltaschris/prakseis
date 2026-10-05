using ExpressionSolverApp.Core.Models;

namespace ExpressionSolverApp.Core.Evaluation;

public class StepSnapshot
{
    public int StepIndex { get; }
    public string ActiveNodeId { get; }
    public string RuleCategory { get; }
    public string ExplanationText { get; }
    public string ExpressionText { get; }
    public object EvaluatedResult { get; }
    public AstNode CurrentAstRoot { get; }

    public StepSnapshot(
        int stepIndex,
        string activeNodeId,
        string ruleCategory,
        string explanationText,
        string expressionText,
        object evaluatedResult,
        AstNode currentAstRoot)
    {
        StepIndex = stepIndex;
        ActiveNodeId = activeNodeId;
        RuleCategory = ruleCategory;
        ExplanationText = explanationText;
        ExpressionText = expressionText;
        EvaluatedResult = evaluatedResult;
        CurrentAstRoot = currentAstRoot;
    }
}
