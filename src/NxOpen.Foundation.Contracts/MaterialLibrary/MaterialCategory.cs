namespace NxOpen.Foundation.Contracts.Materials;

/// <summary>Normalized grouping used to render one dialog tab. <see cref="Key"/> identifies the category
/// (equality/grouping key); <see cref="PathSegments"/> preserves the source class hierarchy for display/debugging.</summary>
public sealed record MaterialCategory(
    string Key,
    string DisplayName,
    IReadOnlyList<string> PathSegments,
    int? SortOrder = null)
{
    public static readonly MaterialCategory Uncategorized =
        new("uncategorized", "Uncategorized", Array.Empty<string>(), SortOrder: int.MaxValue);
}
