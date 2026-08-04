using NXOpen;
using NXOpen.Foundation.Contracts.Common;

namespace NxOpen.Foundation.NxAdapters;

/// <summary>Generic NXMessageBox wrappers — Confirm/ShowResult/ShowError — extracted from a
/// tool-specific BlockAccessor's "Generic dialogs" region, since these have no dependency on any
/// particular dialog's blocks or domain types.</summary>
public static class NxMessageBoxHelper
{
    // VERIFY: exact NXMessageBox.Show return type/values for a Yes/No dialog — "== 1 means Yes" is a
    // best-effort guess, not confirmed against the installed NX version.
    public static bool Confirm(string message) =>
        UI.GetUI().NXMessageBox.Show("Confirm", NXMessageBox.DialogType.QuestionYesNo, message) == 1;

    public static void ShowResult(OperationResult result, string successMessage)
    {
        var type = result.Ok ? NXMessageBox.DialogType.Information : NXMessageBox.DialogType.Error;
        var message = result.Ok ? successMessage : $"{result.ErrorCode}: {result.Message}";
        UI.GetUI().NXMessageBox.Show(result.Ok ? "Success" : "Error", type, message);
    }

    public static void ShowError(string message) =>
        UI.GetUI().NXMessageBox.Show("Error", NXMessageBox.DialogType.Error, message);
}
