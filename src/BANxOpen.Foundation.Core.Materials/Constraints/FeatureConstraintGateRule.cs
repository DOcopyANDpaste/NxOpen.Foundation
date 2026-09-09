using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.RuleEngine;

namespace BANxOpen.Foundation.Core.Materials.Constraints;

/// <summary>Blocks an assignment that the features already on the target body forbid.
///
/// Ordered at 150, between the body-type restriction (100) and the reassignment confirmation (200): a
/// material the body's features rule out should be rejected outright rather than confirmed, so this has
/// to run before anything that asks the user a question.
///
/// With no providers registered this is a no-op that always allows, so wiring it into the standard rule
/// set costs nothing until a feature domain opts in.
///
/// Deliberately stateless — it queries the providers on every evaluation. The planner evaluates each body
/// exactly once per plan, so an Apply pays for one provider call per body either way. Repeatedly planning
/// the same body against many candidate materials (which is what filtering a library does) is the case
/// that would benefit from memoisation, and that is what <see cref="CachingFeatureConstraintProvider"/>
/// is for: the caller decides its lifetime, so a cache can never outlive the model state it was read
/// from.</summary>
public sealed class FeatureConstraintGateRule : IMaterialAssignmentRule
{
    private readonly IReadOnlyList<IFeatureMaterialConstraintProvider> _providers;

    public FeatureConstraintGateRule(IEnumerable<IFeatureMaterialConstraintProvider> providers) =>
        _providers = providers.ToList();

    public FeatureConstraintGateRule(params IFeatureMaterialConstraintProvider[] providers)
        : this((IEnumerable<IFeatureMaterialConstraintProvider>)providers) { }

    public string RuleId => "FEATURE_MATERIAL_CONSTRAINT";

    public int Order => 150;

    public RuleOutcome Evaluate(MaterialAssignmentRuleContext context)
    {
        var candidate = MaterialCandidate.FromLibrary(context.RequestedMaterial);

        foreach (var provider in _providers)
        {
            foreach (var constraint in provider.ConstraintsFor(context.TargetBody.Id))
            {
                if (constraint.IsSatisfiedBy(candidate))
                    continue;

                return new RuleOutcome(
                    RuleId,
                    RuleDecision.Block,
                    constraint.ReasonCode,
                    $"{constraint.DescribeViolation(candidate)} (required by {constraint.SourceLabel})");
            }
        }

        return new RuleOutcome(RuleId, RuleDecision.Allow, null, null);
    }
}