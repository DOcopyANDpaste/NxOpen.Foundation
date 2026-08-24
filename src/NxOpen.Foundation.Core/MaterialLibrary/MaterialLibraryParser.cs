using System.Xml.Linq;
using NxOpen.Foundation.Contracts.Common;
using NxOpen.Foundation.Contracts.Materials;

namespace NxOpen.Foundation.Core.MaterialLibrary;

/// <summary>Parses NX material library XML (generic MatML shape: Metadata/PropertyDetails +
/// Metadata/ClassDetails + repeated Material/BulkDetails elements) into domain objects.
///
/// NOTE: built against the generic/assumed MatML schema, not a confirmed NX sample file — expect to
/// adjust element/attribute names once real library XML is available.</summary>
public sealed class MaterialLibraryParser : IMaterialLibraryParser
{
    public NxOpen.Foundation.Contracts.Materials.MaterialLibrary Parse(MaterialLibraryId id, string displayName, string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        var root = doc.Root ?? throw new FormatException("Material library XML has no root element.");

        var metadata = root.Element("Metadata");
        var propertyDetails = BuildPropertyDetails(metadata);
        var categories = MaterialCategoryNormalizer.BuildFromMetadata(metadata);

        var materials = new List<Material>();
        var index = 0;
        foreach (var materialElement in root.Elements("Material"))
        {
            materials.Add(ParseMaterial(materialElement, id, propertyDetails, categories, index));
            index++;
        }

        return new NxOpen.Foundation.Contracts.Materials.MaterialLibrary(id, displayName, materials);
    }

    private static Material ParseMaterial(
        XElement materialElement,
        MaterialLibraryId libraryId,
        IReadOnlyDictionary<string, PropertyDetail> propertyDetails,
        IReadOnlyDictionary<string, MaterialCategory> categories,
        int index)
    {
        var bulk = materialElement.Element("BulkDetails")
            ?? throw new FormatException($"Material at index {index} is missing BulkDetails.");

        var rawName = bulk.Element("Name")?.Value?.Trim();
        var name = !string.IsNullOrEmpty(rawName) ? rawName! : $"Material {index + 1}";

        var rawId = (string?)materialElement.Attribute("id");
        var materialId = new MaterialId(rawId ?? $"{libraryId.Value}:{index}");

        var classElement = bulk.Element("Class");
        var classId = (string?)classElement?.Attribute("ID")
            ?? (string?)classElement?.Attribute("idref");
        var category = classId is not null && categories.TryGetValue(classId, out var matchedCategory)
            ? matchedCategory
            : BuildInlineCategory(classElement) ?? MaterialCategory.Uncategorized;

        var propertyValues = bulk.Elements("PropertyData")
            .Select(propertyData => ParsePropertyValue(propertyData, propertyDetails))
            .Where(value => value is not null)
            .Select(value => value!)
            .ToList();

        var description = bulk.Elements("Annotation")
            .FirstOrDefault(a => string.Equals((string?)a.Attribute("name"), "Description", StringComparison.OrdinalIgnoreCase))
            ?.Value?.Trim();

        return new Material(
            materialId,
            libraryId,
            name,
            category,
            propertyValues,
            string.IsNullOrEmpty(description) ? null : description);
    }

    /// <summary>Some NX exports give a class inline (<c>&lt;Class&gt;&lt;Name&gt;...&lt;/Name&gt;&lt;/Class&gt;</c>)
    /// rather than as an ID/idref into Metadata/ClassDetails. When there's no registry match, fall back to
    /// building a category straight from that inline name so the material isn't silently dumped into
    /// "Uncategorized".</summary>
    private static MaterialCategory? BuildInlineCategory(XElement? classElement)
    {
        var rawName = classElement?.Element("Name")?.Value?.Trim();
        if (rawName is null || rawName.Length == 0)
            return null;

        return new MaterialCategory(rawName.ToLowerInvariant(), rawName, [rawName]);
    }

    private static MaterialPropertyValue? ParsePropertyValue(
        XElement propertyData,
        IReadOnlyDictionary<string, PropertyDetail> propertyDetails)
    {
        var propertyId = (string?)propertyData.Attribute("property");
        if (propertyId is null)
            return null;

        var rawValue = propertyData.Element("Data")?.Value?.Trim() ?? string.Empty;

        var detail = propertyDetails.TryGetValue(propertyId, out var found)
            ? found
            : new PropertyDetail(propertyId, null, null);

        return new MaterialPropertyValue(propertyId, detail.Name, detail.Symbol, rawValue, detail.Unit);
    }

    private static IReadOnlyDictionary<string, PropertyDetail> BuildPropertyDetails(XElement? metadata)
    {
        var result = new Dictionary<string, PropertyDetail>(StringComparer.OrdinalIgnoreCase);
        if (metadata is null)
            return result;

        foreach (var propertyDetails in metadata.Elements("PropertyDetails"))
        {
            var id = (string?)propertyDetails.Attribute("id");
            if (id is null)
                continue;

            var nameElement = propertyDetails.Element("Name");
            var rawName = nameElement?.Value?.Trim();
            var name = !string.IsNullOrEmpty(rawName) ? rawName! : id;

            var symbol = (string?)nameElement?.Attribute("symbol");
            var unit = (string?)propertyDetails.Element("Units")?.Attribute("name");

            result[id] = new PropertyDetail(name, symbol, unit);
        }

        return result;
    }

    private readonly record struct PropertyDetail(string Name, string? Symbol, string? Unit);
}
