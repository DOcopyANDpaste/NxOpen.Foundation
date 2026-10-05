using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Contracts.Tests.Common;

public class IdsTests
{
    [Fact]
    public void MaterialId_ToString_ReturnsValue()
    {
        Assert.Equal("steel-1", new MaterialId("steel-1").ToString());
    }

    [Fact]
    public void MaterialId_EqualityIsByValue()
    {
        Assert.Equal(new MaterialId("a"), new MaterialId("a"));
        Assert.NotEqual(new MaterialId("a"), new MaterialId("b"));
    }
}
