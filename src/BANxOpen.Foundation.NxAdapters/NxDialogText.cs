using NXOpen;

namespace BANxOpen.Foundation.NxAdapters;

/// <summary>Wording every NX dialog built on this foundation shows the same way, with no domain meaning: message box
/// titles, start-up and unexpected-error messages, and how an NX error is put in front of a user. Default text a tool
/// may override, like the library describers (<c>FoundationText</c>, <c>MaterialsText</c>, <c>SheetMetalText</c>);
/// a tool's own wording is in its <c>UiText</c>. See the nxopen-csharp-patterns skill, common.md §8.</summary>
public class NxDialogText
{
    public static NxDialogText Default { get; } = new();

    // ---- Message box titles ----

    public virtual string ErrorTitle => "Error";
    public virtual string ConfirmTitle => "Confirm";
    public virtual string SuccessTitle => "Success";

    // ---- Messages ----

    /// <param name="tool">The tool's name, as the user knows it.</param>
    /// <param name="detail">Why, as the session check or the lower layer said it.</param>
    public virtual string CouldNotStart(string tool, string? detail) => WithDetails($"{tool} could not start.", detail);

    /// <summary>A dialog callback threw. The full exception is written to the Listing Window by the caller.</summary>
    public virtual string HandlerFailed(string what, string detail) =>
        $"An error occurred handling '{what}':{Environment.NewLine}{detail}{Environment.NewLine}{Environment.NewLine}" +
        "Full details were written to the Listing Window.";

    /// <summary>A failed operation that came with no message of its own.</summary>
    public virtual string NxRefusedTheChange(string? detail) => WithDetails("NX refused the change.", detail);

    /// <summary>An NX error as shown to a user, under a worded sentence.</summary>
    public virtual string NxError(NXException ex) => $"NX {ex.ErrorCode}: {ex.Message}";

    protected virtual string WithDetails(string text, string? detail) =>
        string.IsNullOrWhiteSpace(detail) ? text : $"{text}{Environment.NewLine}{Environment.NewLine}Details: {detail}";
}
