namespace NxOpen.Foundation.Contracts.Common;

public readonly record struct MaterialId(string Value) : IStronglyTypedId<string>
{
    public override string ToString() => Value;
}

public readonly record struct MaterialLibraryId(string Value) : IStronglyTypedId<string>
{
    public override string ToString() => Value;
}
