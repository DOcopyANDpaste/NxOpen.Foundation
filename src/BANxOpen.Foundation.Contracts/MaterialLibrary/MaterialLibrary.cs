using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Contracts.Materials;

public sealed record MaterialLibrary(
    MaterialLibraryId Id,
    string DisplayName,
    IReadOnlyList<Material> Materials,
    string FilePath = "");
