using BANxOpen.Foundation.Core.Materials;
using BANxOpen.Foundation.Core.Materials.Assignment.Rules;
using BANxOpen.Foundation.Core.Materials.Constraints;
using BANxOpen.Foundation.Core.Materials.Tests.Assignment;
using static BANxOpen.Foundation.Core.Materials.Tests.Constraints.ConstraintFixtures;

namespace BANxOpen.Foundation.Core.Materials.Tests;

public class StandardMaterialRulesTests
{
    [Fact]
    public void Gates_are_returned_in_ascending_order()
    {
        var orders = StandardMaterialRules.Gates().Select(r => r.Order).ToList();

        Assert.Equal(orders.OrderBy(o => o), orders);
    }

    [Fact]
    public void Feature_constraints_are_gated_before_any_rule_that_asks_the_user_a_question()
    {
        // A material the body's features forbid must be refused outright, never surfaced as a
        // confirmation the user could click through.
        var gates = StandardMaterialRules.Gates();

        var constraintOrder = gates.Single(r => r is FeatureConstraintGateRule).Order;
        var confirmationOrder = gates.Single(r => r is RequireConfirmationOnReassignmentRule).Order;

        Assert.True(constraintOrder < confirmationOrder);
    }

    [Fact]
    public void Default_gates_include_the_constraint_rule_as_a_no_op()
    {
        // Wiring the seam into the shared baseline must not change behaviour for a caller that has
        // registered no feature domains.
        var body = TestFixtures.MakeBody("body-1");
        var context = new BANxOpen.Foundation.Core.Materials.Assignment.MaterialAssignmentRuleContext(
            TestFixtures.MakeMaterial("Anything"), body, null, new[] { body });

        var rule = StandardMaterialRules.Gates().Single(r => r is FeatureConstraintGateRule);

        Assert.Equal(BANxOpen.Foundation.Core.RuleEngine.RuleDecision.Allow, rule.Evaluate(context).Decision);
    }

    [Fact]
    public void Registered_providers_reach_the_constraint_rule()
    {
        var body = TestFixtures.MakeBody("body-1");
        var context = new BANxOpen.Foundation.Core.Materials.Assignment.MaterialAssignmentRuleContext(
            TestFixtures.MakeMaterial("7075-T6"), body, null, new[] { body });

        var gates = StandardMaterialRules.Gates(new[]
        {
            new FakeConstraintProvider("BEAD", AllowOnly("SPEC BA-1", "NOT_ALLOWED", "2024-O")),
        });

        var rule = gates.Single(r => r is FeatureConstraintGateRule);

        Assert.Equal(BANxOpen.Foundation.Core.RuleEngine.RuleDecision.Block, rule.Evaluate(context).Decision);
    }

    [Fact]
    public void Physical_property_sync_stays_unregistered()
    {
        // It generates SYNC_PHYSICAL_PROPERTY instructions that nothing executes. Kept, but deliberately
        // not wired -- see StandardMaterialRules.Effects.
        Assert.DoesNotContain(StandardMaterialRules.Effects(), r => r is SyncPhysicalPropertiesEffectRule);
    }

    [Fact]
    public void Coating_sync_is_registered_so_both_entry_points_colour_bodies_the_same_way()
    {
        Assert.Contains(StandardMaterialRules.Effects(), r => r is SyncCoatingDisplayMaterialEffectRule);
    }
}