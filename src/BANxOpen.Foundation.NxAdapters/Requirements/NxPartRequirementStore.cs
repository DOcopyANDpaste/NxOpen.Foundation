using System.Globalization;
using NXOpen;
using NXOpen.Validate;
using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Contracts.Requirements;
using BANxOpen.Foundation.Core.Requirements;

namespace BANxOpen.Foundation.NxAdapters.Requirements;

/// <summary>Holds <see cref="RequirementSpec"/>s in the work part as native NX Requirements, each linked to its
/// checked expression by a Requirement Check (<c>Requirement.NewCheck</c>). NX re-evaluates the checks on every change
/// (<see cref="Validation.UpdateTime.EveryChange"/>), so a requirement keeps watching the part after the tool that
/// wrote it has closed — and with no BANxOpen code loaded at all.
///
/// API verified by reflecting NX 2412's NXOpen.dll and against a journal of the Expressions dialog's
/// "Add a Check" (RequirementBuilder: RequirementTypeOption, DefinitionMethodOption, SingleSidedValue,
/// RequirementTolerance, Commit).
///
/// A changed requirement is replaced — its check and itself deleted, then both created again — rather than edited
/// through <c>CreateRequirementBuilder(existing)</c>: deleting and creating are the two operations whose undo
/// behaviour inside the caller's undo mark is known, and a requirement holds nothing worth preserving across a
/// change.</summary>
public sealed class NxPartRequirementStore : IPartRequirementStore
{
    private readonly NxSessionContext _context;

    public NxPartRequirementStore(NxSessionContext context) => _context = context;

    private Part WorkPart => _context.WorkPart;

    public RequirementRead Read(string domain)
    {
        var (owned, errors) = ReadOwned(domain);
        return new RequirementRead(owned.Select(o => new StoredRequirement(o.Spec, o.Status)).ToList(), errors);
    }

    public OperationResult Refresh(string domain, IReadOnlyList<RequirementSpec> current)
    {
        var foreign = current.Where(s => s.Domain != domain).Select(RequirementNaming.Name).ToList();
        if (foreign.Count > 0)
            return OperationResult.Fail("REQUIREMENT_WRONG_DOMAIN", $"Requirements {string.Join(", ", foreign)} do not belong to domain '{domain}'.");

        var (owned, readErrors) = ReadOwned(domain);
        if (readErrors.Count > 0)
        {
            // Never write over what could not be read: an unreadable requirement would look absent and be duplicated,
            // or look unwanted and be deleted without anyone knowing what it said.
            return OperationResult.Fail("REQUIREMENTS_UNREADABLE", string.Join(" ", readErrors));
        }

        var diff = RequirementDiff.Compute(owned.Select(o => o.Spec).ToList(), current);
        if (diff.Errors.Count > 0)
            return OperationResult.Fail("REQUIREMENT_NAME_CONFLICT", string.Join(" ", diff.Errors));

        try
        {
            foreach (var spec in diff.ToDelete.Concat(diff.ToReplace.Select(r => r.Existing)))
                Delete(owned.First(o => ReferenceEquals(o.Spec, spec)));

            foreach (var spec in diff.ToCreate.Concat(diff.ToReplace.Select(r => r.Wanted)))
                Create(spec);
        }
        catch (NXException ex)
        {
            _context.Log.Error($"Writing requirements for '{domain}' failed: NX {ex.ErrorCode}: {ex.Message}");
            return OperationResult.Fail("REQUIREMENT_WRITE_FAILED", $"NX {ex.ErrorCode}: {ex.Message}");
        }

        if (!diff.IsEmpty)
        {
            _context.Log.Info($"Requirements for '{domain}': {diff.ToCreate.Count} created, {diff.ToReplace.Count} replaced, " +
                              $"{diff.ToDelete.Count} deleted.");
        }

        return OperationResult.Success();
    }

    // ---- Reading ----

    private sealed record Owned(RequirementSpec Spec, RequirementStatus Status, Requirement Requirement, RequirementCheck? Check);

    private (List<Owned> Owned, List<string> Errors) ReadOwned(string domain)
    {
        var owned = new List<Owned>();
        var errors = new List<string>();

        Requirement[] requirements;
        RequirementCheck[] checks;
        try
        {
            requirements = WorkPart.Requirements.ToArray();
            checks = WorkPart.RequirementChecks.ToArray();
        }
        catch (NXException ex)
        {
            errors.Add($"The part's requirements could not be listed: NX {ex.ErrorCode}: {ex.Message}");
            return (owned, errors);
        }

        // Every requirement is opened, not only those named BA_REQ_*: ownership is decided by the bookkeeping line in the
        // description, so a requirement whose NX name did not come out as written is still found — and never duplicated.
        foreach (var requirement in requirements)
        {
            var name = SafeName(requirement);
            try
            {
                var check = checks.FirstOrDefault(c => c.ParentRequirement?.Tag == requirement.Tag);
                if (ReadSpec(requirement, check) is { } read && read.Spec.Domain == domain)
                    owned.Add(read);
            }
            catch (NXException ex)
            {
                errors.Add($"Requirement '{name}' could not be read: NX {ex.ErrorCode}: {ex.Message}");
            }
            catch (FormatException ex)
            {
                errors.Add($"Requirement '{name}' holds a value that could not be read: {ex.Message}");
            }
        }

        return (owned, errors);
    }

    private static string SafeName(Requirement requirement)
    {
        try
        {
            return requirement.Name;
        }
        catch (NXException)
        {
            return $"(tag {requirement.Tag})";
        }
    }

    /// <summary>Null for a requirement carrying no BANxOpen identity line — not ours to read or to touch.</summary>
    private Owned? ReadSpec(Requirement requirement, RequirementCheck? check)
    {
        var builder = WorkPart.Requirements.CreateRequirementBuilder(requirement);
        try
        {
            var (description, identity) = RequirementNaming.SplitDescription(builder.GetRequirementDescription() ?? Array.Empty<string>());
            if (identity is not { } id)
                return null;

            RequirementRule rule = builder.DefinitionMethodOption == RequirementBuilder.DefinitionMethodOptions.SetOfValues
                ? new SetOfValuesRule(builder.GetValidValues() ?? Array.Empty<string>())
                : new EqualsValueRule(
                    double.Parse(builder.SingleSidedValue, NumberStyles.Float, CultureInfo.InvariantCulture),
                    builder.RequirementTolerance);

            var spec = new RequirementSpec(
                id.Domain,
                id.Key,
                id.Aspect,
                builder.DataTypeOption == RequirementBuilder.DataTypeOptions.String ? RequirementValueType.String : RequirementValueType.Number,
                rule,
                FromNx(builder.SeverityOption),
                check?.Formula ?? string.Empty,
                description,
                AssociatedKeys(check));

            return new Owned(spec, check is null ? RequirementStatus.Unknown : FromNx(check.GetCheckResult()), requirement, check);
        }
        finally
        {
            builder.Destroy();
        }
    }

    private static IReadOnlyList<string> AssociatedKeys(RequirementCheck? check)
    {
        if (check is null)
            return Array.Empty<string>();

        check.GetAssociatedObjects(out var objects);
        return (objects ?? Array.Empty<NXObject>()).Select(o => o.Tag.ToString()).ToList();
    }

    // ---- Writing ----

    private void Delete(Owned owned)
    {
        owned.Check?.Delete();
        owned.Requirement.Delete();
    }

    private void Create(RequirementSpec spec)
    {
        var name = RequirementNaming.Name(spec);

        var builder = WorkPart.Requirements.CreateRequirementBuilder(null);
        Requirement requirement;
        try
        {
            builder.RequirementTypeOption = RequirementBuilder.RequirementTypeOptions.ValidationLimit;
            builder.Name = name;
            builder.DataTypeOption = spec.ValueType == RequirementValueType.String
                ? RequirementBuilder.DataTypeOptions.String
                : RequirementBuilder.DataTypeOptions.Number;
            builder.SeverityOption = ToNx(spec.Severity);

            switch (spec.Rule)
            {
                case SetOfValuesRule set:
                    builder.DefinitionMethodOption = RequirementBuilder.DefinitionMethodOptions.SetOfValues;
                    builder.SetValidValues(set.Values.ToArray());
                    break;

                case EqualsValueRule equals:
                    builder.DefinitionMethodOption = RequirementBuilder.DefinitionMethodOptions.SingleSidedComparison;
                    builder.RelationalOperatorOption = RequirementBuilder.RelationalOperatorOptions.Equal;
                    builder.SingleSidedValue = equals.Value.ToString("R", CultureInfo.InvariantCulture);
                    builder.RequirementTolerance = equals.Tolerance;
                    break;

                default:
                    throw new InvalidOperationException($"Requirement '{name}' has an unsupported rule {spec.Rule.GetType().Name}.");
            }

            builder.SetRequirementDescription(RequirementNaming.DescriptionWithMetadata(spec).ToArray());
            requirement = (Requirement)builder.Commit();
        }
        finally
        {
            builder.Destroy();
        }

        var check = requirement.NewCheck(RequirementNaming.CheckName(name), spec.CheckedFormula);
        check.SetUpdateControl(Validation.UpdateTime.EveryChange);

        var objects = ResolveObjects(spec.AssociatedObjectKeys, name);
        if (objects.Length > 0)
            check.SetAssociatedObjects(objects);
    }

    /// <summary>A key that no longer resolves (the feature was deleted since the inventory was read) is left out with
    /// a warning: the check still guards the part, it just highlights one object fewer.</summary>
    private NXObject[] ResolveObjects(IReadOnlyList<string> keys, string requirementName)
    {
        var objects = new List<NXObject>();
        foreach (var key in keys)
        {
            NXObject? found = null;
            try
            {
                if (Enum.TryParse<Tag>(key, out var tag))
                    found = NXOpen.Utilities.NXObjectManager.Get(tag) as NXObject;
            }
            catch (NXException)
            {
            }

            if (found is null)
                _context.Log.Warn($"Requirement '{requirementName}': object {key} no longer exists and is not highlighted by its check.");
            else
                objects.Add(found);
        }

        return objects.ToArray();
    }

    private static RequirementBuilder.SeverityOptions ToNx(RequirementSeverity severity) => severity switch
    {
        RequirementSeverity.Error => RequirementBuilder.SeverityOptions.Error,
        RequirementSeverity.Information => RequirementBuilder.SeverityOptions.Information,
        _ => RequirementBuilder.SeverityOptions.Warning,
    };

    private static RequirementSeverity FromNx(RequirementBuilder.SeverityOptions severity) => severity switch
    {
        RequirementBuilder.SeverityOptions.Error => RequirementSeverity.Error,
        RequirementBuilder.SeverityOptions.Information => RequirementSeverity.Information,
        _ => RequirementSeverity.Warning,
    };

    private static RequirementStatus FromNx(Validation.Result result) => result switch
    {
        Validation.Result.Pass => RequirementStatus.Pass,
        Validation.Result.Information => RequirementStatus.Information,
        Validation.Result.Warning => RequirementStatus.Warning,
        Validation.Result.Failed => RequirementStatus.Failed,
        Validation.Result.Skipped => RequirementStatus.Skipped,
        _ => RequirementStatus.Unknown,
    };
}
