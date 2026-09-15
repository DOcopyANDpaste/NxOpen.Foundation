using BANxOpen.Foundation.Core.Materials.Assignment;

namespace BANxOpen.Foundation.Core.Materials.Rules.Standard;

/// <summary>Emits a SYNC_PHYSICAL_PROPERTY instruction per numeric property on the assigned material,
/// carrying the raw value and its unit string as-is. Unit conversion is deliberately NOT done here —
/// the adapter layer converts at execution time using NX's real unit system, since Core has no NXOpen
/// access and can't know the work part's units.</summary>
public sealed class SyncPhysicalPropertiesEffectRule : IPostAssignmentEffectRule
{
    public const string InstructionType = "SYNC_PHYSICAL_PROPERTY";

    private static readonly string[] EmittedInstructionTypes = { InstructionType };

    public string RuleId => "SYNC_PHYSICAL_PROPERTIES";

    public int Order => MaterialRuleOrder.SideEffect.MaterialProperties;

    public IReadOnlyCollection<string> InstructionTypes => EmittedInstructionTypes;

    public IReadOnlyList<SideEffectInstruction> GenerateEffects(MaterialAssignmentRuleContext context)
    {
        var numericProperties = context.RequestedMaterial.Properties
            .Where(p => p.AsNumber().HasValue)
            .ToList();

        if (numericProperties.Count == 0)
            return Array.Empty<SideEffectInstruction>();

        var instructions = new List<SideEffectInstruction>(numericProperties.Count);
        foreach (var property in numericProperties)
        {
            var data = new Dictionary<string, object>
            {
                ["PropertyName"] = property.Name,
                ["RawValue"] = property.RawValue,
                ["Unit"] = property.Unit ?? string.Empty,
            };
            instructions.Add(new SideEffectInstruction(InstructionType, context.TargetBody.Id, data));
        }

        return instructions;
    }
}
