using System.Text.Json;
using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Rules.SheetMetal;

/// <summary>Which material libraries hold sheet metal materials — the libraries
/// <see cref="BlockRestrictedBodyTypeRule"/> reserves for sheet metal bodies.
///
/// Configured in <c>material-library-rules.json</c>, which sits beside the library XML files so every tool
/// reading those libraries reads the same rules:
/// <code>
/// { "sheetMetalLibraries": [ "Sheet Metal Materials" ] }
/// </code>
/// Names are library ids — the XML file name without its extension — compared case-insensitively and exactly,
/// so "Sheet Metal Materials Archive" is not included just because it contains the name.
///
/// When no file exists the original rule applies: a library whose name contains "sheetmetal", ignoring spaces
/// and case. That keeps existing installations behaving as before until someone opts in.</summary>
public sealed class SheetMetalLibraries
{
    public const string FileName = "material-library-rules.json";

    /// <summary>Points at a rules file elsewhere — useful when the library folder is under Program Files and
    /// not writable.</summary>
    public const string PathEnvironmentVariable = "BANXOPEN_MATERIAL_LIBRARY_RULES";

    private readonly HashSet<string>? _names;

    private SheetMetalLibraries(IEnumerable<string>? names) =>
        _names = names is null ? null : new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

    /// <summary>The name-based rule used when nothing is configured.</summary>
    public static SheetMetalLibraries NameHeuristic { get; } = new(null);

    public static SheetMetalLibraries FromNames(IEnumerable<string> names)
    {
        var list = names.Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
        if (list.Count == 0)
            throw new ArgumentException(
                "sheetMetalLibraries must list at least one library; delete the file to use the name-based default instead.",
                nameof(names));

        return new SheetMetalLibraries(list);
    }

    /// <summary>True when the list came from configuration rather than the name heuristic.</summary>
    public bool IsConfigured => _names is not null;

    public IReadOnlyCollection<string> ConfiguredNames => (IReadOnlyCollection<string>?)_names ?? Array.Empty<string>();

    public bool IsSheetMetalLibrary(MaterialLibraryId libraryId) =>
        _names is not null
            ? _names.Contains(libraryId.Value)
            : libraryId.Value.Replace(" ", "").IndexOf("sheetmetal", StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>Where the rules file is read from: <see cref="PathEnvironmentVariable"/> if set, otherwise
    /// <see cref="FileName"/> inside <paramref name="libraryRootDirectory"/>.</summary>
    public static string ResolvePath(string libraryRootDirectory)
    {
        var overridePath = Environment.GetEnvironmentVariable(PathEnvironmentVariable);
        return string.IsNullOrWhiteSpace(overridePath)
            ? Path.Combine(libraryRootDirectory, FileName)
            : overridePath!;
    }

    /// <summary>Loads the rules file. Missing file → <see cref="NameHeuristic"/>. Present but invalid → error,
    /// because silently falling back would ignore a restriction someone configured on purpose.</summary>
    public static SheetMetalLibraries Load(string path)
    {
        if (!File.Exists(path))
            return NameHeuristic;

        RulesFile? file;
        try
        {
            using var stream = File.OpenRead(path);
            file = JsonSerializer.Deserialize<RulesFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Material library rules at '{path}' are not valid JSON: {ex.Message}", ex);
        }

        if (file?.SheetMetalLibraries is null)
            throw new InvalidDataException(
                $"Material library rules at '{path}' have no \"sheetMetalLibraries\" list. " +
                "Add one, or delete the file to use the name-based default.");

        try
        {
            return FromNames(file.SheetMetalLibraries);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException($"Material library rules at '{path}': {ex.Message}", ex);
        }
    }

    private sealed class RulesFile
    {
        public List<string>? SheetMetalLibraries { get; set; }
    }
}
