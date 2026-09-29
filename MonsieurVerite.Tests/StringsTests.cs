using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace MonsieurVerite.Tests;

public class StringsTests
{
    // The generator matches keys with a regex, since MSBuild gives it no JSON reader; the app
    // reads the same file with a real one, and the two must agree.
    [Fact]
    public void EveryEnglishKeyIsAMemberAndEveryMemberHasText()
    {
        var members = typeof(Strings)
            .GetMembers(BindingFlags.Public | BindingFlags.Static)
            .Select(member => member.Name)
            .Where(name => Regex.IsMatch(name, "^[A-Z][A-Z0-9_]*$"))
            .Distinct()
            .Order()
            .ToList();

        Assert.Equal(Strings.English.Keys.Order(), members);
        Assert.All(Strings.English, pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value), pair.Key));
    }

    // x:Static resolves only public members, and only when the window that names one is opened,
    // so neither the build nor starting the app catches a string it cannot see.
    [Fact]
    public void EveryStringTheXamlNamesIsPublic()
    {
        var project = Path.Combine(RepositoryRoot(), "MonsieurVerite");
        var names = Directory.EnumerateFiles(project, "*.xaml", SearchOption.AllDirectories)
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"local:Strings\.(\w+)"))
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(names);
        Assert.All(names, name =>
            Assert.NotNull(typeof(Strings).GetProperty(name, BindingFlags.Public | BindingFlags.Static)));
    }

    // A translation that drops or renames a placeholder shows it raw, and only when that message
    // comes up.
    [Fact]
    public void TranslationsKeepTheEnglishKeysAndPlaceholders()
    {
        Assert.Contains("en-US", Strings.Languages);
        foreach (var language in Strings.Languages.Where(language => language != "en-US"))
        {
            CultureInfo.GetCultureInfo(language, predefinedOnly: true);
            foreach (var (key, text) in Strings.Load(language).Where(pair => pair.Value.Length > 0))
            {
                Assert.True(Strings.English.ContainsKey(key), $"{language}: {key} is not in en-US");
                Assert.Equal(Placeholders(Strings.English[key]), Placeholders(text));
            }
        }
    }

    [Fact]
    public void PlaceholdersAreFilledOnceSoAValueCannotFillAnother()
    {
        Assert.Equal("Could not read {error}: boom", Strings.FOLDER_UNREADABLE_LOG("{error}", "boom"));
    }

    [Theory]
    [InlineData("de-AT", "de-DE")]
    [InlineData("zh-HK", "zh-TW")]
    [InlineData("zh-SG", "zh-CN")]
    [InlineData("pt-PT", "pt-BR")]
    [InlineData("en-GB", "en-US")]
    [InlineData("fr-FR", null)]
    public void TheClosestLanguageIsChosen(string culture, string? expected)
    {
        string[] languages = ["en-US", "de-DE", "zh-CN", "zh-TW", "pt-BR"];

        Assert.Equal(expected, Strings.Closest(CultureInfo.GetCultureInfo(culture), languages));
    }

    [Fact]
    public void ADownloadedStringReplacesTheBuiltInOneOnlyWhereItsPlaceholdersMatch()
    {
        var builtIn = new Dictionary<string, string>
        {
            ["OPEN_FOLDER"] = "Ordner öffnen",
            ["ALREADY_QUEUED_LOG"] = "{file} ist schon in der Warteschlange.",
        };
        var downloaded = new Dictionary<string, string>
        {
            ["OPEN_FOLDER"] = "Ordner auswählen",
            ["ALREADY_QUEUED_LOG"] = "{path} ist schon in der Warteschlange.",
            ["ADD_FILES"] = "",
            ["NOT_A_KEY_YET"] = "Neu",
        };

        var merged = Strings.Merge(builtIn, downloaded);

        Assert.Equal("Ordner auswählen", merged["OPEN_FOLDER"]);
        Assert.Equal("{file} ist schon in der Warteschlange.", merged["ALREADY_QUEUED_LOG"]);
        Assert.False(merged.ContainsKey("ADD_FILES"));
        Assert.False(merged.ContainsKey("NOT_A_KEY_YET"));
    }

    [Fact]
    public void ALanguageThatOnlyExistsAsADownloadIsStillFound()
    {
        using var scratch = new ScratchFolder();
        var path = Strings.DownloadedPath(scratch.Root, "th-TH");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, """{ "OPEN_FOLDER": "เปิดโฟลเดอร์" }""");

        var translation = Strings.Resolve(CultureInfo.GetCultureInfo("th-TH"), scratch.Root);

        Assert.Equal("เปิดโฟลเดอร์", translation?["OPEN_FOLDER"]);
    }

    [Fact]
    public void ANullStringInADownloadIsSkipped()
    {
        using var scratch = new ScratchFolder();
        var path = Strings.DownloadedPath(scratch.Root, "th-TH");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, """{ "OPEN_FOLDER": null, "ADD_FILES": "เพิ่มไฟล์" }""");

        var translation = Strings.Resolve(CultureInfo.GetCultureInfo("th-TH"), scratch.Root);

        Assert.Equal(["ADD_FILES"], translation?.Keys);
    }

    [Fact]
    public void AnUnreadableDownloadOrAStrayFolderIsIgnored()
    {
        using var scratch = new ScratchFolder();
        var path = Strings.DownloadedPath(scratch.Root, "th-TH");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "not json");
        var stray = Strings.DownloadedPath(scratch.Root, "old translations");
        Directory.CreateDirectory(Path.GetDirectoryName(stray)!);
        File.WriteAllText(stray, "{}");

        Assert.Empty(Strings.Resolve(CultureInfo.GetCultureInfo("th-TH"), scratch.Root)!);
        Assert.Null(Strings.Resolve(CultureInfo.GetCultureInfo("fr-FR"), scratch.Root));
    }

    private static List<string> Placeholders(string text) =>
        Strings.Placeholder().Matches(text).Select(match => match.Value).Order().ToList();

    private static string RepositoryRoot([CallerFilePath] string path = "") =>
        Path.GetDirectoryName(Path.GetDirectoryName(path))!;
}
