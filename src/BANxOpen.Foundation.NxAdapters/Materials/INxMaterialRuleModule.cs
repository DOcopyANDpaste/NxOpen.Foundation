using BANxOpen.Foundation.Core.Materials.Rules;

namespace BANxOpen.Foundation.NxAdapters.Materials;

/// <summary>A rule module together with the NX executors for its side effects — how a feature domain registers with
/// <see cref="MaterialEngine"/>. The rules stay in the domain's NX-free Core library; this is the adapter-side
/// bundle that pairs them with the code that carries their instructions out, so the two are registered as one unit
/// and <see cref="MaterialEngine.Create"/> can refuse a side effect that has no executor.</summary>
public interface INxMaterialRuleModule : IMaterialRuleModule
{
    /// <summary>One executor per instruction type this module's <see cref="IMaterialRuleModule.SideEffectRules"/>
    /// emit. Empty for a module with no side effects.</summary>
    IReadOnlyList<ISideEffectExecutor> SideEffectExecutors { get; }
}
