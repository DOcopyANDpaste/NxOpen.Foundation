using System.Xml.Linq;
using NxOpen.Foundation.Contracts.Materials;

namespace NxOpen.Foundation.Core.MaterialLibrary;

/// <summary>Flattens a MatML Metadata/ClassDetails hierarchy (nested &lt;ClassDetails&gt; elements) into
/// a lookup from class id to a normalized <see cref="MaterialCategory"/> carrying the full path from
/// root to that class. Materials with no matching class fall back to <see cref="MaterialCategory.Uncategorized"/>
/// so callers never have to special-case a missing category.</summary>
internal static class MaterialCategoryNormalizer
{
    public static IReadOnlyDictionary<string, MaterialCategory> BuildFromMetadata(XElement? metadata)
    {
        var result = new Dictionary<string, MaterialCategory>(StringComparer.OrdinalIgnoreCase);
        if (metadata is null)
            return result;

        foreach (var root in metadata.Elements("ClassDetails"))
            Walk(root, Array.Empty<string>(), result);

        return result;
    }

    private static void Walk(XElement classDetails, IReadOnlyList<string> parentPath, Dictionary<string, MaterialCategory> result)
    {
        var id = (string?)classDetails.Attribute("id");
        var rawName = classDetails.Element("Name")?.Value?.Trim();
        var name = !string.IsNullOrEmpty(rawName) ? rawName! : (id ?? "Unknown");

        var path = parentPath.Append(name).ToArray();
        var key = string.Join("/", path).ToLowerInvariant();
        var category = new MaterialCategory(key, name, path);

        if (id is not null)
            result[id] = category;

        foreach (var child in classDetails.Elements("ClassDetails"))
            Walk(child, path, result);
    }
}
