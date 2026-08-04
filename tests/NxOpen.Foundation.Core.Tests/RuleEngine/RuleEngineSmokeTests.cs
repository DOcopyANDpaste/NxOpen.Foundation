using NxOpen.Foundation.Core.RuleEngine;

namespace NxOpen.Foundation.Core.Tests.RuleEngine;

public class RuleEngineSmokeTests
{
    private sealed record FakeContext(int Value);

    private sealed class AllowIfPositiveRule : IGateRule<FakeContext, RuleOutcome>
    {
        public string RuleId => "AllowIfPositive";
        public int Order => 100;

        public RuleOutcome Evaluate(FakeContext context) =>
            context.Value > 0
                ? new RuleOutcome(RuleId, RuleDecision.Allow, null, null)
                : new RuleOutcome(RuleId, RuleDecision.Block, "NonPositive", "Value must be positive.");
    }

    private sealed class DoubleEffectRule : IEffectRule<FakeContext, int>
    {
        public string RuleId => "Double";
        public int Order => 100;

        public IReadOnlyList<int> GenerateEffects(FakeContext context) => new[] { context.Value * 2 };
    }

    private sealed class SumPlanner : IPlanner<IReadOnlyList<int>, int>
    {
        public int Plan(IReadOnlyList<int> input) => input.Sum();
    }

    private sealed class JoinFinalizer : IPlanFinalizer<int, IReadOnlyList<int>, int, string>
    {
        public string Finalize(int plan, IReadOnlyList<int> input, HashSet<int> confirmedIds) =>
            $"{plan}:{confirmedIds.Count}";
    }

    [Fact]
    public void IGateRule_EvaluatesAgainstContext()
    {
        IGateRule<FakeContext, RuleOutcome> rule = new AllowIfPositiveRule();

        Assert.Equal(RuleDecision.Allow, rule.Evaluate(new FakeContext(1)).Decision);
        Assert.Equal(RuleDecision.Block, rule.Evaluate(new FakeContext(-1)).Decision);
    }

    [Fact]
    public void IEffectRule_GeneratesEffectsFromContext()
    {
        IEffectRule<FakeContext, int> rule = new DoubleEffectRule();

        Assert.Equal(new[] { 6 }, rule.GenerateEffects(new FakeContext(3)));
    }

    [Fact]
    public void IPlanner_ProducesAPlanFromInput()
    {
        IPlanner<IReadOnlyList<int>, int> planner = new SumPlanner();

        Assert.Equal(6, planner.Plan(new[] { 1, 2, 3 }));
    }

    [Fact]
    public void IPlanFinalizer_CombinesPlanInputAndConfirmedIds()
    {
        IPlanFinalizer<int, IReadOnlyList<int>, int, string> finalizer = new JoinFinalizer();

        Assert.Equal("6:2", finalizer.Finalize(6, new[] { 1, 2, 3 }, new HashSet<int> { 1, 2 }));
    }
}
