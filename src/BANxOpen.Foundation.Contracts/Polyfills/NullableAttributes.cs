#if NETFRAMEWORK
// The nullable-flow attributes the compiler special-cases (NotNullWhen and friends) ship in .NET Core's
// BCL but not .NET Framework's, so a net48 target that writes `[NotNullWhen(true)] out T? value` fails to
// compile without these.
//
// PUBLIC and living here for the same reason as IsExternalInit next door: the compiler only honours these
// across an assembly reference if they are visible there, so declaring them once in Contracts covers every
// net48 project that references it (directly, or transitively via Core) instead of each redeclaring its own.
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        public bool ReturnValue { get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
    public sealed class MaybeNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
    public sealed class NotNullAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class MaybeNullWhenAttribute : Attribute
    {
        public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        public bool ReturnValue { get; }
    }
}
#endif
