using NxOpen.Foundation.Contracts.Materials;

namespace NxOpen.Foundation.Core.MaterialLibrary;

/// <summary>Loads a material library on demand (lazy — only the library the caller actually picks gets
/// parsed) and caches the parsed result for the lifetime of this instance, so re-selecting a previously
/// loaded library is free.</summary>
public interface IMaterialLibraryLoader
{
    NxOpen.Foundation.Contracts.Materials.MaterialLibrary GetOrLoad(MaterialLibraryReference reference);
}
