using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Rules.Display;
using BANxOpen.Foundation.Core.Materials.Rules.Features;
using BANxOpen.Foundation.Core.Materials.Rules.SheetMetal;
using BANxOpen.Foundation.Core.Materials.Rules.Standard;

namespace BANxOpen.Foundation.Core.Materials.Rules;

/// <summary>Every material rule an entry point enforces, composed from <see cref="IMaterialRuleModule"/>s. Two
/// composition roots — the Material Assignment dialog and any feature tool that assigns material — build their
/// planner and finalizer from the same modules, instead of hand-assembled lists that drift apart.
///
/// Composing checks what a hand-built list could not: a rule id registered twice, and (through
/// <see cref="EnsureExecutorsFor"/>) a side effect nothing will carry out. Both throw — they are wiring mistakes
/// that would otherwise show up only as a rule quietly not applying.
///
/// Feature constraints from every module are enforced by a single <see cref="FeatureConstraintGateRule"/>, so a
/// refusal from one domain still beats a warning from another, and every warning is shown at once.</summary>
public sealed class MaterialRuleSet
{
    private readonly IReadOnlyList<IMaterialValidationRule> _moduleValidationRules;
    private readonly IReadOnlyList<IFeatureMaterialConstraintProvider> _featureConstraints;

    private MaterialRuleSet(IReadOnlyList<IMaterialRuleModule> modules)
    {
        Modules = modules;
        _moduleValidationRules = modules.SelectMany(m => m.ValidationRules).ToList();
        _featureConstraints = modules.SelectMany(m => m.FeatureConstraints).ToList();
        ValidationRules = WithFeatureGate(_featureConstraints);
        SideEffectRules = modules.SelectMany(m => m.SideEffectRules).OrderBy(r => r.Order).ToList();
    }

    /// <summary>The modules every entry point starts from: standard rules, display material, and the sheet metal
    /// library restriction. <paramref name="sheetMetalLibraries"/> is normally loaded with
    /// <see cref="SheetMetalLibraries.Load"/>; omitted, the name-based default applies.</summary>
    public static IReadOnlyList<IMaterialRuleModule> Baseline(SheetMetalLibraries? sheetMetalLibraries = null) =>
        new IMaterialRuleModule[]
        {
            new StandardRuleModule(),
            new DisplayMaterialRuleModule(),
            new SheetMetalLibraryRuleModule(sheetMetalLibraries),
        };

    /// <exception cref="ArgumentException">A module id, or a rule id, is registered more than once.</exception>
    public static MaterialRuleSet From(IEnumerable<IMaterialRuleModule> modules)
    {
        var list = modules.ToList();

        var moduleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in list)
        {
            if (!moduleIds.Add(module.ModuleId))
                throw new ArgumentException($"Material rule module '{module.ModuleId}' is registered more than once.", nameof(modules));
        }

        // The rule set adds the feature gate itself, so its id is taken before any module's rules are seen.
        var ruleOwners = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [FeatureConstraintGateRule.Id] = nameof(MaterialRuleSet),
        };

        foreach (var module in list)
        {
            var ruleIds = module.ValidationRules.Select(r => r.RuleId).Concat(module.SideEffectRules.Select(r => r.RuleId));
            foreach (var ruleId in ruleIds)
            {
                if (ruleOwners.TryGetValue(ruleId, out var owner))
                {
                    throw new ArgumentException(
                        $"Rule id '{ruleId}' in module '{module.ModuleId}' is already registered by '{owner}'.", nameof(modules));
                }

                ruleOwners[ruleId] = module.ModuleId;
            }
        }

        return new MaterialRuleSet(list);
    }

    public IReadOnlyList<IMaterialRuleModule> Modules { get; }

    /// <summary>Every validation rule in evaluation order, including the one feature constraint gate.</summary>
    public IReadOnlyList<IMaterialValidationRule> ValidationRules { get; }

    /// <summary>Every side-effect rule in evaluation order.</summary>
    public IReadOnlyList<IPostAssignmentEffectRule> SideEffectRules { get; }

    /// <param name="cacheFeatureConstraints">Memoise each body's feature constraints for the planner's lifetime.
    /// For planning one body against many candidate materials (<see cref="AssignableMaterialQuery"/>); create a new
    /// planner for each such query, since constraints come from live model state — see
    /// <see cref="CachingFeatureConstraintProvider"/>.</param>
    public IMaterialAssignmentPlanner CreatePlanner(bool cacheFeatureConstraints = false) =>
        new MaterialAssignmentPlanner(cacheFeatureConstraints
            ? WithFeatureGate(CachingFeatureConstraintProvider.WrapAll(_featureConstraints))
            : ValidationRules);

    public IAssignmentPlanFinalizer CreateFinalizer() => new AssignmentPlanFinalizer(SideEffectRules);

    /// <summary>Checks that every instruction type a registered side-effect rule can emit has an executor.</summary>
    /// <exception cref="InvalidOperationException">Lists each instruction type with no executor and the rule and
    /// module that emit it.</exception>
    public void EnsureExecutorsFor(IEnumerable<string> executorInstructionTypes)
    {
        var available = new HashSet<string>(executorInstructionTypes, StringComparer.Ordinal);

        var missing = Modules
            .SelectMany(module => module.SideEffectRules.SelectMany(rule => rule.InstructionTypes
                .Where(type => !available.Contains(type))
                .Select(type => $"'{type}' (emitted by {rule.RuleId} in module {module.ModuleId})")))
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"No side-effect executor is registered for {string.Join(", ", missing)}. " +
                "Register the executor with the module that owns the rule.");
        }
    }

    private IReadOnlyList<IMaterialValidationRule> WithFeatureGate(IEnumerable<IFeatureMaterialConstraintProvider> providers) =>
        _moduleValidationRules
            .Append(new FeatureConstraintGateRule(providers))
            .OrderBy(r => r.Order)
            .ToList();
}
