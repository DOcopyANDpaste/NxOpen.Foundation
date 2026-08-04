using NxOpen.Foundation.Contracts.Common;
using NxOpen.Foundation.Contracts.Materials;

namespace NxOpen.Foundation.Core.MaterialLibrary;

/// <summary>Parses material library XML content into domain objects. Pure text-in, data-out — no file
/// I/O, no NXOpen. The adapter layer reads the file and hands the content here.</summary>
public interface IMaterialLibraryParser
{
    NxOpen.Foundation.Contracts.Materials.MaterialLibrary Parse(MaterialLibraryId id, string displayName, string xmlContent);
}
