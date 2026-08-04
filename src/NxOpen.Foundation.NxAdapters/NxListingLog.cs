using NXOpen;

namespace NxOpen.Foundation.NxAdapters;

/// <summary>Writes to the NX Listing Window — Console.WriteLine is invisible inside NX. Straight from
/// Skills/without-block-ui.md §5, with Warn added alongside Info/Error since rule pipelines commonly
/// have a non-blocking Warn concept to surface.</summary>
public sealed class NxListingLog
{
    private readonly ListingWindow _lw;

    public NxListingLog(Session session) => _lw = session.ListingWindow;

    private void Write(string level, string message)
    {
        if (!_lw.IsOpen)
            _lw.Open();
        _lw.WriteLine($"[{level}] {message}");
    }

    public void Info(string message) => Write("INFO", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message) => Write("ERROR", message);
}
