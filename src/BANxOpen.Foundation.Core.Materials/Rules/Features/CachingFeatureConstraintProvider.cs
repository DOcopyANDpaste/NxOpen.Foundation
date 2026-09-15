using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Rules.Features;

/// <summary>Memoises another provider's answers per body, for callers that ask about the same body many
/// times in a row — filtering a whole library through the planner being the case that needs it, since
/// that plans one body against every candidate material and would otherwise re-read the model once per
/// candidate.
///
/// The cache has no invalidation on purpose. Constraints are derived from live model state, so a cache
/// that outlives the operation it was built for would go stale silently — the kind of bug that shows up
/// as a stale material list nobody can reproduce. Instead the lifetime is the caller's to choose:
/// construct one, use it for a single query or refresh, then drop it. Do not hold one in a composition
/// root.
///
/// Not thread-safe, matching CachingMaterialLibraryLoader.</summary>
public sealed class CachingFeatureConstraintProvider : IFeatureMaterialConstraintProvider
{
    private readonly IFeatureMaterialConstraintProvider _inner;
    private readonly Dictionary<BodyId, IReadOnlyList<MaterialConstraint>> _cache = new();

    public CachingFeatureConstraintProvider(IFeatureMaterialConstraintProvider inner) => _inner = inner;

    public string DomainId => _inner.DomainId;

    public IReadOnlyList<MaterialConstraint> ConstraintsFor(BodyId bodyId)
    {
        if (_cache.TryGetValue(bodyId, out var cached))
            return cached;

        var constraints = _inner.ConstraintsFor(bodyId);
        _cache[bodyId] = constraints;
        return constraints;
    }

    /// <summary>Wraps each provider so a caller can memoise a whole set in one step.</summary>
    public static IReadOnlyList<IFeatureMaterialConstraintProvider> WrapAll(
        IEnumerable<IFeatureMaterialConstraintProvider> providers) =>
        providers.Select(p => (IFeatureMaterialConstraintProvider)new CachingFeatureConstraintProvider(p)).ToList();
}