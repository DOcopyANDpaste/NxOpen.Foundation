using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.RuleEngine;

namespace BANxOpen.Foundation.Core.Materials.Rules.Standard;

/// <summary>If the body already has a different material assigned, require the user to confirm the
/// overwrite before it proceeds.</summary>
public sealed class RequireConfirmationOnReassignmentRule : IMaterialValidationRule
{
    public string RuleId => "CONFIRM_REASSIGNMENT";

    public int Order => MaterialRuleOrder.Validation.Confirmation;

    public RuleOutcome Evaluate(MaterialAssignmentRuleContext context)
    {
        var currentMaterialName = context.CurrentAssignment?.MaterialName;
        var isChangingMaterial = !string.IsNullOrEmpty(currentMaterialName)
            && !string.Equals(currentMaterialName, context.RequestedMaterial.Name, StringComparison.OrdinalIgnoreCase);

        return isChangingMaterial
            ? new RuleOutcome(
                RuleId,
                RuleDecision.RequireConfirmation,
                "REASSIGN",
                $"Body already has '{currentMaterialName}'. Replace with '{context.RequestedMaterial.Name}'?")
            : new RuleOutcome(RuleId, RuleDecision.Allow, null, null);
    }
}
