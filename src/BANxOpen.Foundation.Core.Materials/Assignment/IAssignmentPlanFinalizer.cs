using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.Materials.Assignment.Choices;
using BANxOpen.Foundation.Core.RuleEngine;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

public interface IAssignmentPlanFinalizer : IPlanFinalizer<AssignmentPlan, MaterialAssignmentPlanningInput, BodyId, ExecutablePlan>
{
    /// <summary>Finalizes with the answers to the questions <see cref="IAssignmentChoiceCollector"/> raised. The
    /// inherited three-argument form passes no answers.</summary>
    ExecutablePlan Finalize(
        AssignmentPlan plan,
        MaterialAssignmentPlanningInput input,
        HashSet<BodyId> confirmedIds,
        AssignmentChoiceAnswers choiceAnswers);
}
