using BANxOpen.Foundation.Contracts.Requirements;
using BANxOpen.Foundation.Core.Requirements;

namespace BANxOpen.Foundation.Core.Tests.Requirements;

public class RequirementNamingTests
{
    [Fact]
    public void Name_KeepsASeparatorForPunctuation_SoNearlyEqualKeysStayDistinct()
    {
        Assert.Equal("BA_REQ_SHEETMETAL_BEAD_BA_B1005010_2_GRADE", RequirementNaming.Name("SHEETMETAL.BEAD", "BA|B1005010-2", "GRADE"));
        Assert.NotEqual(
            RequirementNaming.Name("SHEETMETAL.BEAD", "BA|B1005010-2", "GRADE"),
            RequirementNaming.Name("SHEETMETAL.BEAD", "BA|B10050102", "GRADE"));
    }

    [Fact]
    public void Name_IsUpperCasedAndAsciiOnly()
    {
        // Space, 'é' and space each become one '_'.
        Assert.Equal("BA_REQ_D_K___X_A", RequirementNaming.Name("d", "k é x", "a"));
    }

    [Fact]
    public void Metadata_RoundTripsAnyKey()
    {
        var spec = Spec(domain: "SHEETMETAL.BEAD", key: "Std A|SPEC=1 2%", aspect: "THICKNESS",
            description: new[] { "Bead SPEC 1", "Users: x" });

        var written = RequirementNaming.DescriptionWithMetadata(spec);
        var (description, identity) = RequirementNaming.SplitDescription(written);

        Assert.Equal(spec.Description, description);
        Assert.Equal(("SHEETMETAL.BEAD", "Std A|SPEC=1 2%", "THICKNESS"), identity);
    }

    [Fact]
    public void SplitDescription_WithoutMetadata_HasNoIdentity()
    {
        var (description, identity) = RequirementNaming.SplitDescription(new[] { "written by hand" });

        Assert.Null(identity);
        Assert.Equal(new[] { "written by hand" }, description);
    }

    [Fact]
    public void CheckName_DerivesFromTheRequirementName()
    {
        Assert.Equal("BA_REQ_X_Y_Z_CHECK", RequirementNaming.CheckName("BA_REQ_X_Y_Z"));
    }

    internal static RequirementSpec Spec(
        string domain = "D", string key = "K", string aspect = "A",
        RequirementRule? rule = null, IReadOnlyList<string>? description = null, IReadOnlyList<string>? objects = null,
        string formula = "expr") =>
        new(domain, key, aspect, RequirementValueType.String, rule ?? new SetOfValuesRule(new[] { "a", "b" }),
            RequirementSeverity.Warning, formula, description ?? new[] { "line" }, objects ?? new[] { "101", "102" });
}
