using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MonsieurVerite;

/// <summary>
/// The UI text in Lang/*.json. Each key is a member generated from en-US.json, and a string missing
/// from the chosen language falls back to English.
/// </summary>
public static partial class Strings
{
    private const string Prefix = "Lang/";

    private static Dictionary<string, string>? translation;

    internal static IReadOnlyDictionary<string, string> English { get; } = Load("en-US");

    internal static IReadOnlyList<string> Languages { get; } =
        typeof(Strings).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal))
            .Select(name => Path.GetFileNameWithoutExtension(name[Prefix.Length..]))
            .ToList();

    /// <summary>Switches to the closest language there is a file for, else English.</summary>
    public static void Use(CultureInfo culture) =>
        translation = Closest(culture, Languages) is { } language and not "en-US"
            ? Load(language)
            : null;

    /// <summary>A string with its placeholders left in, for text that puts links in them.</summary>
    public static string Template(string key) => Text(key);

    internal static string? Closest(CultureInfo culture, IEnumerable<string> languages)
    {
        var available = languages.ToList();
        foreach (var wanted in Lineage(culture))
        {
            var match = available.FirstOrDefault(language =>
                Lineage(CultureInfo.GetCultureInfo(language)).Contains(wanted));
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    internal static Dictionary<string, string> Load(string language)
    {
        var name = $"{Prefix}{language}.json";
        using var stream = typeof(Strings).Assembly.GetManifestResourceStream(name)
                           ?? throw new FileNotFoundException($"No {name} in the build.");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
    }

    [GeneratedRegex(@"\{([a-z][a-z0-9_]*)\}")]
    internal static partial Regex Placeholder();

    private static IEnumerable<string> Lineage(CultureInfo culture)
    {
        for (var current = culture; current.Name.Length > 0; current = current.Parent)
        {
            yield return current.Name;
        }
    }

    private static string Text(string key) =>
        translation?.GetValueOrDefault(key) is { Length: > 0 } text ? text : English[key];

    // One pass, so a value that itself contains "{name}", such as a file name, is left as it is.
    private static string Fill(string key, params (string Name, object? Value)[] values) =>
        Placeholder().Replace(Text(key), match =>
        {
            var found = values.FirstOrDefault(value => value.Name == match.Groups[1].Value);
            return found.Name is null
                ? match.Value
                : Convert.ToString(found.Value, CultureInfo.CurrentCulture) ?? "";
        });
}
