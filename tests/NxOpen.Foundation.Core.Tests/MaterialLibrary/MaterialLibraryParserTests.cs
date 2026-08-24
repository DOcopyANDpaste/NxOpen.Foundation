using NxOpen.Foundation.Contracts.Common;
using NxOpen.Foundation.Contracts.Materials;
using NxOpen.Foundation.Core.MaterialLibrary;

namespace NxOpen.Foundation.Core.Tests.MaterialLibrary;

public class MaterialLibraryParserTests
{
    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName));

    private static NxOpen.Foundation.Contracts.Materials.MaterialLibrary ParseMetricFixture()
    {
        var parser = new MaterialLibraryParser();
        return parser.Parse(new MaterialLibraryId("metric"), "Metric Library", ReadFixture("sample-metric-library.xml"));
    }

    [Fact]
    public void Parse_ReadsAllMaterialsInDocumentOrder()
    {
        var library = ParseMetricFixture();

        Assert.Equal(3, library.Materials.Count);
        Assert.Equal(new[] { "Steel, Mild", "ABS Plastic", "Unclassified Sample" }, library.Materials.Select(m => m.Name));
    }

    [Fact]
    public void Parse_UsesExplicitMaterialIdWhenPresent()
    {
        var library = ParseMetricFixture();

        var steel = library.Materials.Single(m => m.Name == "Steel, Mild");
        Assert.Equal("mat_steel_mild", steel.Id.Value);
    }

    [Fact]
    public void Parse_GeneratesFallbackIdWhenMaterialHasNoIdAttribute()
    {
        var library = ParseMetricFixture();

        var unclassified = library.Materials.Single(m => m.Name == "Unclassified Sample");
        Assert.Equal("metric:2", unclassified.Id.Value);
    }

    [Fact]
    public void Parse_ResolvesNestedClassHierarchyIntoCategoryPath()
    {
        var library = ParseMetricFixture();

        var steel = library.Materials.Single(m => m.Name == "Steel, Mild");
        Assert.Equal("metal/ferrous/steel", steel.Category.Key);
        Assert.Equal(new[] { "Metal", "Ferrous", "Steel" }, steel.Category.PathSegments);
        Assert.Equal("Steel", steel.Category.DisplayName);
    }

    [Fact]
    public void Parse_FallsBackToUncategorizedWhenMaterialHasNoClass()
    {
        var library = ParseMetricFixture();

        var unclassified = library.Materials.Single(m => m.Name == "Unclassified Sample");
        Assert.Equal(MaterialCategory.Uncategorized, unclassified.Category);
    }

    [Fact]
    public void Parse_UsesInlineClassNameWhenNoRegistryMatch()
    {
        // Real NX exports have been seen to give the class inline (no Metadata/ClassDetails registry,
        // no ID/idref) rather than through the generic MatML registry-lookup shape the rest of this
        // fixture set targets.
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <MatML_Doc>
              <Material>
                <BulkDetails>
                  <Name>TestMaterial</Name>
                  <Class>
                    <Name>Testing Only Material</Name>
                  </Class>
                  <PropertyData property="Density">
                    <Data format="exponential">0.000000e+00</Data>
                  </PropertyData>
                </BulkDetails>
              </Material>
            </MatML_Doc>
            """;

        var parser = new MaterialLibraryParser();
        var library = parser.Parse(new MaterialLibraryId("inline"), "Inline Library", xml);

        var material = library.Materials.Single();
        Assert.Equal("Testing Only Material", material.Category.DisplayName);
        Assert.NotEqual(MaterialCategory.Uncategorized, material.Category);
    }

    [Fact]
    public void Parse_ReadsNumericPropertyValueWithUnitAndSymbol()
    {
        var library = ParseMetricFixture();

        var steel = library.Materials.Single(m => m.Name == "Steel, Mild");
        var density = steel.Properties.Single(p => p.Name == "Density");

        Assert.Equal(7.872, density.AsNumber());
        Assert.Equal("7.872", density.RawValue);
        Assert.Equal("g/cm^3", density.Unit);
        Assert.Equal("rho", density.Symbol);
    }

    [Fact]
    public void Parse_ReadsDescriptionAnnotationWhenPresent()
    {
        var library = ParseMetricFixture();

        var steel = library.Materials.Single(m => m.Name == "Steel, Mild");
        var abs = library.Materials.Single(m => m.Name == "ABS Plastic");

        Assert.Equal("General purpose mild steel.", steel.Description);
        Assert.Null(abs.Description);
    }

    [Fact]
    public void Parse_PassesThroughEnglishUnitsWithoutConversion()
    {
        var parser = new MaterialLibraryParser();
        var library = parser.Parse(new MaterialLibraryId("english"), "English Library", ReadFixture("sample-english-library.xml"));

        var aluminum = library.Materials.Single();
        var density = aluminum.Properties.Single(p => p.Name == "Density");

        Assert.Equal("lbm/in^3", density.Unit);
        Assert.Equal(0.0975, density.AsNumber());
    }
}
