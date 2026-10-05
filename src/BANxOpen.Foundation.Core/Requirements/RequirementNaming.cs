using System.Text;
using BANxOpen.Foundation.Contracts.Requirements;

namespace BANxOpen.Foundation.Core.Requirements;

/// <summary>How a <see cref="RequirementSpec"/> is named in NX, and the bookkeeping line that records its identity.
///
/// The NX name is for people (it shows in the Part Navigator and the Expressions dialog), so it is readable but lossy:
/// non-alphanumerics become '_'. The exact identity therefore travels in a bookkeeping line appended to the
/// requirement's description, and that line — not the name — is what decides ownership on read. A name can collide
/// only for two keys differing purely in punctuation; <see cref="RequirementDiff"/> refuses such a set rather than
/// letting one overwrite the other.</summary>
public static class RequirementNaming
{
    /// <summary>Every requirement the BANxOpen tools write starts with this, so a reader skips everyone else's cheaply.</summary>
    public const string Prefix = "BA_REQ_";

    private const string MetadataMarker = "#BA_REQ v1";

    /// <summary><c>BA_REQ_&lt;DOMAIN&gt;_&lt;KEY&gt;_&lt;ASPECT&gt;</c>, upper-cased, each part reduced to letters, digits and
    /// '_'. A separator is kept for every punctuation character, so <c>B1005010-2</c> and <c>B10050102</c> stay
    /// distinct.</summary>
    public static string Name(string domain, string key, string aspect) =>
        $"{Prefix}{Token(domain)}_{Token(key)}_{Token(aspect)}";

    public static string Name(RequirementSpec spec) => Name(spec.Domain, spec.Key, spec.Aspect);

    /// <summary>The name of the Requirement Check that links a requirement to its checked expression.</summary>
    public static string CheckName(string requirementName) => $"{requirementName}_CHECK";

    /// <summary>The line appended to a requirement's description to carry its exact identity. Each value is
    /// URI-escaped, so any key round-trips.</summary>
    public static string MetadataLine(string domain, string key, string aspect) =>
        $"{MetadataMarker} domain={Uri.EscapeDataString(domain)} key={Uri.EscapeDataString(key)} aspect={Uri.EscapeDataString(aspect)}";

    /// <summary>The description as written to NX: the domain's own lines, then the bookkeeping line.</summary>
    public static IReadOnlyList<string> DescriptionWithMetadata(RequirementSpec spec) =>
        spec.Description.Concat(new[] { MetadataLine(spec.Domain, spec.Key, spec.Aspect) }).ToList();

    /// <summary>Splits a description read from NX into the domain's own lines and the identity the bookkeeping line
    /// carries. Null identity when there is no well-formed line — a requirement someone else wrote, or one edited by
    /// hand.</summary>
    public static (IReadOnlyList<string> Description, (string Domain, string Key, string Aspect)? Identity) SplitDescription(
        IReadOnlyList<string> lines)
    {
        var own = new List<string>();
        (string, string, string)? identity = null;

        foreach (var line in lines)
        {
            if (identity is null && TryParseMetadata(line, out var parsed))
                identity = parsed;
            else
                own.Add(line);
        }

        return (own, identity);
    }

    private static bool TryParseMetadata(string line, out (string Domain, string Key, string Aspect) identity)
    {
        identity = default;
        if (!line.StartsWith(MetadataMarker + " ", StringComparison.Ordinal))
            return false;

        var fields = line.Substring(MetadataMarker.Length + 1)
            .Split(' ')
            .Select(part => part.Split(new[] { '=' }, 2))
            .Where(pair => pair.Length == 2)
            .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.Ordinal);

        if (!fields.TryGetValue("domain", out var domain) || !fields.TryGetValue("key", out var key)
            || !fields.TryGetValue("aspect", out var aspect))
            return false;

        identity = (Uri.UnescapeDataString(domain), Uri.UnescapeDataString(key), Uri.UnescapeDataString(aspect));
        return true;
    }

    private static string Token(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
            builder.Append(char.IsLetterOrDigit(c) && c < 128 ? char.ToUpperInvariant(c) : '_');

        return builder.ToString();
    }
}
