#if NETFRAMEWORK
// Records and init-only properties compile against this marker type, which .NET Framework's BCL
// doesn't ship. .NET 8 already provides it, so this is only compiled in for the net48 target.
//
// PUBLIC, deliberately (unlike the typical single-project "internal" version of this shim): the
// Roslyn compiler's special-cased lookup for this type only sees it across an assembly reference if
// it's visible there, i.e. public. Every net48 project that ProjectReferences
// NxOpen.Foundation.Contracts (directly, or transitively via Core) resolves records/init-only
// properties against THIS copy instead of needing its own — that's what actually eliminates the
// duplication this repo had before (see area E of the architecture plan).
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit
    {
    }
}
#endif
