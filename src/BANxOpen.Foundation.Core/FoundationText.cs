namespace BANxOpen.Foundation.Core;

/// <summary>The wording of every message the Foundation libraries can put in front of a user, as default text a tool
/// may override (the ASP.NET Identity <c>IdentityErrorDescriber</c> pattern): one virtual member per message, and a
/// <see cref="Default"/> instance. Code that produces one of these messages takes a <c>FoundationText?</c> and falls
/// back to <see cref="Default"/>. Log lines are not user text and are not here. See the nxopen-csharp-patterns skill,
/// common.md §8.
///
/// Parameters that carry an NX error are already formatted (<c>NxDialogText.NxError</c>): Core has no NXOpen.</summary>
public class FoundationText
{
    public static FoundationText Default { get; } = new();

    // ---- Session ----

    public virtual string NoActiveSession => "No active NX session.";

    public virtual string NoWorkPart => "No active work part. Open a part before launching this tool.";

    // ---- Requirements ----

    public virtual string RequirementsWrongDomain(IEnumerable<string> names, string domain) =>
        $"Requirements {string.Join(", ", names)} do not belong to domain '{domain}'.";

    public virtual string RequirementsUnreadable(IEnumerable<string> errors) => string.Join(" ", errors);

    public virtual string RequirementNameConflict(IEnumerable<string> errors) => string.Join(" ", errors);

    public virtual string RequirementWriteFailed(string nxDetail) => nxDetail;

    public virtual string RequirementsNotListed(string nxDetail) => $"The part's requirements could not be listed: {nxDetail}";

    public virtual string RequirementUnreadable(string name, string nxDetail) => $"Requirement '{name}' could not be read: {nxDetail}";

    public virtual string RequirementValueUnreadable(string name, string detail) =>
        $"Requirement '{name}' holds a value that could not be read: {detail}";

    public virtual string RequirementNameClaimed(string name, IEnumerable<string> identities) =>
        $"Requirement name '{name}' is claimed by {string.Join(", ", identities)}.";

    // ---- Shared ----

    /// <summary>A worded sentence with the technical detail that caused it below, or the sentence alone.</summary>
    protected virtual string WithDetails(string text, string? detail) =>
        string.IsNullOrWhiteSpace(detail) ? text : $"{text}{Environment.NewLine}{Environment.NewLine}Details: {detail}";
}
