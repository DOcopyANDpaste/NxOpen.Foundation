using NxOpen.Foundation.Contracts.Common;

namespace NxOpen.Foundation.Contracts.Materials;

public sealed record Material(
    MaterialId Id,
    MaterialLibraryId LibraryId,
    string Name,
    MaterialCategory Category,
    IReadOnlyList<MaterialPropertyValue> Properties,
    string? Description = null,
    (byte R, byte G, byte B)? AppearanceColor = null);
