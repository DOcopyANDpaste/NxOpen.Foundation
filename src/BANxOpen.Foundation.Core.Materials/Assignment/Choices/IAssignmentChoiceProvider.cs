namespace BANxOpen.Foundation.Core.Materials.Assignment.Choices;

/// <summary>Asks the user something a side-effect rule needs decided before it can act. Pure: returning the choice
/// is how it asks. The matching <see cref="Rules.IPostAssignmentEffectRule"/> reads the answer from
/// <see cref="MaterialAssignmentRuleContext.ChoiceAnswers"/> under the same <see cref="ChoiceId"/>.</summary>
public interface IAssignmentChoiceProvider
{
    /// <summary>Stable identifier, e.g. "SHEETMETAL.PREFERENCE_ROW". Unique within a
    /// <see cref="Rules.MaterialRuleSet"/>.</summary>
    string ChoiceId { get; }

    /// <summary>The question for this (body, requested material) pair, or null when there is nothing to ask.
    /// Must be deterministic: the finalizer calls it again to check the answer is present.</summary>
    AssignmentChoice? ChoiceFor(MaterialAssignmentRuleContext context);
}
