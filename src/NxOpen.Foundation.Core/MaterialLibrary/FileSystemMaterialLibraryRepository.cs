using NxOpen.Foundation.Contracts.Common;
using NxOpen.Foundation.Contracts.Materials;

namespace NxOpen.Foundation.Core.MaterialLibrary;

/// <summary>Pure file-system seam for material library discovery — no NXOpen types touched. Scans a
/// root directory for *.xml files; one <see cref="MaterialLibraryReference"/> per file, keyed by
/// filename (without extension).
///
/// Takes a plain <c>Action&lt;string&gt;</c> for warnings instead of a concrete NX logger type: this
/// class lives in Foundation.Core (net48;net8.0, no NXOpen), and any NX-specific logger (e.g.
/// NxListingLog) lives in the NXOpen-touching NxAdapters tier — Core must never depend on that tier.
/// Callers in an adapter layer typically pass a method group like <c>log.Warn</c>.</summary>
public sealed class FileSystemMaterialLibraryRepository : IMaterialLibraryRepository
{
    private readonly string _rootDirectory;
    private readonly Action<string>? _onWarning;
    private Dictionary<MaterialLibraryId, string>? _pathsById;

    public FileSystemMaterialLibraryRepository(string? rootDirectoryOverride = null, Action<string>? onWarning = null)
    {
        _rootDirectory = rootDirectoryOverride ?? ResolveDefaultRootDirectory();
        _onWarning = onWarning;
    }

    public IReadOnlyList<MaterialLibraryReference> ListAvailableLibraries()
    {
        var references = new List<MaterialLibraryReference>();
        var paths = new Dictionary<MaterialLibraryId, string>();

        if (!Directory.Exists(_rootDirectory))
        {
            _onWarning?.Invoke($"Material library directory not found: {_rootDirectory}");
            _pathsById = paths;
            return references;
        }

        foreach (var filePath in Directory.EnumerateFiles(_rootDirectory, "*.xml", SearchOption.TopDirectoryOnly))
        {
            var displayName = Path.GetFileNameWithoutExtension(filePath);
            var id = new MaterialLibraryId(displayName);
            paths[id] = filePath;
            references.Add(new MaterialLibraryReference(id, displayName, filePath));
        }

        _pathsById = paths;
        return references;
    }

    public string ReadLibraryXml(MaterialLibraryId id)
    {
        if (_pathsById is null)
            ListAvailableLibraries();

        if (_pathsById is null || !_pathsById.TryGetValue(id, out var filePath))
            throw new FileNotFoundException($"No material library found for id '{id}' under '{_rootDirectory}'.");

        return File.ReadAllText(filePath);
    }

    // VERIFY: exact conventional folder for shipped/customer material libraries — best-effort guesses
    // below, no NX install available to confirm. UGII_CUSTOMER_DIR is checked first since a
    // customer-authored materials folder should take precedence if both exist.
    private static string ResolveDefaultRootDirectory()
    {
        var customerDir = Environment.GetEnvironmentVariable("UGII_CUSTOMER_DIR");
        if (!string.IsNullOrEmpty(customerDir))
        {
            var candidate = Path.Combine(customerDir, "MATERIALS");
            if (Directory.Exists(candidate))
                return candidate;
        }

        var baseDir = Environment.GetEnvironmentVariable("UGII_BASE_DIR");
        if (string.IsNullOrEmpty(baseDir))
            throw new InvalidOperationException("UGII_BASE_DIR is not set — cannot resolve the material library directory.");

        return Path.Combine(baseDir, "MATERIALS");
    }
}
