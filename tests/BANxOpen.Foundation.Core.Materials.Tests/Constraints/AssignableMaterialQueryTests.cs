using BANxOpen.Foundation.Contracts.Bodies;
using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Constraints;
using BANxOpen.Foundation.Core.Materials.Tests.Assignment;
using static BANxOpen.Foundation.Core.Materials.Tests.Constraints.ConstraintFixtures;

namespace BANxOpen.Foundation.Core.Materials.Tests.Constraints;

public class AssignableMaterialQueryTests
{
    private static readonly BodyInfo Body = TestFixtures.MakeBody("body-1");

    private static AssignableMaterialQuery QueryWith(params IFeatureMaterialConstraintProvider[] providers) =>
        new(new MaterialAssignmentPlanner(new[] { new FeatureConstraintGateRule(providers) }));

    private static readonly string[] CandidateNames = { "2024-O", "5052-O", "7075-T6" };

    private static IEnumerable<BANxOpen.Foundation.Contracts.Materials.Material> Candidates() =>
        CandidateNames.Select(n => TestFixtures.MakeMaterial(n));

    [Fact]
    public void Returns_every_candidate_when_nothing_constrains_the_body()
    {
        var query = QueryWith();

        var assignable = query.ListAssignable(Body, null, Candidates());

        Assert.Equal(CandidateNames, assignable.Select(m => m.Name));
    }

    [Fact]
    public void Returns_exactly_the_candidates_the_constraint_permits()
    {
        var query = QueryWith(
            new FakeConstraintProvider("BEAD", AllowOnly("SPEC BA-1", "NOT_ALLOWED", "2024-O", "5052-O")));

        var assignable = query.ListAssignable(Body, null, Candidates());

        Assert.Equal(new[] { "2024-O", "5052-O" }, assignable.Select(m => m.Name));
    }

    [Fact]
    public void Filtering_agrees_with_gating_for_every_candidate()
    {
        // The property that matters: a material this query offers must not then be refused on Apply, and
        // one it withholds must genuinely be refused. Both answers come from the same planner, so this
        // asserts they cannot diverge.
        var providers = new IFeatureMaterialConstraintProvider[]
        {
            new FakeConstraintProvider("BEAD", AllowOnly("SPEC BA-1", "NOT_ALLOWED", "5052-O")),
        };
        var gate = new FeatureConstraintGateRule(providers);
        var query = new AssignableMaterialQuery(new MaterialAssignmentPlanner(new[] { gate }));

        foreach (var result in query.Evaluate(Body, null, Candidates()))
        {
            var gateOutcome = gate.Evaluate(
                new MaterialAssignmentRuleContext(result.Material, Body, null, new[] { Body }));

            Assert.Equal(
                gateOutcome.Decision != BANxOpen.Foundation.Core.RuleEngine.RuleDecision.Block,
                result.IsAssignable);
        }
    }

    [Fact]
    public void Evaluate_keeps_blocked_candidates_so_a_caller_can_grey_them_out()
    {
        var query = QueryWith(
            new FakeConstraintProvider("BEAD", AllowOnly("SPEC BA-1", "NOT_ALLOWED", "2024-O")));

        var results = query.Evaluate(Body, null, Candidates());

        Assert.Equal(3, results.Count);
        Assert.Equal(CandidateNames, results.Select(r => r.Material.Name));
        Assert.Single(results.Where(r => r.IsAssignable));
    }

    [Fact]
    public void A_blocked_candidate_carries_a_reason_naming_the_feature()
    {
        var query = QueryWith(
            new FakeConstraintProvider("BEAD", AllowOnly("Bead SPEC BA-1234", "NOT_ALLOWED", "2024-O")));

        var blocked = query.Evaluate(Body, null, Candidates()).Single(r => r.Material.Name == "7075-T6");

        Assert.False(blocked.IsAssignable);
        Assert.Contains("Bead SPEC BA-1234", blocked.BlockedReason);
    }

    [Fact]
    public void An_assignable_candidate_has_no_blocked_reason()
    {
        var query = QueryWith();

        var result = query.Evaluate(Body, null, Candidates()).First();

        Assert.True(result.IsAssignable);
        Assert.Null(result.BlockedReason);
    }

    [Fact]
    public void Passes_the_bodys_current_assignment_through_to_the_rules()
    {
        // Rules that compare against the body's existing material (reassignment confirmation, coating
        // validation) are useless if the query plans every candidate as though the body were bare.
        var current = new BodyMaterialAssignment(Body.Id, "5052-O", new MaterialId("5052-O"));
        BodyMaterialAssignment? seen = null;
        var planner = new MaterialAssignmentPlanner(new[]
        {
            new TestFixtures.FakeGateRule("SPY", 100, ctx =>
            {
                seen = ctx.CurrentAssignment;
                return new BANxOpen.Foundation.Core.RuleEngine.RuleOutcome(
                    "SPY", BANxOpen.Foundation.Core.RuleEngine.RuleDecision.Allow, null, null);
            }),
        });

        new AssignableMaterialQuery(planner).Evaluate(Body, current, Candidates().Take(1));

        Assert.Same(current, seen);
    }

    [Fact]
    public void Caching_provider_reads_the_model_once_no_matter_how_many_candidates_are_tried()
    {
        // Filtering a library plans one body against every candidate; without memoisation that is one
        // model read per candidate.
        var inner = new FakeConstraintProvider("BEAD", AllowOnly("SPEC BA-1", "NOT_ALLOWED", "2024-O"));
        var query = QueryWith(new CachingFeatureConstraintProvider(inner));

        query.ListAssignable(Body, null, Candidates());

        Assert.Single(inner.QueriedBodies);
    }

    [Fact]
    public void Uncached_provider_is_asked_once_per_candidate()
    {
        // The companion to the test above: states the cost the cache exists to remove.
        var inner = new FakeConstraintProvider("BEAD", AllowOnly("SPEC BA-1", "NOT_ALLOWED", "2024-O"));
        var query = QueryWith(inner);

        query.ListAssignable(Body, null, Candidates());

        Assert.Equal(CandidateNames.Length, inner.QueriedBodies.Count);
    }

    [Fact]
    public void Handles_an_empty_candidate_set()
    {
        var query = QueryWith();

        Assert.Empty(query.ListAssignable(Body, null, Array.Empty<BANxOpen.Foundation.Contracts.Materials.Material>()));
    }
}