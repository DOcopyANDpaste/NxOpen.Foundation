using BANxOpen.Foundation.Contracts.Materials;
using BANxOpen.Foundation.Core.Materials.Assignment;

namespace BANxOpen.Foundation.Core.Materials.Rules.Features;

/// <summary>What happens when a constraint is not satisfied.</summary>
public enum ConstraintSeverity
{
    /// <summary>The assignment is refused.</summary>
    Block,

    /// <summary>The assignment goes ahead, and the message is shown to the user as an advisory. For cases a
    /// domain wants surfaced but has decided not to enforce — for example a feature that cannot be matched
    /// to any known specification.</summary>
    Warn,
}

/// <summary>One restriction that features already on a body place on what material may be assigned to it.
///
/// Enforced by <see cref="FeatureConstraintGateRule"/>.</summary>
/// <param name="DomainId">Which feature domain imposed this, e.g. "SHEETMETAL.BEAD". Used for grouping
/// and diagnostics, never for dispatch.</param>
/// <param name="SourceLabel">The specific thing on the body responsible, e.g. "Bead SPEC BA-1234".
/// Shown to the user, so it must name something they can go and look at.</param>
/// <param name="ReasonCode">Stable machine-readable code, in the same style as the built-in rules'
/// reason codes.</param>
/// <param name="IsSatisfiedBy">True when the material is acceptable. Must be pure and cheap. A pure advisory that always applies
/// returns false unconditionally with <paramref name="Severity"/> Warn.</param>
/// <param name="DescribeViolation">Builds the user-facing message for a material that fails. Taken as a
/// function rather than a fixed string so the message can name the offending material and grade.</param>
/// <param name="Severity">Whether an unsatisfied constraint refuses the assignment or only warns. Defaults
/// to <see cref="ConstraintSeverity.Block"/>.</param>
public sealed record MaterialConstraint(
    string DomainId,
    string SourceLabel,
    string ReasonCode,
    Func<Material, bool> IsSatisfiedBy,
    Func<Material, string> DescribeViolation,
    ConstraintSeverity Severity = ConstraintSeverity.Block);
