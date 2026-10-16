namespace BANxOpen.Foundation.Core.RuleEngine;

public enum RuleDecision
{
    Allow,
    Block,
    RequireConfirmation,

    /// <summary>Allowed to proceed — behaves like <see cref="Allow"/> for planning/short-circuit purposes
    /// — but the message is meant to be surfaced to the user as a non-blocking, advisory warning.</summary>
    Warn,
}

/// <param name="ReasonCode">The stable code tools branch on — the contract, not the wording.</param>
/// <param name="Message">The canonical wording, built from the producing library's describer (<c>MaterialsText</c>,
/// <c>SheetMetalText</c>, …). Every tool shows it as-is; to change it, change (or override) the describer.</param>
public sealed record RuleOutcome(
    string RuleId,
    RuleDecision Decision,
    string? ReasonCode,
    string? Message)
{
    private static readonly IReadOnlyDictionary<string, string> NoArgs = new Dictionary<string, string>();

    /// <summary>The values <see cref="Message"/> was built from, already formatted for display (names, thicknesses,
    /// colours), keyed by name — for a tool that shows them in its own layout, and for diagnostics. The keys each
    /// reason code carries are documented where the code is produced.</summary>
    public IReadOnlyDictionary<string, string> Args { get; init; } = NoArgs;

    /// <summary>The separate outcomes this one stands for, when a rule folds several into one (several advisory
    /// warnings on one body, say). Each carries its own code and <see cref="Args"/>. Empty for a single outcome.</summary>
    public IReadOnlyList<RuleOutcome> Inner { get; init; } = Array.Empty<RuleOutcome>();
}
