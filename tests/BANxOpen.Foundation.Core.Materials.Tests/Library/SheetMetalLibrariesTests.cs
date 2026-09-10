using BANxOpen.Foundation.Contracts.Bodies;
using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Assignment.Rules;
using BANxOpen.Foundation.Core.Materials.Library;
using BANxOpen.Foundation.Core.RuleEngine;
using static BANxOpen.Foundation.Core.Materials.Tests.Assignment.TestFixtures;

namespace BANxOpen.Foundation.Core.Materials.Tests.Library;

public class SheetMetalLibrariesTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "BANxOpenLibraryRulesTests_" + Guid.NewGuid().ToString("N"));

    public SheetMetalLibrariesTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private string Write(string json)
    {
        var path = Path.Combine(_dir, SheetMetalLibraries.FileName);
        File.WriteAllText(path, json);
        return path;
    }

    private static MaterialLibraryId Lib(string name) => new(name);

    [Fact]
    public void Without_a_file_the_name_based_rule_applies()
    {
        var libraries = SheetMetalLibraries.Load(Path.Combine(_dir, SheetMetalLibraries.FileName));

        Assert.False(libraries.IsConfigured);
        Assert.True(libraries.IsSheetMetalLibrary(Lib("Sheet Metal Materials")));
        Assert.True(libraries.IsSheetMetalLibrary(Lib("SHEETMETAL_ALUMINUM")));
        Assert.False(libraries.IsSheetMetalLibrary(Lib("Castings")));
    }

    [Fact]
    public void A_configured_list_is_matched_exactly_and_case_insensitively()
    {
        var libraries = SheetMetalLibraries.Load(Write("""{ "sheetMetalLibraries": [ "Sheet Metal Materials", "Aero Skins" ] }"""));

        Assert.True(libraries.IsConfigured);
        Assert.True(libraries.IsSheetMetalLibrary(Lib("sheet metal materials")));
        Assert.True(libraries.IsSheetMetalLibrary(Lib("Aero Skins")));
    }

    [Fact]
    public void A_configured_list_replaces_the_name_rule_entirely()
    {
        // Containing "sheet metal" no longer makes a library count; only being listed does.
        var libraries = SheetMetalLibraries.Load(Write("""{ "sheetMetalLibraries": [ "Aero Skins" ] }"""));

        Assert.False(libraries.IsSheetMetalLibrary(Lib("Sheet Metal Materials")));
        Assert.False(libraries.IsSheetMetalLibrary(Lib("Aero Skins Archive")));
    }

    [Theory]
    [InlineData("""{ "sheetMetalLibraries": [] }""", "at least one library")]
    [InlineData("""{ "sheetMetalLibraries": [ "  " ] }""", "at least one library")]
    [InlineData("""{ "somethingElse": true }""", "no \"sheetMetalLibraries\" list")]
    [InlineData("not json", "not valid JSON")]
    public void An_invalid_file_is_rejected_rather_than_ignored(string json, string expected)
    {
        var ex = Assert.Throws<InvalidDataException>(() => SheetMetalLibraries.Load(Write(json)));

        Assert.Contains(expected, ex.Message);
    }

    [Fact]
    public void The_rules_file_is_found_beside_the_libraries_unless_overridden()
    {
        var previous = Environment.GetEnvironmentVariable(SheetMetalLibraries.PathEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(SheetMetalLibraries.PathEnvironmentVariable, null);
            Assert.Equal(Path.Combine(_dir, SheetMetalLibraries.FileName), SheetMetalLibraries.ResolvePath(_dir));

            var elsewhere = Path.Combine(_dir, "shared", "rules.json");
            Environment.SetEnvironmentVariable(SheetMetalLibraries.PathEnvironmentVariable, elsewhere);
            Assert.Equal(elsewhere, SheetMetalLibraries.ResolvePath(_dir));
        }
        finally
        {
            Environment.SetEnvironmentVariable(SheetMetalLibraries.PathEnvironmentVariable, previous);
        }
    }

    [Fact]
    public void The_body_type_rule_uses_the_configured_list()
    {
        var rule = new BlockRestrictedBodyTypeRule(SheetMetalLibraries.FromNames(new[] { "Aero Skins" }));
        var body = MakeBody("sm1", BodyKind.SheetMetal);

        RuleDecision On(string library) => rule.Evaluate(new MaterialAssignmentRuleContext(
            MakeMaterial("Al 2024") with { LibraryId = Lib(library) }, body, null, new[] { body })).Decision;

        Assert.Equal(RuleDecision.Allow, On("Aero Skins"));
        Assert.Equal(RuleDecision.Block, On("Sheet Metal Materials"));
    }

    [Fact]
    public void Standard_gates_pass_the_configured_list_to_the_body_type_rule()
    {
        var gates = StandardMaterialRules.Gates(sheetMetalLibraries: SheetMetalLibraries.FromNames(new[] { "Aero Skins" }));
        var rule = gates.OfType<BlockRestrictedBodyTypeRule>().Single();
        var body = MakeBody("sm1", BodyKind.SheetMetal);

        var decision = rule.Evaluate(new MaterialAssignmentRuleContext(
            MakeMaterial("Al 2024") with { LibraryId = Lib("Aero Skins") }, body, null, new[] { body })).Decision;

        Assert.Equal(RuleDecision.Allow, decision);
    }
}
