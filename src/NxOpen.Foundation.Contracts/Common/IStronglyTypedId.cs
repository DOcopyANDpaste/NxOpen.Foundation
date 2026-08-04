namespace NxOpen.Foundation.Contracts.Common;

/// <summary>Marker for a value-typed identifier wrapping a single underlying value (e.g. a
/// <c>readonly record struct</c> like <c>BodyId(string Value)</c>). Lets shared code constrain on
/// <c>where TId : IStronglyTypedId&lt;string&gt;</c> without forcing identifier types to give up
/// struct/value semantics by inheriting a base record.</summary>
public interface IStronglyTypedId<out TValue>
{
    TValue Value { get; }
}
