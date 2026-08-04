using NxOpen.Foundation.Contracts.Common;

namespace NxOpen.Foundation.Contracts.Materials;

/// <summary>Seam to the material library directory on disk. Implemented by an adapter (e.g. a
/// plain file-system reader) in a later phase; Core never touches the file system directly.</summary>
public interface IMaterialLibraryRepository
{
    IReadOnlyList<MaterialLibraryReference> ListAvailableLibraries();

    string ReadLibraryXml(MaterialLibraryId id);
}
