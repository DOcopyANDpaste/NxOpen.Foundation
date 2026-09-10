using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Contracts.Bodies;

public enum BodyKind
{
    Solid,

    /// <summary>A zero-thickness surface body (NX <c>Body.IsSheetBody</c>). Not sheet metal.</summary>
    Sheet,

    /// <summary>A sheet metal body. In NX these are solid bodies with a thickness, so <c>IsSheetBody</c> is
    /// false for them; they are recognised through the sheet metal manager instead. Kept distinct from
    /// <see cref="Solid"/> because sheet metal material libraries apply to these bodies only, and distinct
    /// from <see cref="Sheet"/> because a surface body is not sheet metal.</summary>
    SheetMetal,

    Unknown,
}

/// <summary>One body in a part, as read off the live model into a form Core-layer code can hold.
/// <see cref="Attributes"/> carries the body's NX user attributes verbatim (title to string value),
/// which is how feature domains recognise their own stamps without the reader knowing about them.</summary>
public sealed record BodyInfo(
    BodyId Id,
    string Name,
    BodyKind Kind,
    double Volume,
    IReadOnlyDictionary<string, string> Attributes);