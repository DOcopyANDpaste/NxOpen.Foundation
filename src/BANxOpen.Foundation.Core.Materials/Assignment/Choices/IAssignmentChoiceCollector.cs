using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Assignment.Choices;

/// <summary>Gathers every question the registered <see cref="IAssignmentChoiceProvider"/>s have about one Apply,
/// for the UI to put to the user between planning and finalizing.</summary>
public interface IAssignmentChoiceCollector
{
    /// <param name="confirmedBodyIds">The same set that will be passed to
    /// <see cref="AssignmentPlanFinalizer.Finalize(AssignmentPlan, MaterialAssignmentPlanningInput,
    /// HashSet{BodyId}, AssignmentChoiceAnswers)"/>, so that the bodies asked about are exactly the bodies
    /// assigned.</param>
    /// <returns>One entry per distinct question, in provider order; empty when nothing needs deciding.</returns>
    IReadOnlyList<PendingAssignmentChoice> Collect(
        AssignmentPlan plan,
        MaterialAssignmentPlanningInput input,
        HashSet<BodyId> confirmedBodyIds);
}
