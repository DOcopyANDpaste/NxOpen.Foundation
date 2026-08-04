using NxOpen.Foundation.Contracts.Common;
using NxOpen.Foundation.Core.MaterialLibrary;

namespace NxOpen.Foundation.Core.Tests.MaterialLibrary;

public class FileSystemMaterialLibraryRepositoryTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "NxOpenFoundationTests_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void ListAvailableLibraries_ReturnsOneReferencePerXmlFile_KeyedByFileNameWithoutExtension()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "metric.xml"), "<MatML_Doc></MatML_Doc>");
        File.WriteAllText(Path.Combine(_tempDir, "english.xml"), "<MatML_Doc></MatML_Doc>");
        File.WriteAllText(Path.Combine(_tempDir, "notes.txt"), "ignored");

        var repository = new FileSystemMaterialLibraryRepository(_tempDir);
        var libraries = repository.ListAvailableLibraries();

        Assert.Equal(2, libraries.Count);
        Assert.Contains(libraries, l => l.Id == new MaterialLibraryId("metric"));
        Assert.Contains(libraries, l => l.Id == new MaterialLibraryId("english"));
    }

    [Fact]
    public void ReadLibraryXml_ReturnsFileContent()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "metric.xml"), "<MatML_Doc><Material/></MatML_Doc>");

        var repository = new FileSystemMaterialLibraryRepository(_tempDir);

        Assert.Equal("<MatML_Doc><Material/></MatML_Doc>", repository.ReadLibraryXml(new MaterialLibraryId("metric")));
    }

    [Fact]
    public void ListAvailableLibraries_MissingDirectory_InvokesOnWarningAndReturnsEmpty()
    {
        var missingDir = Path.Combine(_tempDir, "does-not-exist");
        string? warning = null;

        var repository = new FileSystemMaterialLibraryRepository(missingDir, onWarning: msg => warning = msg);
        var libraries = repository.ListAvailableLibraries();

        Assert.Empty(libraries);
        Assert.NotNull(warning);
    }
}
