using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Contracts.Materials;

namespace BANxOpen.Foundation.Core.MaterialLibrary;

/// <summary>Parses material library XML content into domain objects. Pure text-in, data-out — no file
/// I/O, no NXOpen. The adapter layer reads the file and hands the content here.</summary>
public interface IMaterialLibraryParser
{
    BANxOpen.Foundation.Contracts.Materials.MaterialLibrary Parse(MaterialLibraryId id, string displayName, string xmlContent);
}
