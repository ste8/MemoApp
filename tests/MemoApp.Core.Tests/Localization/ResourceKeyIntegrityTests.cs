using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MemoApp.Core.Tests.Localization;

/// <summary>
/// Guards the two failure modes of a resource file, from opposite directions.
///
/// A key that code asks for but no resource defines is user-visible: the lookup returns the
/// key itself, so the app displays "Share_FlashNumbers_Title" where a sentence belongs. A key
/// present in English but absent from Italian silently falls back to English.
///
/// These are source-scanning tests rather than runtime ones: the mistake happens when someone
/// writes GetString("...") without adding the entry, and the compiler cannot see it.
/// </summary>
public class ResourceKeyIntegrityTests
{
    private static readonly Regex[] KeyRequests =
    [
        new(@"GetString\(""([A-Za-z0-9_]+)""\)", RegexOptions.Compiled),
        new(@"getString\(""([A-Za-z0-9_]+)""\)", RegexOptions.Compiled),
        new(@"\{localization:Localize\s+([A-Za-z0-9_]+)\s*\}", RegexOptions.Compiled)
    ];

    [Fact]
    public void EveryKeyRequestedBySourceCodeIsDefined()
    {
        var defined = KeysIn("AppResources.resx");
        var requested = RequestedKeys();

        Assert.NotEmpty(requested);

        var missing = requested.Keys.Where(k => !defined.Contains(k)).OrderBy(k => k).ToList();

        Assert.True(missing.Count == 0,
            "These keys are requested in source but not defined in AppResources.resx, so they " +
            "render as the raw key name at runtime:\n" +
            string.Join("\n", missing.Select(k => $"  {k}  ({string.Join(", ", requested[k].Take(2))})")));
    }

    [Fact]
    public void EnglishAndItalianDefineTheSameKeys()
    {
        var english = KeysIn("AppResources.resx");
        var italian = KeysIn("AppResources.it.resx");

        var missingItalian = english.Except(italian).OrderBy(k => k).ToList();
        var extraItalian = italian.Except(english).OrderBy(k => k).ToList();

        Assert.True(missingItalian.Count == 0,
            "Present in English but missing from Italian, so Italian users see English:\n" +
            string.Join("\n", missingItalian.Select(k => $"  {k}")));

        Assert.True(extraItalian.Count == 0,
            "Present in Italian but not English:\n" + string.Join("\n", extraItalian.Select(k => $"  {k}")));
    }

    [Fact]
    public void EveryDefinedKeyHasANonEmptyValue()
    {
        foreach (var file in new[] { "AppResources.resx", "AppResources.it.resx" })
        {
            var blank = Entries(file).Where(e => string.IsNullOrWhiteSpace(e.Value)).Select(e => e.Key).ToList();

            Assert.True(blank.Count == 0, $"{file} has entries with no value:\n" + string.Join("\n", blank));
        }
    }

    // ---------------------------------------------------------------

    private static Dictionary<string, List<string>> RequestedKeys()
    {
        var found = new Dictionary<string, List<string>>();

        foreach (var file in SourceFiles())
        {
            var relative = Path.GetRelativePath(RepositoryRoot, file);

            foreach (var line in File.ReadLines(file))
            {
                // Comments describe the pattern rather than using it.
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("<!--"))
                {
                    continue;
                }

                foreach (var pattern in KeyRequests)
                {
                    foreach (Match match in pattern.Matches(line))
                    {
                        var key = match.Groups[1].Value;
                        if (!found.TryGetValue(key, out var sources))
                        {
                            found[key] = sources = [];
                        }

                        if (!sources.Contains(relative))
                        {
                            sources.Add(relative);
                        }
                    }
                }
            }
        }

        return found;
    }

    private static IEnumerable<string> SourceFiles()
        => Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".cs") || f.EndsWith(".xaml"))
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                             && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                    .Where(f => !Path.GetFileName(f).StartsWith("AppResources"));

    private static HashSet<string> KeysIn(string fileName)
        => Entries(fileName).Select(e => e.Key).ToHashSet();

    private static IEnumerable<KeyValuePair<string, string>> Entries(string fileName)
    {
        var path = Path.Combine(RepositoryRoot, "src", "MemoApp.Localization", "Resources", fileName);

        return XDocument.Load(path).Root!
            .Elements("data")
            .Select(d => new KeyValuePair<string, string>(
                d.Attribute("name")!.Value,
                d.Element("value")?.Value ?? string.Empty));
    }

    private static string RepositoryRoot { get; } = FindRepositoryRoot();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MemoApp.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "Could not locate the repository root (no MemoApp.sln found above the test output directory).");
    }
}
