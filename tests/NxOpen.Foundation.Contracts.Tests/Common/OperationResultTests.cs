using NxOpen.Foundation.Contracts.Common;

namespace NxOpen.Foundation.Contracts.Tests.Common;

public class OperationResultTests
{
    [Fact]
    public void Success_IsOkWithNoErrorCodeOrMessage()
    {
        var result = OperationResult.Success();

        Assert.True(result.Ok);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.Message);
    }

    [Fact]
    public void Fail_IsNotOkAndCarriesCodeAndMessage()
    {
        var result = OperationResult.Fail("E001", "Something went wrong.");

        Assert.False(result.Ok);
        Assert.Equal("E001", result.ErrorCode);
        Assert.Equal("Something went wrong.", result.Message);
    }
}
