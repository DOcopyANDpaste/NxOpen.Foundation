using System.Globalization;

namespace NxOpen.Foundation.Contracts.Materials;

/// <summary>A single property read off a material entry (MatML PropertyData/PropertyDetails). MatML
/// properties come in different practical shapes (plain text like a grade name, a single number like
/// density, or a comma-separated list of values for e.g. temperature-dependent tables) and nothing in
/// the source XML reliably declares which — so this holds only the raw text plus metadata, and
/// interpretation happens on demand via <see cref="AsString"/>/<see cref="AsArray"/>/<see cref="AsNumber"/>
/// rather than being decided once at parse time.</summary>
public sealed record MaterialPropertyValue(
    string PropertyId,
    string Name,
    string? Symbol,
    string RawValue,
    string? Unit)
{
    public string AsString() => RawValue;

    /// <summary>Splits a comma-separated raw value into trimmed, non-empty entries.</summary>
    public IReadOnlyList<string> AsArray() =>
        [.. RawValue.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0)];

    /// <summary>Parses the raw value as a single number; null if it isn't one (e.g. text or a
    /// comma-separated list).</summary>
    public double? AsNumber() =>
        double.TryParse(RawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
}
