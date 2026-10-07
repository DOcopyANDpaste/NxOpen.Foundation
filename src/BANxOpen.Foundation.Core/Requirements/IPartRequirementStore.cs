using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Contracts.Requirements;

namespace BANxOpen.Foundation.Core.Requirements;

/// <summary>Reads and writes the requirements a domain holds in the work part. The NX implementation writes native NX
/// Requirements and Requirement Checks, so NX itself re-evaluates them on every change — including changes made with
/// no BANxOpen tool loaded.
///
/// A domain only ever sees and touches its own requirements (<see cref="RequirementSpec.Domain"/>).</summary>
public interface IPartRequirementStore
{
    /// <summary>Everything <paramref name="domain"/> holds in the part, with NX's last verdict on each.</summary>
    RequirementRead Read(string domain);

    /// <summary>Makes the part hold exactly <paramref name="current"/> for <paramref name="domain"/>: missing ones are
    /// created, changed ones replaced, and the domain's requirements not in the list deleted with their checks.
    ///
    /// Callers run this inside their own undo mark: a failure part-way leaves the part half-written, and only the
    /// caller's rollback puts it back.</summary>
    OperationResult Refresh(string domain, IReadOnlyList<RequirementSpec> current);
}
