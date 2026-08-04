using NxOpen.Foundation.Contracts.Materials;

namespace NxOpen.Foundation.Core.Tests.MaterialLibrary;

public class MaterialPropertyValueTests
{
    private static MaterialPropertyValue Make(string rawValue) =>
        new("pr1", "Test Property", null, rawValue, null);

    [Fact]
    public void AsString_ReturnsRawValueUnchanged()
    {
        Assert.Equal("Matte", Make("Matte").AsString());
    }

    [Fact]
    public void AsNumber_ParsesASingleNumericRawValue()
    {
        Assert.Equal(7.872, Make("7.872").AsNumber());
    }

    [Theory]
    [InlineData("Matte")]
    [InlineData("10,20,30")]
    [InlineData("")]
    public void AsNumber_ReturnsNullForNonNumericRawValue(string rawValue)
    {
        Assert.Null(Make(rawValue).AsNumber());
    }

    [Fact]
    public void AsArray_SplitsCommaSeparatedRawValueAndTrimsEachEntry()
    {
        var result = Make("10, 20 ,30").AsArray();

        Assert.Equal(new[] { "10", "20", "30" }, result);
    }

    [Fact]
    public void AsArray_SingleValueWithNoCommas_ReturnsOneElement()
    {
        var result = Make("7.872").AsArray();

        Assert.Equal(new[] { "7.872" }, result);
    }

    [Fact]
    public void AsArray_DropsEmptyEntriesFromTrailingOrRepeatedCommas()
    {
        var result = Make("10,,20,").AsArray();

        Assert.Equal(new[] { "10", "20" }, result);
    }
}
