using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.Materials.Bodies;
using BANxOpen.Foundation.Core.Materials.Rules;
using BANxOpen.Foundation.Core.Materials.Rules.SheetMetal;

namespace BANxOpen.Foundation.NxAdapters.Materials;

/// <summary>The material engine as an entry point that assigns material uses it: the baseline rule modules, the
/// feature domains' modules, the executors for every side effect they emit, and the
/// <see cref="IPartMaterialService"/> that runs them. Building it here, once, is what keeps the Material Assignment
/// dialog and every feature tool enforcing the same rules with the same wiring.
///
/// A configuration problem the user can fix — an invalid <see cref="SheetMetalLibraries.FileName"/> — comes back as
/// a failure to show. A wiring mistake — a duplicate rule id, a side effect with no executor — throws, because it is
/// a defect in the tool that the first launch should expose rather than a dialog that runs with a rule missing.</summary>
public sealed class MaterialEngine
{
    private MaterialEngine(MaterialRuleSet rules, IPartMaterialService partMaterials)
    {
        Rules = rules;
        PartMaterials = partMaterials;
    }

    public MaterialRuleSet Rules { get; }

    public IPartMaterialService PartMaterials { get; }

    /// <param name="libraryRootDirectory">The material library folder, where the library rules file is looked for.</param>
    /// <param name="domainModules">The feature domains' modules, e.g. <c>SheetMetalServices.MaterialModules</c>.</param>
    public static OperationResult<MaterialEngine> Create(
        NxSessionContext context,
        BodyResolver bodyResolver,
        string libraryRootDirectory,
        IEnumerable<INxMaterialRuleModule> domainModules)
    {
        // A broken rules file stops the caller rather than being ignored: silently falling back to the name-based
        // default would drop a restriction someone configured on purpose.
        var libraryRulesPath = SheetMetalLibraries.ResolvePath(libraryRootDirectory);
        SheetMetalLibraries sheetMetalLibraries;
        try
        {
            sheetMetalLibraries = SheetMetalLibraries.Load(libraryRulesPath);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            context.Log.Error($"Material library rules could not be loaded: {ex.Message}");
            return OperationResult<MaterialEngine>.Fail("MATERIAL_RULES_UNAVAILABLE", ex.Message);
        }

        if (!sheetMetalLibraries.IsConfigured)
            context.Log.Info($"No {SheetMetalLibraries.FileName} at '{libraryRulesPath}'; sheet metal libraries are recognised by name.");

        var domains = domainModules.ToList();
        var rules = MaterialRuleSet.From(MaterialRuleSet.Baseline(sheetMetalLibraries).Concat<IMaterialRuleModule>(domains));

        // Display material sync is a baseline side effect, so its executor is the one this layer owns.
        var displayMaterialHelper = new DisplayMaterialHelper(context);
        var executors = new List<ISideEffectExecutor> { displayMaterialHelper };
        executors.AddRange(domains.SelectMany(domain => domain.SideEffectExecutors));
        rules.EnsureExecutorsFor(executors.Select(executor => executor.InstructionType));

        // Owns the only path that touches NX's own material library, which is slow — see the class doc for why that
        // happens lazily, per material, and only after asking.
        var physicalMaterials = new NxPhysicalMaterialSource(context);
        var partMaterials = new PartMaterialService(context, executors, bodyResolver, displayMaterialHelper, physicalMaterials);

        return OperationResult<MaterialEngine>.Success(new MaterialEngine(rules, partMaterials));
    }
}
