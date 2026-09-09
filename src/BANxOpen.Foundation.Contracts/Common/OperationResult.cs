namespace BANxOpen.Foundation.Contracts.Common;

public sealed record OperationResult(bool Ok, string? ErrorCode, string? Message)
{
    public static OperationResult Success() => new(true, null, null);
    public static OperationResult Fail(string code, string message) => new(false, code, message);
}

/// <summary>The same shape carrying a success payload. <see cref="Value"/> is meaningful only when
/// <see cref="Ok"/> is true; a failure leaves it default.</summary>
public sealed record OperationResult<T>(bool Ok, T? Value, string? ErrorCode, string? Message)
{
    public static OperationResult<T> Success(T value) => new(true, value, null, null);
    public static OperationResult<T> Fail(string code, string message) => new(false, default, code, message);
}
