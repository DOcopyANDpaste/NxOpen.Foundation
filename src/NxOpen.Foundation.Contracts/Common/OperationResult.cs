namespace NxOpen.Foundation.Contracts.Common;

public sealed record OperationResult(bool Ok, string? ErrorCode, string? Message)
{
    public static OperationResult Success() => new(true, null, null);
    public static OperationResult Fail(string code, string message) => new(false, code, message);
}
