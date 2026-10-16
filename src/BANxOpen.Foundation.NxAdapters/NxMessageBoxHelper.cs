using NXOpen;
using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.NxAdapters;

/// <summary>Generic NXMessageBox wrappers — Confirm/ShowResult/ShowError — extracted from a
/// tool-specific BlockAccessor's "Generic dialogs" region, since these have no dependency on any
/// particular dialog's blocks or domain types. Titles and the fallback wording are <see cref="NxDialogText"/>'s;
/// a failed result's message is the library's own wording and is shown as-is.</summary>
public static class NxMessageBoxHelper
{
    private static NxDialogText Text => NxDialogText.Default;

    // DialogType has exactly four members in NX 2412 — Error, Warning, Information, Question — so there is
    // no QuestionYesNo (the previous guess). Question is the yes/no dialog, and Show returns an int.
    //
    // VERIFY: that the int is 1 for Yes. The return type is confirmed; the meaning of the value is not.
    //
    // Keep this signature as it is: it is passed around as a Func<string, bool>.
    public static bool Confirm(string message) =>
        UI.GetUI().NXMessageBox.Show(Text.ConfirmTitle, NXMessageBox.DialogType.Question, message) == 1;

    public static void ShowResult(OperationResult result, string successMessage)
    {
        if (result.Ok)
        {
            UI.GetUI().NXMessageBox.Show(Text.SuccessTitle, NXMessageBox.DialogType.Information, successMessage);
            return;
        }

        ShowError(string.IsNullOrWhiteSpace(result.Message) ? Text.NxRefusedTheChange(null) : result.Message!);
    }

    public static void ShowError(string message) =>
        UI.GetUI().NXMessageBox.Show(Text.ErrorTitle, NXMessageBox.DialogType.Error, message);

    /// <summary>Tells the user something that is neither a failure nor a question — chiefly a choice a rule
    /// made on their behalf, which they would otherwise have no way of knowing about.</summary>
    public static void ShowInfo(string title, string message) =>
        UI.GetUI().NXMessageBox.Show(title, NXMessageBox.DialogType.Information, message);
}
