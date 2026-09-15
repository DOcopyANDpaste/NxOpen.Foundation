using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Rules.Features;

/// <summary>Implemented once per feature domain to say what the features already on a body demand of its
/// material. The material engine calls this; it never learns what a bead, a louver or a casting is.
///
/// Implementations live in their own domain library (e.g. BANxOpen.SheetMetal.Core) and are registered
/// into the rule set by a composition root. Keep implementations pure: any NX access — reading the
/// stamped attributes that say which features are present — belongs behind a seam injected into the
/// implementation, so the provider itself stays unit-testable without NX.
///
/// Returning an empty list means "this domain has nothing to say about this body", which is the common
/// case and must be cheap. Note that it is indistinguishable from "this domain could not tell", so a
/// provider that fails to read the part should say so rather than silently returning nothing —
/// constraints that vanish fail open, and failing open on a material restriction is the bug this seam
/// exists to prevent.</summary>
public interface IFeatureMaterialConstraintProvider
{
    /// <summary>Stable identifier for the domain, e.g. "SHEETMETAL.BEAD".</summary>
    string DomainId { get; }

    IReadOnlyList<MaterialConstraint> ConstraintsFor(BodyId bodyId);
}