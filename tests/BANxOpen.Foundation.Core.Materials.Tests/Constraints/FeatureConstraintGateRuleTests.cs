using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Constraints;
using BANxOpen.Foundation.Core.RuleEngine;
using BANxOpen.Foundation.Core.Materials.Tests.Assignment;
using static BANxOpen.Foundation.Core.Materials.Tests.Constraints.ConstraintFixtures;

namespace BANxOpen.Foundation.Core.Materials.Tests.Constraints;

public class FeatureConstraintGateRuleTests
{
    private static MaterialAssignmentRuleContext Context(string materialName)
    {
        var body = TestFixtures.MakeBody("body-1");
        return new MaterialAssignmentRuleContext(
            TestFixtures.MakeMaterial(materialName), body, null, new[] { body });
    }

    [Fact]
    public void Allows_when_no_providers_are_registered()
    {
        var rule = new FeatureConstraintGateRule();

        var outcome = rule.Evaluate(Context("Anything"));

        Assert.Equal(RuleDecision.Allow, outcome.Decision);
    }

    [Fact]
    public void Allows_when_a_provider_has_nothing_to_say_about_the_body()
    {
        var rule = new FeatureConstraintGateRule(new FakeConstraintProvider("BEAD"));

        var outcome = rule.Evaluate(Context("Anything"));

        Assert.Equal(RuleDecision.Allow, outcome.Decision);
    }

    [Fact]
    public void Allows_a_material_that_satisfies_every_constraint()
    {
        var rule = new FeatureConstraintGateRule(
            new FakeConstraintProvider("BEAD", AllowOnly("SPEC BA-1", "NOT_ALLOWED", "2024-O", "5052-O")));

        var outcome = rule.Evaluate(Context("5052-O"));

        Assert.Equal(RuleDecision.Allow, outcome.Decision);
    }

    [Fact]
    public void Blocks_a_material_a_constraint_rejects_and_reports_its_reason_code()
    {
        var rule = new FeatureConstraintGateRule(
            new FakeConstraintProvider("BEAD", AllowOnly("SPEC BA-1", "MATERIAL_NOT_ALLOWED_BY_BEAD", "2024-O")));

        var outcome = rule.Evaluate(Context("7075-T6"));

        Assert.Equal(RuleDecision.Block, outcome.Decision);
        Assert.Equal("MATERIAL_NOT_ALLOWED_BY_BEAD", outcome.ReasonCode);
    }

    [Fact]
    public void Block_message_names_the_material_and_the_feature_responsible()
    {
        var rule = new FeatureConstraintGateRule(
            new FakeConstraintProvider("BEAD", AllowOnly("Bead SPEC BA-1234", "NOT_ALLOWED", "2024-O")));

        var outcome = rule.Evaluate(Context("7075-T6"));

        // The user has to be able to tell which material was refused and which feature refused it,
        // otherwise the block is unactionable.
        Assert.Contains("7075-T6", outcome.Message);
        Assert.Contains("Bead SPEC BA-1234", outcome.Message);
    }

    [Fact]
    public void Reports_the_first_violated_constraint_when_several_fail()
    {
        var rule = new FeatureConstraintGateRule(
            new FakeConstraintProvider(
                "BEAD",
                AllowOnly("SPEC BA-1", "FIRST", "2024-O"),
                AllowOnly("SPEC BA-2", "SECOND", "2024-O")));

        var outcome = rule.Evaluate(Context("7075-T6"));

        Assert.Equal("FIRST", outcome.ReasonCode);
    }

    [Fact]
    public void A_violation_in_any_provider_blocks_the_assignment()
    {
        var rule = new FeatureConstraintGateRule(
            new FakeConstraintProvider("BEAD", AllowOnly("SPEC BA-1", "BEAD_SAYS_NO", "7075-T6")),
            new FakeConstraintProvider("LOUVER", AllowOnly("SPEC LV-9", "LOUVER_SAYS_NO", "2024-O")));

        // Satisfies the bead constraint but not the louver one.
        var outcome = rule.Evaluate(Context("7075-T6"));

        Assert.Equal(RuleDecision.Block, outcome.Decision);
        Assert.Equal("LOUVER_SAYS_NO", outcome.ReasonCode);
    }

    [Fact]
    public void Allows_only_when_every_provider_is_satisfied()
    {
        var rule = new FeatureConstraintGateRule(
            new FakeConstraintProvider("BEAD", AllowOnly("SPEC BA-1", "BEAD_SAYS_NO", "2024-O", "7075-T6")),
            new FakeConstraintProvider("LOUVER", AllowOnly("SPEC LV-9", "LOUVER_SAYS_NO", "2024-O")));

        var outcome = rule.Evaluate(Context("2024-O"));

        Assert.Equal(RuleDecision.Allow, outcome.Decision);
    }

    [Fact]
    public void Queries_providers_for_the_body_under_evaluation()
    {
        var provider = new FakeConstraintProvider("BEAD");
        var rule = new FeatureConstraintGateRule(provider);

        rule.Evaluate(Context("Anything"));

        Assert.Equal(new[] { TestFixtures.MakeBody("body-1").Id }, provider.QueriedBodies);
    }

    [Fact]
    public void Runs_at_order_150_between_body_type_and_reassignment_confirmation()
    {
        // The ordering is load-bearing: a material the body's features forbid must be rejected outright,
        // not offered to the user as a confirmation by the rule at 200.
        Assert.Equal(150, new FeatureConstraintGateRule().Order);
    }
}