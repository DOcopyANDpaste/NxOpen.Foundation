using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Assignment.Choices;

/// <summary>Asks every provider what it needs decided for the bodies that are going ahead (blocked and declined
/// bodies are never asked about), and merges choices sharing a <see cref="AssignmentChoice.ChoiceId"/> and
/// <see cref="AssignmentChoice.GroupKey"/> into one question; the first body's choice is the one kept.</summary>
public sealed class AssignmentChoiceCollector : IAssignmentChoiceCollector
{
    private readonly IReadOnlyList<IAssignmentChoiceProvider> _providers;

    public AssignmentChoiceCollector(IEnumerable<IAssignmentChoiceProvider> providers) => _providers = providers.ToList();

    public IReadOnlyList<PendingAssignmentChoice> Collect(
        AssignmentPlan plan,
        MaterialAssignmentPlanningInput input,
        HashSet<BodyId> confirmedBodyIds)
    {
        if (_providers.Count == 0)
            return Array.Empty<PendingAssignmentChoice>();

        var targets = AssignmentTargets.Select(plan, input, confirmedBodyIds);
        if (targets.ToAssign.Count == 0)
            return Array.Empty<PendingAssignmentChoice>();

        // Insertion-ordered so the user is asked in provider order, then in the order the bodies were selected,
        // rather than in whatever order a hash bucket happens to produce.
        var grouped = new List<(AssignmentChoice Choice, List<BodyId> BodyIds)>();
        var indexByKey = new Dictionary<(string ChoiceId, string GroupKey), int>();

        foreach (var provider in _providers)
        {
            foreach (var target in targets.ToAssign)
            {
                input.CurrentAssignments.TryGetValue(target.Body.Id, out var currentAssignment);
                var context = new MaterialAssignmentRuleContext(
                    input.RequestedMaterial, target.Body, currentAssignment, input.TargetBodies);

                if (provider.ChoiceFor(context) is not { } choice)
                    continue;

                Validate(provider, choice);

                var key = (choice.ChoiceId, choice.GroupKey);
                if (indexByKey.TryGetValue(key, out var existing))
                {
                    grouped[existing].BodyIds.Add(target.Body.Id);
                    continue;
                }

                indexByKey[key] = grouped.Count;
                grouped.Add((choice, new List<BodyId> { target.Body.Id }));
            }
        }

        return grouped
            .Select(g => new PendingAssignmentChoice(g.Choice, g.BodyIds))
            .ToList();
    }

    /// <summary>Checks the shape of a choice before it reaches the UI. These are wiring mistakes in a provider,
    /// not user-facing conditions, so they throw for the same reason
    /// <see cref="Rules.MaterialRuleSet.EnsureExecutorsFor"/> does: a malformed choice otherwise shows up as a
    /// window with a blank column or an option that can never be selected.</summary>
    private static void Validate(IAssignmentChoiceProvider provider, AssignmentChoice choice)
    {
        string Fail(string what) =>
            $"Choice '{choice.ChoiceId}' from {provider.GetType().Name} {what}.";

        if (!string.Equals(choice.ChoiceId, provider.ChoiceId, StringComparison.Ordinal))
            throw new InvalidOperationException(Fail($"does not carry the provider's own id '{provider.ChoiceId}'"));

        if (choice.Options.Count == 0)
            throw new InvalidOperationException(Fail("has no options; return null instead of an empty choice"));

        if (choice.Columns.Count == 0)
            throw new InvalidOperationException(Fail("has no columns"));

        var optionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in choice.Options)
        {
            if (!optionIds.Add(option.OptionId))
                throw new InvalidOperationException(Fail($"lists option id '{option.OptionId}' more than once"));

            if (option.Cells.Count != choice.Columns.Count)
            {
                throw new InvalidOperationException(Fail(
                    $"has {choice.Columns.Count} column(s) but option '{option.OptionId}' has {option.Cells.Count} cell(s)"));
            }
        }

        if (choice.PreselectedOptionId is { } preselected && !optionIds.Contains(preselected))
            throw new InvalidOperationException(Fail($"preselects '{preselected}', which is not one of its options"));

        if (choice.Auto is { } auto && !optionIds.Contains(auto.OptionId))
            throw new InvalidOperationException(Fail($"auto-selects '{auto.OptionId}', which is not one of its options"));
    }
}
