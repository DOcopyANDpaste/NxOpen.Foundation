namespace NxOpen.Foundation.Core.RuleEngine;

public enum RuleDecision
{
    Allow,
    Block,
    RequireConfirmation,

    /// <summary>Allowed to proceed — behaves like <see cref="Allow"/> for planning/short-circuit purposes
    /// — but the message is meant to be surfaced to the user as a non-blocking, advisory warning.</summary>
    Warn,
}

public sealed record RuleOutcome(
    string RuleId,
    RuleDecision Decision,
    string? ReasonCode,
    string? Message);
