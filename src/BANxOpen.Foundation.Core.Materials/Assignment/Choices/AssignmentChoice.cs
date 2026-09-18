using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Assignment.Choices;

/// <summary>One column of the table a choice's options are shown in. <see cref="IsNumeric"/> is a rendering hint
/// only — the cell values are strings the domain has already formatted, because only the domain knows how many
/// decimals a thickness is meaningful to.</summary>
public sealed record AssignmentChoiceColumn(string Header, bool IsNumeric = false);

/// <summary>One thing the user can pick.</summary>
/// <param name="OptionId">Stable, domain-defined, and unique within its choice. It is what comes back in
/// <see cref="AssignmentChoiceAnswers"/>, so the rule that emitted the choice has to be able to resolve it back to
/// whatever it stands for — the sheet metal domain uses the standards file row name.</param>
/// <param name="Cells">One per <see cref="AssignmentChoice.Columns"/>, in the same order.</param>
/// <param name="IsPreferred">Whether this option fits the body the assignment targets. Drives the UI's
/// "preferred only" filter; it never restricts what may be picked.</param>
/// <param name="ConfirmationPrompt">When non-null, the UI must ask this question and get a yes before accepting
/// the option. For an option that is allowed but questionable — a sheet metal row whose thickness is not the
/// body's. The text is shown verbatim, so it must make sense on its own.</param>
public sealed record AssignmentChoiceOption(
    string OptionId,
    IReadOnlyList<string> Cells,
    bool IsPreferred = false,
    string? ConfirmationPrompt = null);

/// <summary>A choice the domain made on the user's behalf. The UI shows <see cref="Message"/> and does not
/// prompt.</summary>
/// <param name="OptionId">Must be one of the choice's <see cref="AssignmentChoice.Options"/>.</param>
/// <param name="Message">Why this option was chosen, for the information window. Names the option, since the
/// user never sees the list it came from.</param>
public sealed record AssignmentChoiceAutoSelection(string OptionId, string Message);

/// <summary>Something only the user can decide before a rule can generate its side effects, shown as a table of
/// domain-formatted string cells so the UI needs no domain knowledge.</summary>
/// <param name="ChoiceId">Which provider is asking, e.g. "SHEETMETAL.PREFERENCE_ROW". Unique across a
/// <see cref="Rules.MaterialRuleSet"/>.</param>
/// <param name="BodyId">The body this choice was computed for; for a grouped question, the first one seen.</param>
/// <param name="GroupKey">Bodies whose choices share a <see cref="ChoiceId"/> and a <see cref="GroupKey"/> are
/// asked once and share the answer — a constant for per-part choices, the body id for per-body ones.</param>
/// <param name="Title">Window title.</param>
/// <param name="Prompt">One or two sentences above the table saying what is being decided and for what. Shown
/// verbatim.</param>
/// <param name="Columns">The table's columns, left to right.</param>
/// <param name="Options">Every option, in the order the domain wants them listed. Never empty: a provider with
/// nothing to ask returns null instead of an empty choice.</param>
/// <param name="PreselectedOptionId">Selected when the window opens, or null to open with no selection. Must be
/// one of <paramref name="Options"/>.</param>
/// <param name="PreferredOnlyLabel">Label for the checkbox that hides every option whose
/// <see cref="AssignmentChoiceOption.IsPreferred"/> is false. Null for no such checkbox. The UI starts with it
/// checked, so a domain that sets this is saying the preferred options are the ones normally wanted.</param>
/// <param name="Auto">Non-null when the domain has already decided — the UI shows the message rather than the
/// table, and still records the option with <see cref="AssignmentChoiceAnswers.Builder.Answer"/>.</param>
public sealed record AssignmentChoice(
    string ChoiceId,
    BodyId BodyId,
    string GroupKey,
    string Title,
    string Prompt,
    IReadOnlyList<AssignmentChoiceColumn> Columns,
    IReadOnlyList<AssignmentChoiceOption> Options,
    string? PreselectedOptionId = null,
    string? PreferredOnlyLabel = null,
    AssignmentChoiceAutoSelection? Auto = null)
{
    /// <summary>The option <paramref name="optionId"/> names, or null when the choice has no such option.</summary>
    public AssignmentChoiceOption? Find(string? optionId) =>
        optionId is null ? null : Options.FirstOrDefault(o => string.Equals(o.OptionId, optionId, StringComparison.Ordinal));
}

/// <summary>One question to put to the user, and every body the answer will apply to.</summary>
public sealed record PendingAssignmentChoice(AssignmentChoice Choice, IReadOnlyList<BodyId> BodyIds);
