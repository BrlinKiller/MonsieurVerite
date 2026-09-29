using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MonsieurVerite;

/// <summary>
/// Every key in Lang/en-US.json is a generated member. A downloaded translation overrides the
/// built-in one, and English fills any gap.
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

    /// <summary>The culture the UI asked for, passed to the engine as CHARLOTTE_LANG.</summary>
    public static string Language { get; private set; } = "en-US";

    /// <summary>Switches to the closest language there is a file for, else English.</summary>
    public static void Use(CultureInfo culture, string appDirectory)
    {
        ArgumentNullException.ThrowIfNull(culture);
        Language = culture.Name;
        translation = Resolve(culture, appDirectory);
    }

    internal static Dictionary<string, string>? Resolve(CultureInfo culture, string appDirectory)
    {
        var language = Closest(culture, Languages.Union(Downloaded(appDirectory)));
        if (language is null or "en-US")
        {
            return null;
        }

        var builtIn = Languages.Contains(language) ? Load(language) : null;
        return Merge(builtIn, ReadDownloaded(appDirectory, language));
    }

    internal static string DownloadedPath(string appDirectory, string language) =>
        Path.Combine(appDirectory, "lang", language, "gui.json");

    internal static bool IsCulture(string name)
    {
        try
        {
            CultureInfo.GetCultureInfo(name, predefinedOnly: true);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    internal static Dictionary<string, string> Merge(
        IReadOnlyDictionary<string, string>? builtIn,
        IReadOnlyDictionary<string, string>? downloaded)
    {
        var merged = builtIn is null ? [] : new Dictionary<string, string>(builtIn);
        if (downloaded is null)
        {
            return merged;
        }

        foreach (var (key, text) in downloaded)
        {
            if (FitsThisBuild(key, text))
            {
                merged[key] = text;
            }
        }

        return merged;
    }

    // Master can be ahead of this build, so its key may be gone or its placeholders renamed.
    private static bool FitsThisBuild(string key, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (!English.TryGetValue(key, out var english))
        {
            return false;
        }

        return Placeholders(english).SetEquals(Placeholders(text));
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

    private static List<string> Downloaded(string appDirectory)
    {
        var languages = new List<string>();
        var folder = Path.Combine(appDirectory, "lang");
        if (!Directory.Exists(folder))
        {
            return languages;
        }

        foreach (var directory in Directory.GetDirectories(folder))
        {
            var language = Path.GetFileName(directory);
            if (IsCulture(language) && File.Exists(DownloadedPath(appDirectory, language)))
            {
                languages.Add(language);
            }
        }

        return languages;
    }

    internal static Dictionary<string, string>? ReadDownloaded(string appDirectory, string language)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(DownloadedPath(appDirectory, language)));
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

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
