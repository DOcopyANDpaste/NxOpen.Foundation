using BANxOpen.Foundation.Contracts.Bodies;
using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.Materials.Assignment;
using BANxOpen.Foundation.Core.Materials.Assignment.Choices;
using BANxOpen.Foundation.Core.Materials.Rules;
using BANxOpen.Foundation.Core.Materials.Rules.Display;
using BANxOpen.Foundation.Core.Materials.Rules.Features;
using BANxOpen.Foundation.Core.Materials.Rules.Standard;
using BANxOpen.Foundation.Core.Materials.Tests.Assignment;
using BANxOpen.Foundation.Core.RuleEngine;
using static BANxOpen.Foundation.Core.Materials.Tests.Rules.Features.ConstraintFixtures;

namespace BANxOpen.Foundation.Core.Materials.Tests.Rules;

public class MaterialRuleSetTests
{
    private const string DomainInstructionType = "DOMAIN_SYNC";

    private static MaterialRuleSet BaselineWith(params IMaterialRuleModule[] domains) =>
        MaterialRuleSet.From(MaterialRuleSet.Baseline().Concat(domains));

    private static MaterialAssignmentRuleContext Context(string materialName)
    {
        var body = TestFixtures.MakeBody("body-1");
        return new MaterialAssignmentRuleContext(TestFixtures.MakeMaterial(materialName), body, null, new[] { body });
    }

    private static TestFixtures.FakeEffectRule DomainEffect() =>
        new("DOMAIN_SYNC_RULE", MaterialRuleOrder.SideEffect.DomainState, ctx => new[]
        {
            new SideEffectInstruction(DomainInstructionType, ctx.TargetBody.Id, new Dictionary<string, object>()),
        })
        {
            InstructionTypes = new[] { DomainInstructionType },
        };

    [Fact]
    public void Baseline_runs_the_validation_rules_the_standard_set_always_ran_in_the_same_order()
    {
        Assert.Equal(
            new[] { "BLOCK_BODY_TYPE_RESTRICTION", "FEATURE_MATERIAL_CONSTRAINT", "CONFIRM_REASSIGNMENT", "VALIDATE_COATING_DISPLAY_MATERIAL" },
            BaselineWith().ValidationRules.Select(r => r.RuleId));
    }

    [Fact]
    public void Baseline_side_effects_are_display_material_sync_only()
    {
        Assert.Equal(new[] { "SYNC_COATING_DISPLAY_MATERIAL" }, BaselineWith().SideEffectRules.Select(r => r.RuleId));
    }

    [Fact]
    public void Validation_rules_are_returned_in_ascending_order()
    {
        var orders = BaselineWith().ValidationRules.Select(r => r.Order).ToList();

        Assert.Equal(orders.OrderBy(o => o), orders);
    }

    [Fact]
    public void Feature_constraints_are_gated_before_any_rule_that_asks_the_user_a_question()
    {
        // A material the body's features forbid must be refused outright, never surfaced as a
        // confirmation the user could click through.
        var rules = BaselineWith().ValidationRules;

        var constraintOrder = rules.Single(r => r is FeatureConstraintGateRule).Order;
        var confirmationOrder = rules.Single(r => r is RequireConfirmationOnReassignmentRule).Order;

        Assert.True(constraintOrder < confirmationOrder);
    }

    [Fact]
    public void Without_feature_constraints_the_gate_is_a_no_op()
    {
        // The gate is always present, and must not change behaviour for a caller that has registered no
        // feature domains.
        var rule = BaselineWith().ValidationRules.Single(r => r is FeatureConstraintGateRule);

        Assert.Equal(RuleDecision.Allow, rule.Evaluate(Context("Anything")).Decision);
    }

    [Fact]
    public void Feature_constraints_from_every_module_reach_one_gate()
    {
        var permissive = new FakeConstraintProvider("A", AllowOnly("SPEC A", "NOT_ALLOWED", "7075-T6"));
        var restrictive = new FakeConstraintProvider("B", AllowOnly("SPEC B", "NOT_ALLOWED", "2024-O"));

        var rules = BaselineWith(
            new FakeModule("TEST.A", constraints: new IFeatureMaterialConstraintProvider[] { permissive }),
            new FakeModule("TEST.B", constraints: new IFeatureMaterialConstraintProvider[] { restrictive }));

        var gate = Assert.Single(rules.ValidationRules.OfType<FeatureConstraintGateRule>());

        Assert.Equal(RuleDecision.Block, gate.Evaluate(Context("7075-T6")).Decision);
        Assert.Single(permissive.QueriedBodies);
        Assert.Single(restrictive.QueriedBodies);
    }

    [Fact]
    public void Physical_property_sync_stays_unregistered()
    {
        // It generates SYNC_PHYSICAL_PROPERTY instructions that nothing executes. Kept, but deliberately
        // not wired -- see StandardRuleModule.
        Assert.DoesNotContain(BaselineWith().SideEffectRules, r => r is SyncPhysicalPropertiesEffectRule);
    }

    [Fact]
    public void Coating_sync_is_registered_so_every_entry_point_colours_bodies_the_same_way()
    {
        Assert.Contains(BaselineWith().SideEffectRules, r => r is SyncCoatingDisplayMaterialEffectRule);
    }

    [Fact]
    public void A_domain_side_effect_runs_after_display_material_sync()
    {
        var rules = BaselineWith(new FakeModule("TEST.DOMAIN", effects: new IPostAssignmentEffectRule[] { DomainEffect() }));

        Assert.Equal(new[] { "SYNC_COATING_DISPLAY_MATERIAL", "DOMAIN_SYNC_RULE" }, rules.SideEffectRules.Select(r => r.RuleId));
    }

    [Fact]
    public void The_finalizer_carries_side_effects_from_every_module()
    {
        var rules = BaselineWith(new FakeModule("TEST.DOMAIN", effects: new IPostAssignmentEffectRule[] { DomainEffect() }));
        var body = TestFixtures.MakeBody("body-1");
        var input = new MaterialAssignmentPlanningInput(
            TestFixtures.MakeMaterial("Steel"), new[] { body }, new Dictionary<BodyId, BodyMaterialAssignment>());

        var plan = rules.CreatePlanner().Plan(input);
        var executable = rules.CreateFinalizer().Finalize(plan, input, new HashSet<BodyId>());

        var assignment = Assert.Single(executable.Assignments);
        Assert.Equal(
            new[] { SyncCoatingDisplayMaterialEffectRule.InstructionType, DomainInstructionType },
            assignment.SideEffects.Select(e => e.InstructionType));
    }

    [Fact]
    public void A_module_registered_twice_is_refused()
    {
        var ex = Assert.Throws<ArgumentException>(() => BaselineWith(new FakeModule(DisplayMaterialRuleModule.Id)));

        Assert.Contains(DisplayMaterialRuleModule.Id, ex.Message);
    }

    [Fact]
    public void A_rule_id_already_registered_by_another_module_is_refused()
    {
        var duplicate = TestFixtures.FakeGateRule.AlwaysAllow("CONFIRM_REASSIGNMENT", 500);

        var ex = Assert.Throws<ArgumentException>(() =>
            BaselineWith(new FakeModule("TEST.DOMAIN", validation: new IMaterialValidationRule[] { duplicate })));

        Assert.Contains("CONFIRM_REASSIGNMENT", ex.Message);
        Assert.Contains(StandardRuleModule.Id, ex.Message);
    }

    [Fact]
    public void A_choice_id_already_registered_by_another_module_is_refused()
    {
        // Answers are keyed by choice id, so two providers sharing one would each read the other's answer.
        var first = new FakeModule("TEST.A", choices: new IAssignmentChoiceProvider[] { new FakeChoiceProvider("PICK") });
        var second = new FakeModule("TEST.B", choices: new IAssignmentChoiceProvider[] { new FakeChoiceProvider("PICK") });

        var ex = Assert.Throws<ArgumentException>(() => BaselineWith(first, second));

        Assert.Contains("PICK", ex.Message);
        Assert.Contains("TEST.A", ex.Message);
    }

    [Fact]
    public void The_choice_collector_is_built_from_every_modules_providers()
    {
        var rules = BaselineWith(
            new FakeModule("TEST.A", choices: new IAssignmentChoiceProvider[] { new FakeChoiceProvider("PICK_A") }),
            new FakeModule("TEST.B", choices: new IAssignmentChoiceProvider[] { new FakeChoiceProvider("PICK_B") }));

        Assert.Equal(new[] { "PICK_A", "PICK_B" }, rules.ChoiceProviders.Select(p => p.ChoiceId));
        Assert.NotNull(rules.CreateChoiceCollector());
    }

    [Fact]
    public void A_module_cannot_add_a_second_feature_constraint_gate()
    {
        // Feature rules go in FeatureConstraints; a second gate would split block-beats-warn across two rules.
        Assert.Throws<ArgumentException>(() =>
            BaselineWith(new FakeModule("TEST.DOMAIN", validation: new IMaterialValidationRule[] { new FeatureConstraintGateRule() })));
    }

    [Fact]
    public void A_cached_planner_reads_each_bodys_constraints_once_across_candidates()
    {
        var provider = new FakeConstraintProvider("A", AllowOnly("SPEC A", "NOT_ALLOWED", "2024-O"));
        var rules = BaselineWith(new FakeModule("TEST.A", constraints: new IFeatureMaterialConstraintProvider[] { provider }));

        new AssignableMaterialQuery(rules.CreatePlanner(cacheFeatureConstraints: true))
            .Evaluate(TestFixtures.MakeBody("body-1"), null, Candidates());

        Assert.Single(provider.QueriedBodies);
    }

    [Fact]
    public void Each_cached_planner_starts_with_an_empty_cache()
    {
        // Constraints come from live model state; a cache shared between listings would answer from a model
        // the user has since changed.
        var provider = new FakeConstraintProvider("A", AllowOnly("SPEC A", "NOT_ALLOWED", "2024-O"));
        var rules = BaselineWith(new FakeModule("TEST.A", constraints: new IFeatureMaterialConstraintProvider[] { provider }));
        var body = TestFixtures.MakeBody("body-1");

        new AssignableMaterialQuery(rules.CreatePlanner(cacheFeatureConstraints: true)).Evaluate(body, null, Candidates());
        new AssignableMaterialQuery(rules.CreatePlanner(cacheFeatureConstraints: true)).Evaluate(body, null, Candidates());

        Assert.Equal(2, provider.QueriedBodies.Count);
    }

    [Fact]
    public void An_uncached_planner_asks_the_provider_on_every_plan()
    {
        var provider = new FakeConstraintProvider("A", AllowOnly("SPEC A", "NOT_ALLOWED", "2024-O"));
        var rules = BaselineWith(new FakeModule("TEST.A", constraints: new IFeatureMaterialConstraintProvider[] { provider }));

        new AssignableMaterialQuery(rules.CreatePlanner()).Evaluate(TestFixtures.MakeBody("body-1"), null, Candidates());

        Assert.Equal(Candidates().Count(), provider.QueriedBodies.Count);
    }

    [Fact]
    public void A_side_effect_with_no_executor_is_refused_naming_its_type_and_module()
    {
        var rules = BaselineWith(new FakeModule("TEST.DOMAIN", effects: new IPostAssignmentEffectRule[] { DomainEffect() }));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            rules.EnsureExecutorsFor(new[] { SyncCoatingDisplayMaterialEffectRule.InstructionType }));

        Assert.Contains(DomainInstructionType, ex.Message);
        Assert.Contains("TEST.DOMAIN", ex.Message);
    }

    [Fact]
    public void The_baseline_display_sync_needs_its_executor_too()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BaselineWith().EnsureExecutorsFor(Array.Empty<string>()));

        Assert.Contains(SyncCoatingDisplayMaterialEffectRule.InstructionType, ex.Message);
    }

    [Fact]
    public void Executors_covering_every_emitted_type_are_accepted()
    {
        var rules = BaselineWith(new FakeModule("TEST.DOMAIN", effects: new IPostAssignmentEffectRule[] { DomainEffect() }));

        rules.EnsureExecutorsFor(new[] { SyncCoatingDisplayMaterialEffectRule.InstructionType, DomainInstructionType });
    }

    private static IEnumerable<BANxOpen.Foundation.Contracts.Materials.Material> Candidates() =>
        new[] { "2024-O", "5052-O", "7075-T6" }.Select(name => TestFixtures.MakeMaterial(name));

    private sealed class FakeModule : IMaterialRuleModule
    {
        public FakeModule(
            string moduleId,
            IMaterialValidationRule[]? validation = null,
            IFeatureMaterialConstraintProvider[]? constraints = null,
            IPostAssignmentEffectRule[]? effects = null,
            IAssignmentChoiceProvider[]? choices = null)
        {
            ModuleId = moduleId;
            ValidationRules = validation ?? Array.Empty<IMaterialValidationRule>();
            FeatureConstraints = constraints ?? Array.Empty<IFeatureMaterialConstraintProvider>();
            SideEffectRules = effects ?? Array.Empty<IPostAssignmentEffectRule>();
            ChoiceProviders = choices ?? Array.Empty<IAssignmentChoiceProvider>();
        }

        public string ModuleId { get; }

        public IReadOnlyList<IMaterialValidationRule> ValidationRules { get; }

        public IReadOnlyList<IFeatureMaterialConstraintProvider> FeatureConstraints { get; }

        public IReadOnlyList<IPostAssignmentEffectRule> SideEffectRules { get; }

        public IReadOnlyList<IAssignmentChoiceProvider> ChoiceProviders { get; }
    }

    private sealed class FakeChoiceProvider : IAssignmentChoiceProvider
    {
        public FakeChoiceProvider(string choiceId) => ChoiceId = choiceId;

        public string ChoiceId { get; }

        public AssignmentChoice? ChoiceFor(MaterialAssignmentRuleContext context) => null;
    }
}
