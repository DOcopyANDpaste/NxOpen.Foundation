namespace BANxOpen.Foundation.Core.Materials.Assignment;

public interface IMaterialAssignmentPlanner
{
    AssignmentPlan Plan(MaterialAssignmentPlanningInput input);
}
