using NXOpen;
using NxOpen.Foundation.Contracts.Common;

namespace NxOpen.Foundation.NxAdapters;

/// <summary>Generic NXMessageBox wrappers — Confirm/ShowResult/ShowError — extracted from a
/// tool-specific BlockAccessor's "Generic dialogs" region, since these have no dependency on any
/// particular dialog's blocks or domain types.</summary>
public static class NxMessageBoxHelper
{
    // DialogType has exactly four members in NX 2412 — Error, Warning, Information, Question — so there is
    // no QuestionYesNo (the previous guess). Question is the yes/no dialog, and Show returns an int.
    //
    // VERIFY: that the int is 1 for Yes. The return type is confirmed; the meaning of the value is not.
    public static bool Confirm(string message) =>
        UI.GetUI().NXMessageBox.Show("Confirm", NXMessageBox.DialogType.Question, message) == 1;

    public static void ShowResult(OperationResult result, string successMessage)
    {
        var type = result.Ok ? NXMessageBox.DialogType.Information : NXMessageBox.DialogType.Error;
        var message = result.Ok ? successMessage : $"{result.ErrorCode}: {result.Message}";
        UI.GetUI().NXMessageBox.Show(result.Ok ? "Success" : "Error", type, message);
    }

    public static void ShowError(string message) =>
        UI.GetUI().NXMessageBox.Show("Error", NXMessageBox.DialogType.Error, message);
}
