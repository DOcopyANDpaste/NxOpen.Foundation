using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.RuleEngine;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

public interface IAssignmentPlanFinalizer : IPlanFinalizer<AssignmentPlan, MaterialAssignmentPlanningInput, BodyId, ExecutablePlan>
{
}
