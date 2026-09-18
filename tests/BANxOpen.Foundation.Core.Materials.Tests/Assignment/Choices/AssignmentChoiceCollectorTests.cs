using BANxOpen.Foundation.Contracts.Bodies;
using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Assignment.Choices;
using BANxOpen.Foundation.Core.Materials.Rules;
using BANxOpen.Foundation.Core.Materials.Tests.Assignment;
using static BANxOpen.Foundation.Core.Materials.Tests.Assignment.TestFixtures;

namespace BANxOpen.Foundation.Core.Materials.Tests.Assignment.Choices;

public class AssignmentChoiceCollectorTests
{
    private static readonly BodyInfo BodyA = MakeBody("a");
    private static readonly BodyInfo BodyB = MakeBody("b");

    private static MaterialAssignmentPlanningInput Input(params BodyInfo[] bodies) =>
        new(MakeMaterial(), bodies, new Dictionary<BodyId, BodyMaterialAssignment>());

    private static AssignmentPlan Plan(MaterialAssignmentPlanningInput input, params IMaterialValidationRule[] rules) =>
        new MaterialAssignmentPlanner(rules).Plan(input);

    private static IReadOnlyList<PendingAssignmentChoice> Collect(
        IAssignmentChoiceProvider provider,
        MaterialAssignmentPlanningInput input,
        AssignmentPlan plan,
        HashSet<BodyId>? confirmed = null) =>
        new AssignmentChoiceCollector(new[] { provider }).Collect(plan, input, confirmed ?? new HashSet<BodyId>());

    [Fact]
    public void With_no_providers_nothing_is_asked()
    {
        var input = Input(BodyA);

        Assert.Empty(new AssignmentChoiceCollector(Array.Empty<IAssignmentChoiceProvider>()).Collect(Plan(input), input, new HashSet<BodyId>()));
    }

    [Fact]
    public void A_provider_with_nothing_to_ask_raises_no_question()
    {
        var input = Input(BodyA);

        Assert.Empty(Collect(new FakeChoiceProvider(_ => null), input, Plan(input)));
    }

    [Fact]
    public void A_blocked_body_is_never_asked_about()
    {
        // Its assignment is not happening, so any answer would be collected and then thrown away.
        var input = Input(BodyA);
        var plan = Plan(input, FakeGateRule.AlwaysBlock("BLOCK", 1));

        Assert.Empty(Collect(new FakeChoiceProvider(), input, plan));
    }

    [Fact]
    public void A_body_whose_confirmation_was_declined_is_never_asked_about()
    {
        var input = Input(BodyA);
        var plan = Plan(input, FakeGateRule.AlwaysRequireConfirmation("CONFIRM", 1));

        Assert.Empty(Collect(new FakeChoiceProvider(), input, plan, confirmed: new HashSet<BodyId>()));
    }

    [Fact]
    public void A_body_whose_confirmation_was_given_is_asked_about()
    {
        var input = Input(BodyA);
        var plan = Plan(input, FakeGateRule.AlwaysRequireConfirmation("CONFIRM", 1));

        var pending = Collect(new FakeChoiceProvider(), input, plan, confirmed: new HashSet<BodyId> { BodyA.Id });

        Assert.Equal(new[] { BodyA.Id }, Assert.Single(pending).BodyIds);
    }

    [Fact]
    public void Bodies_sharing_a_group_key_are_asked_once_and_the_answer_covers_all_of_them()
    {
        var input = Input(BodyA, BodyB);

        var pending = Assert.Single(Collect(new FakeChoiceProvider(groupKey: "part"), input, Plan(input)));

        Assert.Equal(new[] { BodyA.Id, BodyB.Id }, pending.BodyIds);

        var answers = AssignmentChoiceAnswers.CreateBuilder().Answer(pending, "one").Build();
        Assert.True(answers.TryGet(FakeChoiceProvider.Id, BodyA.Id, out _));
        Assert.True(answers.TryGet(FakeChoiceProvider.Id, BodyB.Id, out _));
    }

    [Fact]
    public void Bodies_with_their_own_group_key_are_asked_separately()
    {
        var input = Input(BodyA, BodyB);

        var pending = Collect(new FakeChoiceProvider(groupKey: null), input, Plan(input));

        Assert.Equal(2, pending.Count);
        Assert.Equal(new[] { BodyA.Id, BodyB.Id }, pending.Select(p => Assert.Single(p.BodyIds)));
    }

    [Fact]
    public void A_choice_whose_option_cells_do_not_match_its_columns_is_a_wiring_mistake()
    {
        var input = Input(BodyA);
        var provider = new FakeChoiceProvider(context => Choice(context, new AssignmentChoiceOption("x", new[] { "only-one" })));

        var ex = Assert.Throws<InvalidOperationException>(() => Collect(provider, input, Plan(input)));
        Assert.Contains("2 column(s)", ex.Message);
    }

    [Fact]
    public void A_choice_that_preselects_an_option_it_does_not_have_is_a_wiring_mistake()
    {
        var input = Input(BodyA);
        var provider = new FakeChoiceProvider(context =>
            Choice(context) with { PreselectedOptionId = "nonexistent" });

        var ex = Assert.Throws<InvalidOperationException>(() => Collect(provider, input, Plan(input)));
        Assert.Contains("nonexistent", ex.Message);
    }

    [Fact]
    public void A_choice_that_auto_selects_an_option_it_does_not_have_is_a_wiring_mistake()
    {
        var input = Input(BodyA);
        var provider = new FakeChoiceProvider(context =>
            Choice(context) with { Auto = new AssignmentChoiceAutoSelection("nonexistent", "chosen") });

        Assert.Throws<InvalidOperationException>(() => Collect(provider, input, Plan(input)));
    }

    private static AssignmentChoice Choice(MaterialAssignmentRuleContext context, params AssignmentChoiceOption[] options) =>
        new(
            FakeChoiceProvider.Id,
            context.TargetBody.Id,
            "part",
            "Pick one",
            "Pick one",
            new[] { new AssignmentChoiceColumn("First"), new AssignmentChoiceColumn("Second") },
            options.Length > 0
                ? options
                : new[]
                {
                    new AssignmentChoiceOption("one", new[] { "1", "a" }, IsPreferred: true),
                    new AssignmentChoiceOption("two", new[] { "2", "b" }),
                });

    private sealed class FakeChoiceProvider : IAssignmentChoiceProvider
    {
        public const string Id = "TEST.CHOICE";

        private readonly Func<MaterialAssignmentRuleContext, AssignmentChoice?> _choiceFor;

        public FakeChoiceProvider(Func<MaterialAssignmentRuleContext, AssignmentChoice?> choiceFor) =>
            _choiceFor = choiceFor;

        /// <param name="groupKey">Null gives every body its own key, so each is asked separately.</param>
        public FakeChoiceProvider(string? groupKey = "part") =>
            _choiceFor = context => Choice(context) with { GroupKey = groupKey ?? context.TargetBody.Id.Value };

        public string ChoiceId => Id;

        public AssignmentChoice? ChoiceFor(MaterialAssignmentRuleContext context) => _choiceFor(context);
    }
}
