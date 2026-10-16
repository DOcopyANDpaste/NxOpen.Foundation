namespace BANxOpen.Foundation.Core;

/// <summary>The stable codes Foundation puts on a failed <c>OperationResult</c> (<c>ErrorCode</c>). Codes, not
/// wording, are the contract: tools branch on these and show the message as-is. The wording is
/// <see cref="FoundationText"/>'s.</summary>
public static class FoundationCodes
{
    public const string RequirementWrongDomain = "REQUIREMENT_WRONG_DOMAIN";

    /// <summary>Also used by BANxOpen.SheetMetal when a part's requirements cannot be read.</summary>
    public const string RequirementsUnreadable = "REQUIREMENTS_UNREADABLE";

    public const string RequirementNameConflict = "REQUIREMENT_NAME_CONFLICT";

    /// <summary>Also used by BANxOpen.SheetMetal when the requirements could not be written.</summary>
    public const string RequirementWriteFailed = "REQUIREMENT_WRITE_FAILED";
}
