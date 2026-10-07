namespace BANxOpen.Foundation.Contracts.Requirements;

/// <summary>What kind of value an NX Requirement compares.</summary>
public enum RequirementValueType
{
    Number,
    String,
}

/// <summary>How loudly NX reports a failed check. Mirrors <c>NXOpen.Validate.RequirementBuilder.SeverityOptions</c>.</summary>
public enum RequirementSeverity
{
    Information,
    Warning,
    Error,
}

/// <summary>The comparison a requirement makes. Closed set: these are the two definition methods the tools write.</summary>
public abstract record RequirementRule;

/// <summary>The checked value must be one of <paramref name="Values"/> — NX's "set of values" definition method.</summary>
public sealed record SetOfValuesRule(IReadOnlyList<string> Values) : RequirementRule;

/// <summary>The checked value must equal <paramref name="Value"/> within <paramref name="Tolerance"/> — NX's
/// single-sided comparison with the Equal operator. Values are in the part's own units.</summary>
public sealed record EqualsValueRule(double Value, double Tolerance) : RequirementRule;

/// <summary>One requirement a feature domain wants to hold in a part, written as a native NX Requirement plus the
/// Requirement Check that links it to <see cref="CheckedFormula"/>.
///
/// Identity is (<see cref="Domain"/>, <see cref="Key"/>, <see cref="Aspect"/>): a domain owns every requirement it
/// wrote, a key groups the requirements one source imposes (a bead SPEC, say), and the aspect tells them apart
/// (grade, thickness). The NX object name is derived from the identity, so it is never stored separately.</summary>
/// <param name="Domain">Owning domain, e.g. "SHEETMETAL.BEAD". A store only ever touches its own domain's
/// requirements.</param>
/// <param name="Key">What imposes the requirement within the domain, e.g. "BA|B1005010-2".</param>
/// <param name="Aspect">Which property it constrains, e.g. "GRADE".</param>
/// <param name="CheckedFormula">The expression NX evaluates the requirement against, e.g. an expression name.</param>
/// <param name="Description">Shown to the user in NX alongside the check. Plain lines; the store adds its own
/// bookkeeping line and strips it again on read.</param>
/// <param name="AssociatedObjectKeys">The objects NX highlights for the check — tag values as strings, the same form
/// a feature inventory's feature keys take. Order-insensitive.</param>
public sealed record RequirementSpec(
    string Domain,
    string Key,
    string Aspect,
    RequirementValueType ValueType,
    RequirementRule Rule,
    RequirementSeverity Severity,
    string CheckedFormula,
    IReadOnlyList<string> Description,
    IReadOnlyList<string> AssociatedObjectKeys);

/// <summary>NX's last verdict on a requirement's check. Mirrors <c>NXOpen.Validation.Result</c>.</summary>
public enum RequirementStatus
{
    Unknown,
    Pass,
    Information,
    Warning,
    Failed,
    Skipped,
}

/// <summary>A requirement as read back from a part, with NX's own verdict on it.</summary>
public sealed record StoredRequirement(RequirementSpec Spec, RequirementStatus Status);

/// <summary>Everything a domain owns in a part. <see cref="Errors"/> lists what could not be read — a reader never
/// silently drops a requirement it failed to understand, since a caller would then treat it as absent.</summary>
public sealed record RequirementRead(IReadOnlyList<StoredRequirement> Requirements, IReadOnlyList<string> Errors)
{
    public static RequirementRead Empty { get; } = new(Array.Empty<StoredRequirement>(), Array.Empty<string>());

    public bool Ok => Errors.Count == 0;
}
