namespace BANxOpen.Foundation.Contracts.Common;

public readonly record struct MaterialId(string Value) : IStronglyTypedId<string>
{
    public override string ToString() => Value;
}

public readonly record struct MaterialLibraryId(string Value) : IStronglyTypedId<string>
{
    public override string ToString() => Value;
}

/// <summary>Identifies one body within a part. The value is the live NX Body's journal identifier —
/// the one handle that is stable across a save/reopen and that Core-layer code can hold without
/// referencing NXOpen. Shared by every tool that reads or writes body state, so a body means the same
/// thing to the material assignment engine and to a feature domain describing what sits on it.</summary>
public readonly record struct BodyId(string Value) : IStronglyTypedId<string>
{
    public override string ToString() => Value;
}
