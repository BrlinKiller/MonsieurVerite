using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MonsieurVerite;

/// <summary>
/// Every key in Locales/en-US.json is a generated member. English fills any gap in the translation.
/// </summary>
public static partial class Strings
{
    private const string Prefix = "Locales/";

    private static Dictionary<string, string>? translation;

    internal static IReadOnlyDictionary<string, string> English { get; } = Load("en-US");

    internal static IReadOnlyList<string> Languages { get; } =
        typeof(Strings).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal))
            .Select(name => Path.GetFileNameWithoutExtension(name[Prefix.Length..]))
            .ToList();

    /// <summary>The culture the UI asked for, passed to the engine as CHARLOTTE_LANG.</summary>
    public static string Language { get; private set; } = "en-US";

    /// <summary>Switches to the closest language there is a file for, else English.</summary>
    public static void Use(CultureInfo culture, string appDirectory)
    {
        Language = culture.Name;
        translation = Translations.Resolve(culture, appDirectory);
    }

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

    internal static HashSet<string> Placeholders(string text) =>
        Placeholder().Matches(text).Select(match => match.Value).ToHashSet();

    private static IEnumerable<string> Lineage(CultureInfo culture)
    {
        for (var current = culture; current.Name.Length > 0; current = current.Parent)
        {
            yield return current.Name;
        }
    }

    private static string Text(string key) =>
        translation?.GetValueOrDefault(key) is { Length: > 0 } text ? text : English[key];

    // Placeholders are replaced in one pass, which leaves a value such as a file name alone even
    // when it contains "{name}".
    private static string Fill(string key, params (string Name, object? Value)[] values) =>
        Placeholder().Replace(Text(key), match =>
        {
            var found = values.FirstOrDefault(value => value.Name == match.Groups[1].Value);
            return found.Name is null
                ? match.Value
                : Convert.ToString(found.Value, CultureInfo.CurrentCulture) ?? "";
        });
}
