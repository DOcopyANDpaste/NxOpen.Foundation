using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Assignment.Choices;
using BANxOpen.Foundation.Core.Materials.Bodies;
using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.RuleEngine;
using static BANxOpen.Foundation.Core.Materials.Tests.Assignment.TestFixtures;
using BANxOpen.Foundation.Contracts.Bodies;

namespace BANxOpen.Foundation.Core.Materials.Tests.Assignment;

public class AssignmentPlanFinalizerTests
{
    private static AssignmentPlan MakePlan(params BodyAssignmentEvaluation[] evaluations) =>
        new("plan-1", new MaterialId("mat"), evaluations);

    private static BodyAssignmentEvaluation Allowed(string bodyId) =>
        new(new BodyId(bodyId), new[] { new RuleOutcome("r", RuleDecision.Allow, null, null) });

    private static BodyAssignmentEvaluation Blocked(string bodyId) =>
        new(new BodyId(bodyId), new[] { new RuleOutcome("r", RuleDecision.Block, "X", "x") });

    private static BodyAssignmentEvaluation NeedsConfirmation(string bodyId) =>
        new(new BodyId(bodyId), new[] { new RuleOutcome("r", RuleDecision.RequireConfirmation, "C", "c") });

    [Fact]
    public void Finalize_PartialApply_SkipsBlockedAndDeclinedButAppliesTheRest()
    {
        var plan = MakePlan(
            Allowed("clean"),
            Blocked("blocked"),
            NeedsConfirmation("declined"),
            NeedsConfirmation("confirmed"));

        var bodies = new[] { MakeBody("clean"), MakeBody("blocked"), MakeBody("declined"), MakeBody("confirmed") };
        var input = new MaterialAssignmentPlanningInput(MakeMaterial(), bodies, new Dictionary<BodyId, BodyMaterialAssignment>());
        var finalizer = new AssignmentPlanFinalizer(Array.Empty<IPostAssignmentEffectRule>());

        var confirmedIds = new HashSet<BodyId> { new BodyId("confirmed") };
        var executablePlan = finalizer.Finalize(plan, input, confirmedIds);

        Assert.Equal(new[] { "clean", "confirmed" }, executablePlan.Assignments.Select(a => a.BodyId.Value));
        Assert.Equal(new[] { "blocked" }, executablePlan.SkippedBlocked.Select(b => b.Value));
        Assert.Equal(new[] { "declined" }, executablePlan.SkippedDeclinedConfirmation.Select(b => b.Value));
    }

    [Fact]
    public void Finalize_SkipsBodiesMissingFromTheInputInsteadOfThrowing()
    {
        // The part can be rescanned between planning and finalizing, dropping a body the plan still
        // names. That must not take down the bodies alongside it.
        var plan = MakePlan(Allowed("present"), Allowed("vanished"));
        var input = new MaterialAssignmentPlanningInput(
            MakeMaterial(), new[] { MakeBody("present") }, new Dictionary<BodyId, BodyMaterialAssignment>());
        var finalizer = new AssignmentPlanFinalizer(Array.Empty<IPostAssignmentEffectRule>());

        var executablePlan = finalizer.Finalize(plan, input, new HashSet<BodyId>());

        Assert.Equal(new[] { "present" }, executablePlan.Assignments.Select(a => a.BodyId.Value));
        Assert.Equal(new[] { "vanished" }, executablePlan.SkippedBlocked.Select(b => b.Value));
    }

    [Fact]
    public void Finalize_PreservesPlanIdOnTheExecutablePlan()
    {
        var plan = MakePlan(Allowed("b1"));
        var input = new MaterialAssignmentPlanningInput(MakeMaterial(), new[] { MakeBody("b1") }, new Dictionary<BodyId, BodyMaterialAssignment>());
        var finalizer = new AssignmentPlanFinalizer(Array.Empty<IPostAssignmentEffectRule>());

        var executablePlan = finalizer.Finalize(plan, input, new HashSet<BodyId>());

        Assert.Equal(plan.PlanId, executablePlan.PlanId);
    }

    [Fact]
    public void Finalize_OnlyRunsEffectRulesForBodiesThatAreActuallyAssigned()
    {
        var plan = MakePlan(Allowed("clean"), Blocked("blocked"));
        var input = new MaterialAssignmentPlanningInput(
            MakeMaterial(), new[] { MakeBody("clean"), MakeBody("blocked") }, new Dictionary<BodyId, BodyMaterialAssignment>());

        var effectRule = new FakeEffectRule("effect", 100, ctx => Array.Empty<SideEffectInstruction>());
        var finalizer = new AssignmentPlanFinalizer(new[] { effectRule });

        finalizer.Finalize(plan, input, new HashSet<BodyId>());

        Assert.Equal(new[] { "clean" }, effectRule.InvokedForBodies.Select(b => b.Value));
    }

    [Fact]
    public void Finalize_AggregatesSideEffectsFromMultipleEffectRulesOntoTheSameAssignment()
    {
        var plan = MakePlan(Allowed("b1"));
        var input = new MaterialAssignmentPlanningInput(MakeMaterial(), new[] { MakeBody("b1") }, new Dictionary<BodyId, BodyMaterialAssignment>());

        var effectA = new FakeEffectRule("a", 100, ctx => new[]
        {
            new SideEffectInstruction("TYPE_A", ctx.TargetBody.Id, new Dictionary<string, object>()),
        });
        var effectB = new FakeEffectRule("b", 200, ctx => new[]
        {
            new SideEffectInstruction("TYPE_B", ctx.TargetBody.Id, new Dictionary<string, object>()),
        });
        var finalizer = new AssignmentPlanFinalizer(new IPostAssignmentEffectRule[] { effectB, effectA });

        var executablePlan = finalizer.Finalize(plan, input, new HashSet<BodyId>());

        var assignment = Assert.Single(executablePlan.Assignments);
        Assert.Equal(new[] { "TYPE_A", "TYPE_B" }, assignment.SideEffects.Select(e => e.InstructionType));
    }

    // ---- Choice answers ----

    [Fact]
    public void Finalize_PassesTheChoiceAnswersToTheEffectRules()
    {
        var plan = MakePlan(Allowed("b1"));
        var input = new MaterialAssignmentPlanningInput(MakeMaterial(), new[] { MakeBody("b1") }, new Dictionary<BodyId, BodyMaterialAssignment>());

        string? seen = null;
        var effectRule = new FakeEffectRule("effect", 100, ctx =>
        {
            ctx.ChoiceAnswers.TryGet("TEST.CHOICE", ctx.TargetBody.Id, out var option);
            seen = option;
            return Array.Empty<SideEffectInstruction>();
        });

        var answers = AssignmentChoiceAnswers.CreateBuilder()
            .AnswerDirectly("TEST.CHOICE", new BodyId("b1"), "picked")
            .Build();

        new AssignmentPlanFinalizer(new[] { effectRule }).Finalize(plan, input, new HashSet<BodyId>(), answers);

        Assert.Equal("picked", seen);
    }

    [Fact]
    public void Finalize_WithoutAnswersLeavesTheRulesAnEmptyBagRatherThanNull()
    {
        var plan = MakePlan(Allowed("b1"));
        var input = new MaterialAssignmentPlanningInput(MakeMaterial(), new[] { MakeBody("b1") }, new Dictionary<BodyId, BodyMaterialAssignment>());

        var effectRule = new FakeEffectRule("effect", 100, ctx =>
        {
            Assert.False(ctx.ChoiceAnswers.TryGet("TEST.CHOICE", ctx.TargetBody.Id, out _));
            return Array.Empty<SideEffectInstruction>();
        });

        new AssignmentPlanFinalizer(new[] { effectRule }).Finalize(plan, input, new HashSet<BodyId>());

        Assert.Equal(new[] { "b1" }, effectRule.InvokedForBodies.Select(b => b.Value));
    }

    [Fact]
    public void Finalize_SkipsABodyWhoseChoiceWasRaisedAndNeverAnswered()
    {
        // The effect rules would run without something they said they needed, producing a half-done
        // assignment — in the sheet metal case, a body that gets its material while the part's preferences
        // are left pointing at the old one.
        var plan = MakePlan(Allowed("answered"), Allowed("unanswered"));
        var bodies = new[] { MakeBody("answered"), MakeBody("unanswered") };
        var input = new MaterialAssignmentPlanningInput(MakeMaterial(), bodies, new Dictionary<BodyId, BodyMaterialAssignment>());

        var answers = AssignmentChoiceAnswers.CreateBuilder()
            .AnswerDirectly(AlwaysAsks.Id, new BodyId("answered"), "x")
            .Build();

        var executablePlan = new AssignmentPlanFinalizer(Array.Empty<IPostAssignmentEffectRule>(), new[] { new AlwaysAsks() })
            .Finalize(plan, input, new HashSet<BodyId>(), answers);

        Assert.Equal(new[] { "answered" }, executablePlan.Assignments.Select(a => a.BodyId.Value));
        Assert.Equal(new[] { "unanswered" }, executablePlan.SkippedUnresolvedChoice.Select(b => b.Value));

        // Kept apart from the declined list: this is a caller that forgot to ask, not a user who said no.
        Assert.Empty(executablePlan.SkippedDeclinedConfirmation);
    }

    [Fact]
    public void Finalize_WithoutAnswersSkipsEveryBodyAProviderAsksAbout()
    {
        // A caller that never ran the collector must not assign bodies whose choices went unasked.
        var plan = MakePlan(Allowed("b1"));
        var input = new MaterialAssignmentPlanningInput(MakeMaterial(), new[] { MakeBody("b1") }, new Dictionary<BodyId, BodyMaterialAssignment>());

        var executablePlan = new AssignmentPlanFinalizer(Array.Empty<IPostAssignmentEffectRule>(), new[] { new AlwaysAsks() })
            .Finalize(plan, input, new HashSet<BodyId>());

        Assert.Empty(executablePlan.Assignments);
        Assert.Equal(new[] { "b1" }, executablePlan.SkippedUnresolvedChoice.Select(b => b.Value));
    }

    [Fact]
    public void Finalize_SkipsABodyConfirmedAfterTheChoicesWereCollected()
    {
        // Collected with nothing confirmed, so "late" was never asked about; confirmed only at finalize time.
        var plan = MakePlan(Allowed("early"), NeedsConfirmation("late"));
        var input = new MaterialAssignmentPlanningInput(
            MakeMaterial(), new[] { MakeBody("early"), MakeBody("late") }, new Dictionary<BodyId, BodyMaterialAssignment>());
        var providers = new[] { new AlwaysAsks() };

        var pending = new AssignmentChoiceCollector(providers).Collect(plan, input, new HashSet<BodyId>());
        var builder = AssignmentChoiceAnswers.CreateBuilder();
        foreach (var question in pending)
            builder.Answer(question, "x");

        var executablePlan = new AssignmentPlanFinalizer(Array.Empty<IPostAssignmentEffectRule>(), providers)
            .Finalize(plan, input, new HashSet<BodyId> { new BodyId("late") }, builder.Build());

        Assert.Equal(new[] { "early" }, executablePlan.Assignments.Select(a => a.BodyId.Value));
        Assert.Equal(new[] { "late" }, executablePlan.SkippedUnresolvedChoice.Select(b => b.Value));
    }

    private sealed class AlwaysAsks : IAssignmentChoiceProvider
    {
        public const string Id = "TEST.CHOICE";

        public string ChoiceId => Id;

        public AssignmentChoice? ChoiceFor(MaterialAssignmentRuleContext context) =>
            new(Id, context.TargetBody.Id, context.TargetBody.Id.Value, "Pick", "Pick",
                new[] { new AssignmentChoiceColumn("Only") },
                new[] { new AssignmentChoiceOption("x", new[] { "x" }) });
    }
}
