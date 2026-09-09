using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Contracts.Bodies;

public enum BodyKind
{
    Solid,
    Sheet,
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