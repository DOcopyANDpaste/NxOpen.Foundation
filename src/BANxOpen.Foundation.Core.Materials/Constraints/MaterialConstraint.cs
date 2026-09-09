namespace BANxOpen.Foundation.Core.Materials.Constraints;

/// <summary>One restriction that features already on a body place on what material may be assigned to it.
///
/// The predicate form matters: the same constraint is used both to gate a requested assignment
/// (<see cref="FeatureConstraintGateRule"/>) and to filter the set of materials offered to the user
/// (<c>AssignableMaterialQuery</c>). Expressing it once as a predicate is what stops those two answers
/// from drifting apart, which is the failure mode that made the bead dialog and the material dialog
/// disagree in the first place.</summary>
/// <param name="DomainId">Which feature domain imposed this, e.g. "SHEETMETAL.BEAD". Used for grouping
/// and diagnostics, never for dispatch.</param>
/// <param name="SourceLabel">The specific thing on the body responsible, e.g. "Bead SPEC BA-1234".
/// Shown to the user, so it must name something they can go and look at.</param>
/// <param name="ReasonCode">Stable machine-readable code, in the same style as the built-in rules'
/// reason codes.</param>
/// <param name="IsSatisfiedBy">True when the candidate is acceptable. Must be pure and cheap: it is
/// evaluated once per candidate material when filtering a library.</param>
/// <param name="DescribeViolation">Builds the user-facing message for a candidate that fails. Taken as a
/// function rather than a fixed string so the message can name the offending material and grade.</param>
public sealed record MaterialConstraint(
    string DomainId,
    string SourceLabel,
    string ReasonCode,
    Func<MaterialCandidate, bool> IsSatisfiedBy,
    Func<MaterialCandidate, string> DescribeViolation);