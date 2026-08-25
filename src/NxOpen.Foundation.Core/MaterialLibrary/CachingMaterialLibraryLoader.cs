using NxOpen.Foundation.Contracts.Common;
using NxOpen.Foundation.Contracts.Materials;

namespace NxOpen.Foundation.Core.MaterialLibrary;

/// <summary>Reads (via <see cref="IMaterialLibraryRepository"/>, the adapter-implemented seam) and parses
/// a library only the first time it's requested; every subsequent request for the same
/// <see cref="MaterialLibraryId"/> returns the cached result without touching the repository or parser
/// again. Not thread-safe — callers are expected to be single-threaded/modal, so a plain dictionary is
/// enough; revisit if that assumption ever changes.</summary>
public sealed class CachingMaterialLibraryLoader : IMaterialLibraryLoader
{
    private readonly IMaterialLibraryRepository _repository;
    private readonly IMaterialLibraryParser _parser;
    private readonly Dictionary<MaterialLibraryId, NxOpen.Foundation.Contracts.Materials.MaterialLibrary> _cache = new();

    public CachingMaterialLibraryLoader(IMaterialLibraryRepository repository, IMaterialLibraryParser parser)
    {
        _repository = repository;
        _parser = parser;
    }

    public NxOpen.Foundation.Contracts.Materials.MaterialLibrary GetOrLoad(MaterialLibraryReference reference)
    {
        if (_cache.TryGetValue(reference.Id, out var cached))
            return cached;

        var xmlContent = _repository.ReadLibraryXml(reference.Id);
        var library = _parser.Parse(reference.Id, reference.DisplayName, xmlContent) with { FilePath = reference.FilePath };
        _cache[reference.Id] = library;
        return library;
    }
}
