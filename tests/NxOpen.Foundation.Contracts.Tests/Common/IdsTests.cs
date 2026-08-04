using NxOpen.Foundation.Contracts.Common;

namespace NxOpen.Foundation.Contracts.Tests.Common;

public class IdsTests
{
    [Fact]
    public void MaterialId_ExposesValueThroughStronglyTypedIdInterface()
    {
        IStronglyTypedId<string> id = new MaterialId("steel-1");

        Assert.Equal("steel-1", id.Value);
    }

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

    [Fact]
    public void MaterialLibraryId_ExposesValueThroughStronglyTypedIdInterface()
    {
        IStronglyTypedId<string> id = new MaterialLibraryId("metric");

        Assert.Equal("metric", id.Value);
    }
}
