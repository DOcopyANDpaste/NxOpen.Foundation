using NXOpen;

namespace BANxOpen.Foundation.NxAdapters;

/// <summary>Where <see cref="NxListingLog"/> writes Info/Warn/Error lines.</summary>
public enum NxLogTarget
{
    /// <summary>The NX session log (syslog, Help &gt; Log File). The default.</summary>
    NxLog,

    /// <summary>The NX Listing Window.</summary>
    ListingWindow,

    /// <summary>Both.</summary>
    Both,
}

/// <summary>The tools' log. Console.WriteLine is invisible inside NX, so lines go to the NX log, the Listing Window,
/// or both — per <see cref="Target"/> — with Warn added alongside Info/Error since rule pipelines commonly have a
/// non-blocking Warn concept to surface (Skills/without-block-ui.md §5).
///
/// <see cref="Debug"/> lines are for development: they always go to the NX log, and to the Listing Window too only when
/// <see cref="IsDebug"/> is on.
///
/// Both settings come from the environment (<see cref="FromEnvironment"/>), so a user or site sets them in
/// ugii_env or Windows without touching any tool's config:
/// <list type="bullet">
/// <item><c>BANXOPEN_LOG_TARGET</c> = <c>NxLog</c> (default) | <c>ListingWindow</c> | <c>Both</c></item>
/// <item><c>BANXOPEN_DEBUG</c> = <c>1</c> | <c>true</c> | <c>on</c> to show debug lines in the Listing Window (default off)</item>
/// </list></summary>
public sealed class NxListingLog
{
    public const string TargetVariable = "BANXOPEN_LOG_TARGET";
    public const string DebugVariable = "BANXOPEN_DEBUG";

    private const string Prefix = "[BANxOpen]";

    private readonly ListingWindow _lw;
    private readonly LogFile _nxLog;

    public NxListingLog(Session session, NxLogTarget target = NxLogTarget.NxLog, bool isDebug = false)
    {
        _lw = session.ListingWindow;
        _nxLog = session.LogFile;
        Target = target;
        IsDebug = isDebug;
    }

    /// <summary>A log configured from <see cref="TargetVariable"/> and <see cref="DebugVariable"/>. A missing or
    /// unrecognised value falls back to the default, and the resolved settings are written as the first debug line.</summary>
    public static NxListingLog FromEnvironment(Session session)
    {
        var targetText = Environment.GetEnvironmentVariable(TargetVariable);
        var target = Enum.TryParse<NxLogTarget>(targetText?.Trim(), ignoreCase: true, out var parsed)
                     && Enum.IsDefined(typeof(NxLogTarget), parsed)
            ? parsed
            : NxLogTarget.NxLog;

        var debugText = Environment.GetEnvironmentVariable(DebugVariable)?.Trim();
        var isDebug = debugText is not null
                      && (debugText == "1"
                          || debugText.Equals("true", StringComparison.OrdinalIgnoreCase)
                          || debugText.Equals("on", StringComparison.OrdinalIgnoreCase));

        var log = new NxListingLog(session, target, isDebug);
        log.Debug($"Log: target {target} ({TargetVariable}='{targetText ?? ""}'), debug {(isDebug ? "on" : "off")} " +
                  $"({DebugVariable}='{debugText ?? ""}').");
        return log;
    }

    public NxLogTarget Target { get; }

    public bool IsDebug { get; }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message) => Write("ERROR", message);

    /// <summary>Development detail: always in the NX log, in the Listing Window only with <see cref="IsDebug"/>.</summary>
    public void Debug(string message)
    {
        WriteNxLog("DEBUG", message);
        if (IsDebug)
            WriteListingWindow("DEBUG", message);
    }

    private void Write(string level, string message)
    {
        if (Target is NxLogTarget.NxLog or NxLogTarget.Both)
            WriteNxLog(level, message);
        if (Target is NxLogTarget.ListingWindow or NxLogTarget.Both)
            WriteListingWindow(level, message);
    }

    // The syslog is line-oriented: a multi-line message (the dialog's status block) is written line by line, each
    // prefixed, so a grep for the prefix finds all of it.
    private void WriteNxLog(string level, string message)
    {
        foreach (var line in message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            _nxLog.WriteLine($"{Prefix}[{level}] {line}");
    }

    private void WriteListingWindow(string level, string message)
    {
        if (!_lw.IsOpen)
            _lw.Open();
        _lw.WriteLine($"[{level}] {message}");
    }
}
