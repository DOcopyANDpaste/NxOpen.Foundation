using NxOpen.Foundation.Contracts.Common;

namespace NxOpen.Foundation.Contracts.Materials;

public sealed record MaterialLibrary(
    MaterialLibraryId Id,
    string DisplayName,
    IReadOnlyList<Material> Materials,
    string FilePath = "");
