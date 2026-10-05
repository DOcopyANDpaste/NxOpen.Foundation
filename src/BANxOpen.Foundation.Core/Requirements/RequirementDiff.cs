using BANxOpen.Foundation.Contracts.Requirements;

namespace BANxOpen.Foundation.Core.Requirements;

/// <summary>What a refresh has to do to turn the requirements a part holds into the ones a domain wants. Pure, so the
/// store's bookkeeping is tested without NX.</summary>
/// <param name="ToCreate">Wanted, and not in the part.</param>
/// <param name="ToReplace">In the part, but no longer as wanted. Replaced (deleted and re-created) rather than edited:
/// that keeps the store to two NX operations whose undo behaviour is known.</param>
/// <param name="ToDelete">In the part, and no longer wanted.</param>
/// <param name="Errors">Why the wanted set cannot be written — two specs with the same identity, or with identities
/// that collapse to the same NX name. Nothing should be written when this is non-empty.</param>
public sealed record RequirementDiff(
    IReadOnlyList<RequirementSpec> ToCreate,
    IReadOnlyList<(RequirementSpec Existing, RequirementSpec Wanted)> ToReplace,
    IReadOnlyList<RequirementSpec> ToDelete,
    IReadOnlyList<string> Errors)
{
    public bool IsEmpty => ToCreate.Count == 0 && ToReplace.Count == 0 && ToDelete.Count == 0;

    public static RequirementDiff Compute(IReadOnlyList<RequirementSpec> existing, IReadOnlyList<RequirementSpec> wanted)
    {
        var errors = new List<string>();

        foreach (var group in wanted.GroupBy(RequirementNaming.Name, StringComparer.Ordinal).Where(g => g.Count() > 1))
        {
            errors.Add($"Requirement name '{group.Key}' is claimed by " +
                       string.Join(", ", group.Select(s => $"{s.Domain}/{s.Key}/{s.Aspect}")) + ".");
        }

        if (errors.Count > 0)
        {
            return new RequirementDiff(
                Array.Empty<RequirementSpec>(), Array.Empty<(RequirementSpec, RequirementSpec)>(),
                Array.Empty<RequirementSpec>(), errors);
        }

        var existingByName = existing
            .GroupBy(RequirementNaming.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var wantedNames = new HashSet<string>(wanted.Select(RequirementNaming.Name), StringComparer.Ordinal);

        var toCreate = new List<RequirementSpec>();
        var toReplace = new List<(RequirementSpec, RequirementSpec)>();
        foreach (var spec in wanted)
        {
            if (!existingByName.TryGetValue(RequirementNaming.Name(spec), out var current))
                toCreate.Add(spec);
            else if (!AreEquivalent(current, spec))
                toReplace.Add((current, spec));
        }

        // Unwanted names go entirely. A wanted name the part holds more than once (a hand-copied requirement, say)
        // keeps only the first, which the name lookup above compared against.
        var toDelete = new List<RequirementSpec>();
        foreach (var group in existing.GroupBy(RequirementNaming.Name, StringComparer.Ordinal))
            toDelete.AddRange(wantedNames.Contains(group.Key) ? group.Skip(1) : group);

        return new RequirementDiff(toCreate, toReplace, toDelete, Array.Empty<string>());
    }

    /// <summary>Whether two specs would leave the same thing in NX. Value lists compare in order — the order is what
    /// NX shows — while associated objects compare as a set, since NX does not keep their order.</summary>
    private static bool AreEquivalent(RequirementSpec a, RequirementSpec b) =>
        a.Domain == b.Domain
        && a.Key == b.Key
        && a.Aspect == b.Aspect
        && a.ValueType == b.ValueType
        && a.Severity == b.Severity
        && a.CheckedFormula == b.CheckedFormula
        && RulesEqual(a.Rule, b.Rule)
        && a.Description.SequenceEqual(b.Description, StringComparer.Ordinal)
        && new HashSet<string>(a.AssociatedObjectKeys, StringComparer.Ordinal).SetEquals(b.AssociatedObjectKeys);

    private static bool RulesEqual(RequirementRule a, RequirementRule b) => (a, b) switch
    {
        (SetOfValuesRule x, SetOfValuesRule y) => x.Values.SequenceEqual(y.Values, StringComparer.Ordinal),
        (EqualsValueRule x, EqualsValueRule y) => NearlyEqual(x.Value, y.Value) && NearlyEqual(x.Tolerance, y.Tolerance),
        _ => false,
    };

    // NX hands numbers back through a string field; a round trip may differ in the last bits.
    private static bool NearlyEqual(double x, double y) => Math.Abs(x - y) <= 1e-9 * Math.Max(1.0, Math.Max(Math.Abs(x), Math.Abs(y)));
}
