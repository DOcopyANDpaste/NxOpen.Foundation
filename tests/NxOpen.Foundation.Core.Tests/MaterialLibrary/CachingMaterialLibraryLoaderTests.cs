using NxOpen.Foundation.Contracts.Common;
using NxOpen.Foundation.Contracts.Materials;
using NxOpen.Foundation.Core.MaterialLibrary;

namespace NxOpen.Foundation.Core.Tests.MaterialLibrary;

public class CachingMaterialLibraryLoaderTests
{
    private const string EmptyLibraryXml = "<MatML_Doc></MatML_Doc>";

    private sealed class FakeRepository : IMaterialLibraryRepository
    {
        private readonly Dictionary<MaterialLibraryId, string> _xmlById;

        public FakeRepository(Dictionary<MaterialLibraryId, string> xmlById) => _xmlById = xmlById;

        public int ReadCallCount { get; private set; }

        public IReadOnlyList<MaterialLibraryReference> ListAvailableLibraries() => Array.Empty<MaterialLibraryReference>();

        public string ReadLibraryXml(MaterialLibraryId id)
        {
            ReadCallCount++;
            return _xmlById[id];
        }
    }

    private sealed class CountingParser : IMaterialLibraryParser
    {
        private readonly IMaterialLibraryParser _inner = new MaterialLibraryParser();

        public int ParseCallCount { get; private set; }

        public NxOpen.Foundation.Contracts.Materials.MaterialLibrary Parse(MaterialLibraryId id, string displayName, string xmlContent)
        {
            ParseCallCount++;
            return _inner.Parse(id, displayName, xmlContent);
        }
    }

    [Fact]
    public void GetOrLoad_ParsesOnFirstRequest()
    {
        var libraryId = new MaterialLibraryId("lib1");
        var repository = new FakeRepository(new() { [libraryId] = EmptyLibraryXml });
        var parser = new CountingParser();
        var loader = new CachingMaterialLibraryLoader(repository, parser);

        var library = loader.GetOrLoad(new MaterialLibraryReference(libraryId, "Lib 1", "lib1.xml"));

        Assert.Equal(libraryId, library.Id);
        Assert.Equal(1, repository.ReadCallCount);
        Assert.Equal(1, parser.ParseCallCount);
    }

    [Fact]
    public void GetOrLoad_SecondRequestForSameLibrary_ReturnsCachedInstanceWithoutReparsing()
    {
        var libraryId = new MaterialLibraryId("lib1");
        var repository = new FakeRepository(new() { [libraryId] = EmptyLibraryXml });
        var parser = new CountingParser();
        var loader = new CachingMaterialLibraryLoader(repository, parser);
        var reference = new MaterialLibraryReference(libraryId, "Lib 1", "lib1.xml");

        var first = loader.GetOrLoad(reference);
        var second = loader.GetOrLoad(reference);

        Assert.Same(first, second);
        Assert.Equal(1, repository.ReadCallCount);
        Assert.Equal(1, parser.ParseCallCount);
    }

    [Fact]
    public void GetOrLoad_DifferentLibraries_AreCachedIndependently()
    {
        var libAId = new MaterialLibraryId("libA");
        var libBId = new MaterialLibraryId("libB");
        var repository = new FakeRepository(new()
        {
            [libAId] = EmptyLibraryXml,
            [libBId] = EmptyLibraryXml,
        });
        var parser = new CountingParser();
        var loader = new CachingMaterialLibraryLoader(repository, parser);

        loader.GetOrLoad(new MaterialLibraryReference(libAId, "Lib A", "a.xml"));
        loader.GetOrLoad(new MaterialLibraryReference(libBId, "Lib B", "b.xml"));
        loader.GetOrLoad(new MaterialLibraryReference(libAId, "Lib A", "a.xml"));

        Assert.Equal(2, repository.ReadCallCount);
        Assert.Equal(2, parser.ParseCallCount);
    }
}
