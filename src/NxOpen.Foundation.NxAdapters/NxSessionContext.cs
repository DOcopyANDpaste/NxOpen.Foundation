using System.Diagnostics.CodeAnalysis;
using NXOpen;
using NXOpen.UF;

namespace NxOpen.Foundation.NxAdapters;

public enum NxSessionMode
{
    Native,
    TeamcenterManaged,
}

/// <summary>A validated snapshot of the NX environment, resolved once at the boundary — nothing else in
/// the adapter layer should call Session.GetSession()/UFSession.GetUFSession() directly (extends
/// Skills/common.md §1's "resolve once, never scattered" rule to cover UFSession too). Foundational:
/// every NX Open tool built on this foundation depends on this, not just any one feature.
///
/// Hard-blocks (returns false) when there's no active session or no active work part — matches the
/// original spec, where work-part/body inspection happens right at dialog launch, so launch must stop
/// there if neither is available. Does NOT hard-block on <see cref="NxSessionMode"/> — whether managed
/// mode should ever be unsupported isn't decided per-tool; the mode is captured for callers/logging only.</summary>
public sealed class NxSessionContext
{
    public Session Session { get; }

    public UI Ui { get; }

    public Part WorkPart { get; }

    public UFSession UFSession { get; }

    public NxSessionMode Mode { get; }

    public NxListingLog Log { get; }

    private NxSessionContext(Session session, UI ui, Part workPart, UFSession ufSession, NxSessionMode mode, NxListingLog log)
    {
        Session = session;
        Ui = ui;
        WorkPart = workPart;
        UFSession = ufSession;
        Mode = mode;
        Log = log;
    }

    public static bool TryInitialize(
        [NotNullWhen(true)] out NxSessionContext? context,
        [NotNullWhen(false)] out string? failureReason)
    {
        context = null;
        failureReason = null;

        try
        {
            // In practice Session.GetSession() is expected to always return a valid session when this
            // code runs inside an NX process; kept explicit anyway since an active-session check was
            // asked for directly.
            var session = Session.GetSession();
            if (session is null)
            {
                failureReason = "No active NX session.";
                return false;
            }

            var workPart = session.Parts.Work;
            if (workPart is null)
            {
                failureReason = "No active work part. Open a part before launching this tool.";
                return false;
            }

            var ufSession = UFSession.GetUFSession();
            var mode = DetectSessionMode(ufSession);
            var log = new NxListingLog(session);

            context = new NxSessionContext(session, UI.GetUI(), workPart, ufSession, mode, log);
            return true;
        }
        catch (NXException ex)
        {
            failureReason = $"NX {ex.ErrorCode}: {ex.Message}";
            return false;
        }
    }

    // Verified against NX 2412: there is no such thing as Session.IsTeamcenterUsed (the previous guess).
    // UF_is_ugmanager_active — surfaced in .NET as UFSession.UF.IsUgmanagerActive — is the real query, and
    // "UG Manager active" is precisely what a Teamcenter-managed session means.
    private static NxSessionMode DetectSessionMode(UFSession ufSession)
    {
        ufSession.UF.IsUgmanagerActive(out var isManaged);
        return isManaged ? NxSessionMode.TeamcenterManaged : NxSessionMode.Native;
    }
}
