using BANxOpen.Foundation.Contracts.Materials;

namespace BANxOpen.Foundation.Core.MaterialLibrary;

/// <summary>Loads a material library on demand (lazy — only the library the caller actually picks gets
/// parsed) and caches the parsed result for the lifetime of this instance, so re-selecting a previously
/// loaded library is free.</summary>
public interface IMaterialLibraryLoader
{
    BANxOpen.Foundation.Contracts.Materials.MaterialLibrary GetOrLoad(MaterialLibraryReference reference);
}
