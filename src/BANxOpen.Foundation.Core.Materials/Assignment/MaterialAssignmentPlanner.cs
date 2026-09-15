using BANxOpen.Foundation.Core.RuleEngine;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

/// <summary>Runs the ordered gate rules for every body in the batch and produces a pure
/// <see cref="AssignmentPlan"/> — no side effects, no NX calls, nothing executed yet. A
/// <see cref="RuleDecision.Block"/> outcome short-circuits remaining rules for that body only; other
/// bodies in the batch are still evaluated independently.</summary>
public sealed class MaterialAssignmentPlanner : IMaterialAssignmentPlanner
{
    private readonly IReadOnlyList<IMaterialValidationRule> _gateRules;

    public MaterialAssignmentPlanner(IEnumerable<IMaterialValidationRule> gateRules) =>
        _gateRules = gateRules.OrderBy(r => r.Order).ToList();

    public AssignmentPlan Plan(MaterialAssignmentPlanningInput input)
    {
        var evaluations = new List<BodyAssignmentEvaluation>();

        foreach (var body in input.TargetBodies)
        {
            input.CurrentAssignments.TryGetValue(body.Id, out var currentAssignment);
            var context = new MaterialAssignmentRuleContext(
                input.RequestedMaterial, body, currentAssignment, input.TargetBodies);

            var outcomes = new List<RuleOutcome>();
            foreach (var rule in _gateRules)
            {
                var outcome = rule.Evaluate(context);
                outcomes.Add(outcome);
                if (outcome.Decision == RuleDecision.Block)
                    break;
            }

            evaluations.Add(new BodyAssignmentEvaluation(body.Id, outcomes));
        }

        return new AssignmentPlan(Guid.NewGuid().ToString("N"), input.RequestedMaterial.Id, evaluations);
    }
}
