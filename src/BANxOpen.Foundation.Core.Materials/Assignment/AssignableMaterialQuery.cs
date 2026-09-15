using BANxOpen.Foundation.Contracts.Bodies;
using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Contracts.Materials;
using BANxOpen.Foundation.Core.Materials.Rules.Features;
using BANxOpen.Foundation.Core.RuleEngine;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

/// <summary>What one candidate material would do if assigned to the body, without assigning it.</summary>
public sealed record MaterialAssignability(
    Material Material,
    IReadOnlyList<RuleOutcome> Outcomes)
{
    public bool IsAssignable => !Outcomes.Any(o => o.Decision == RuleDecision.Block);

    public bool RequiresConfirmation =>
        IsAssignable && Outcomes.Any(o => o.Decision == RuleDecision.RequireConfirmation);

    public IReadOnlyList<RuleOutcome> BlockingOutcomes =>
        Outcomes.Where(o => o.Decision == RuleDecision.Block).ToList();

    /// <summary>Advisories that do not stop the assignment but should be shown to the user.</summary>
    public IReadOnlyList<string> Warnings =>
        Outcomes.Where(o => o.Decision == RuleDecision.Warn && !string.IsNullOrWhiteSpace(o.Message))
            .Select(o => o.Message!)
            .ToList();

    /// <summary>Why this material cannot go on the body, for a tooltip or an error line. Empty when it
    /// can.</summary>
    public string? BlockedReason =>
        BlockingOutcomes.Count == 0
            ? null
            : string.Join(" ", BlockingOutcomes.Select(o => o.Message).Where(m => !string.IsNullOrWhiteSpace(m)));
}

/// <summary>Answers "which materials may go on this body?" by planning each candidate through the very
/// same <see cref="IMaterialAssignmentPlanner"/> that gates a real assignment, and keeping the ones
/// nothing blocked.
///
/// That indirection is the whole point. The obvious implementation — re-evaluating the constraint
/// predicates here — would be a second copy of the decision, and the two copies would eventually
/// disagree: a material offered in a drop-down that is then refused on Apply, or worse, one quietly
/// omitted that was actually fine. Running the planner means the list a user picks from and the verdict
/// they get on Apply are produced by one implementation, including every non-constraint rule
/// (body-type restrictions, coating validation) as a free consequence.
///
/// Cost note: this plans once per candidate material. Wrap the constraint providers in
/// <see cref="CachingFeatureConstraintProvider"/> before building the planner passed here, or every
/// candidate re-reads the model.</summary>
public sealed class AssignableMaterialQuery
{
    private readonly IMaterialAssignmentPlanner _planner;

    public AssignableMaterialQuery(IMaterialAssignmentPlanner planner) => _planner = planner;

    /// <summary>Every candidate with its verdict, in the order supplied — including the blocked ones, so a
    /// caller can grey them out with a reason rather than silently dropping them.</summary>
    public IReadOnlyList<MaterialAssignability> Evaluate(
        BodyInfo body,
        BodyMaterialAssignment? currentAssignment,
        IEnumerable<Material> candidates)
    {
        var targetBodies = new[] { body };
        var currentAssignments = currentAssignment is null
            ? new Dictionary<BodyId, BodyMaterialAssignment>()
            : new Dictionary<BodyId, BodyMaterialAssignment> { [body.Id] = currentAssignment };

        var results = new List<MaterialAssignability>();

        foreach (var candidate in candidates)
        {
            var plan = _planner.Plan(
                new MaterialAssignmentPlanningInput(candidate, targetBodies, currentAssignments));

            // One body in, so at most one evaluation out; an empty plan means no rule had anything to say.
            var outcomes = plan.BodyEvaluations.Count > 0
                ? plan.BodyEvaluations[0].RuleOutcomes
                : Array.Empty<RuleOutcome>();

            results.Add(new MaterialAssignability(candidate, outcomes));
        }

        return results;
    }

    /// <summary>Just the assignable ones, for a picker that has no way to show a disabled entry.</summary>
    public IReadOnlyList<Material> ListAssignable(
        BodyInfo body,
        BodyMaterialAssignment? currentAssignment,
        IEnumerable<Material> candidates) =>
        Evaluate(body, currentAssignment, candidates)
            .Where(r => r.IsAssignable)
            .Select(r => r.Material)
            .ToList();
}