using BANxOpen.Foundation.Core.RuleEngine;

namespace BANxOpen.Foundation.Core.Materials.Assignment;

/// <summary>A validation rule: decides whether an assignment is allowed, warned about, blocked, or needs user
/// confirmation. Implement this to add a business rule without touching the planner or any other rule, and register
/// it in the <see cref="Rules.IMaterialRuleModule"/> for its category. Thin specialization of the shared
/// BANxOpen.Foundation.Core.RuleEngine.IGateRule shape.</summary>
public interface IMaterialValidationRule : IGateRule<MaterialAssignmentRuleContext, RuleOutcome>
{
}
