using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SimpleImageIO;

public record ImageDatasetEntry(string[] Path, Dictionary<string, Image> Images, List<string> AuxFiles) {
    public override string ToString() => string.Join('/', Path);
}

public class ImageDataSet {
    public readonly List<HashSet<string>> PathComponents;
    public readonly HashSet<string> ImageNames;
    public readonly List<ImageDatasetEntry> Data;

    static IEnumerable<string> GetGroupStrings(Match match)
    => match.Groups.Cast<Group>().ToArray()[1..].Select(g => g.Value);

    static IEnumerable<string[]> GetMatches(string basePath, string pattern) {
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        return Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories)
            .Select(f => System.IO.Path.GetRelativePath(basePath, f))
            .Select(f => regex.Match(f))
            .Where(m => m.Success)
            .Select(m => GetGroupStrings(m).Append(m.Value).ToArray())
            .OrderBy(v => string.Join(' ', v));
    }

    /// <summary>
    /// Converts a path search string containing placeholders (&lt;something&gt;), wildcards (*), and
    /// recursive wildcards (**) to a regular expression with named capture groups for each placeholder.
    /// </summary>
    public static string RegexFromPattern(string pattern) {
        pattern = pattern.Replace("/", @"[\\/]");
        pattern = pattern.Replace(".", @"\.");

        // Must be careful to handle ** versus * so they don't destroy each other
        // TODO there is probably a cleaner way to do this by tracking indices - but not a bottleneck right now
        pattern = pattern.Replace("**", "13_ANY_CHAR_HERE_42");
        pattern = pattern.Replace("*", "13_ANY_CHAR_HERE_NOT_DIRSEP_42");
        pattern = pattern.Replace("13_ANY_CHAR_HERE_42", @".*");
        pattern = pattern.Replace("13_ANY_CHAR_HERE_NOT_DIRSEP_42", @"[^\\/]*");

        Regex placeholder = new("<[^>]*>");
        var matches = placeholder.Matches(pattern);
        foreach (Match m in matches) {
            pattern = pattern.Replace(m.Value, @$"(?{m.Value}[^\\/]*)");
        }
        return $"^{pattern}$";
    }

    /// <summary>
    /// Combines multiple regular expresions by wrapping each in a non-captured group (i.e., "(?: )") and
    /// or-ing these groups.
    /// </summary>
    public static string AnyRegex(params string[] v) {
        return string.Join('|', v.Select(x => $"(?:{x})"));
    }

    /// <summary>
    /// Creates a regular expression that matches any of the given search patterns. (i.e., or-combined)
    /// </summary>
    public static string AnyPattern(params string[] v) => AnyRegex(v.Select(x => RegexFromPattern(x)).ToArray());

    public ImageDataSet(string basePath, string pattern) {
        var data = GetMatches(basePath, pattern);

        PathComponents = new();
        for (int i = 0; ; ++i) {
            var options = data.Where(x => x.Length > i + 1).Select(x => x[i]).Distinct();
            if (options.Count() == 0) break;
            PathComponents.Add(options.ToHashSet());
        }

        Data = data
            .GroupBy(x => x.SkipLast(1).Aggregate((a,b) => $"{a}/{b}"))
            .AsParallel().AsOrdered().Select(x => {
                var layers = new Dictionary<string, Image>();
                var looseFiles = new List<string>();
                foreach (var (dirname, filename) in x.Select(x => (Path.Join(x.SkipLast(1).ToArray()), x.Last()))) {
                    string name = Path.GetRelativePath(dirname, filename).Replace('\\', '/');
                    if (filename.EndsWith(".exr", ignoreCase: true, culture: CultureInfo.InvariantCulture)) {
                        var exrLayers = Layers.LoadFromFile(Path.Join(basePath, filename));
                        foreach (var (k, v) in exrLayers) {
                            string n = name + (string.IsNullOrEmpty(k) ? "" : $".{k}");
                            layers.Add(n, v);
                        }
                    } else if(Image.HasSupportedExtension(filename)) {
                        var img = new RgbImage(Path.Join(basePath, filename));
                        layers.Add(name, img);
                    } else {
                        looseFiles.Add(Path.Join(basePath, filename));
                    }
                }
                return new ImageDatasetEntry(x.First().SkipLast(1).ToArray(), layers, looseFiles);
            }).ToList();

        ImageNames = Data.SelectMany(x => x.Images.Keys).Distinct().ToHashSet();
    }

    public Dictionary<string, Image> GetMethodImages(string sceneName, string imageName = "")
    => Data
        .Where(x => x.Path[0] == sceneName && x.Path.Length > 1)
        .Where(x => x.Images.ContainsKey(imageName))
        .ToDictionary(x => x.Path[1], x => x.Images[imageName]);

    public Dictionary<string, Image> GetSceneImages(string imageName = "")
    => Data
        .Where(x => x.Images.ContainsKey(imageName))
        .ToDictionary(x => x.Path[0], x => x.Images[imageName]);

    public Dictionary<string, JsonNode> GetAuxJson(string sceneName)
    => Data
        .Where(x => x.Path[0] == sceneName && x.Path.Length > 1)
        .Select(x => {
            var auxJson = x.AuxFiles.Where(y => y.EndsWith(".json"));
            if (auxJson.Count() > 0)
                return (x.Path[1], JsonNode.Parse(File.ReadAllText(auxJson.First())));
            return (x.Path[1], null);
        })
        .Where(x => x.Item2 != null)
        .ToDictionary(
            x => x.Item1,
            x => x.Item2
        );

    public IEnumerable<string> ListAuxJsonProperties(string sceneName)
    => GetAuxJson(sceneName)
        .SelectMany(kv => kv.Value.AsObject().Select(x => x.Key))
        .Distinct();
}
