using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SimpleImageIO;

/// <summary>
/// An entry in the image dataset. Corresponds to all images that matched the query regex and had identical
/// values in their captured expressions (e.g., same subdirectory name).
/// </summary>
/// <param name="Path">The list of captured values (e.g., directory names) in the regex</param>
/// <param name="Images">All images that share this <see cref="Path"/></param>
/// <param name="AuxFiles">List of any non-image files that also match the regex and share this path</param>
public record ImageDatasetEntry(string[] Path, Dictionary<string, LazyImage> Images, List<string> AuxFiles) {
    /// <summary>
    /// </summary>
    /// <returns>The <see cref="Path"/> entries concatenated with a slash ('/')</returns>
    public override string ToString() => string.Join('/', Path);
}

/// <summary>
/// LINQ extensions for the image dataset feature
/// </summary>
public static class EnumerableExtensions {
    /// <summary>
    /// Shorthand to convert an enumerable of key-value pairs to a dictionary
    /// </summary>
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
    => source.ToDictionary(item => item.Key, item => item.Value);
}

/// <summary>
/// Loads all images (and other files) in a directory that match a regular expression. Images are grouped
/// based on captures in the regex so they can be queried via Linq.
/// </summary>
public class ImageDataSet {
    /// <summary>
    /// List of all captured path components in any of the loaded files. Convenient way to quickly see what
    /// has been loaded.
    /// </summary>
    public readonly List<HashSet<string>> PathComponents;

    /// <summary>
    /// List of all image names (i.e., bottom level of the loaded file hierarchy) that have been loaded.
    /// Convenient way to quickly see what has been loaded.
    /// </summary>
    public readonly HashSet<string> ImageNames;

    /// <summary>
    /// The loaded image data. This is a flattened representation, i.e., each entry corresponds to one (set of)
    /// files that matched the query regex.
    /// Use Linq to query the images that you desire, or <see cref="GetImages(string, IEnumerable&lt;string&gt;)"/> /
    /// <see cref="GetImages(string, IEnumerable&lt;string&gt;)"/>
    /// </summary>
    public readonly List<ImageDatasetEntry> Data;

    static IEnumerable<string> GetGroupStrings(Match match)
    => match.Groups.Cast<Group>().ToArray()[1..].Select(g => g.Value);

    static IEnumerable<string[]> GetMatches(string basePath, Regex regex) {
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
    public static Regex RegexFromPattern(string pattern) {
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
        return new Regex($"^{pattern}$", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Creates a regular expression that matches any of the given search patterns. (i.e., or-combined)
    /// </summary>
    public static Regex AnyPattern(params string[] v) {
        // Convert each pattern to a regex and put it in an uncaptured group
        // (uncaptured so that it does not interfere with the capture match = sorting logic)
        var groupedRegexes = v.Select(x => $"(?:{RegexFromPattern(x).ToString()})");

        // Return or-combined regex
        return new Regex(string.Join('|', groupedRegexes), RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Creates a dataset of images (and aux files) by scanning the given directory for all files that
    /// match a regular expression.
    /// Images are grouped hierarchically based on the capture groups in the expression(s). For example,
    /// (.*)/(.*)\.exr
    /// Adds all .exr images that are in a subdirectory of the root.
    /// </summary>
    /// <param name="basePath">Path to the root directory</param>
    /// <param name="regex">A regular expression that will be matched against each file.</param>
    public ImageDataSet(string basePath, Regex regex) {
        var data = GetMatches(basePath, regex);

        PathComponents = new();
        for (int i = 0; ; ++i) {
            var options = data.Where(x => x.Length > i + 1).Select(x => x[i]).Distinct();
            if (options.Count() == 0) break;
            PathComponents.Add(options.ToHashSet());
        }

        Data = data
            .GroupBy(x => x.SkipLast(1).Aggregate((a,b) => $"{a}/{b}"))
            .AsParallel().AsOrdered().Select(x => {
                var layers = new Dictionary<string, LazyImage>();
                var looseFiles = new List<string>();
                foreach (var (dirname, filename) in x.Select(x => (Path.Join(x.SkipLast(1).ToArray()), x.Last()))) {
                    string name = Path.GetRelativePath(dirname, filename).Replace('\\', '/');
                    string fullPath = Path.Join(basePath, filename);

                    // Handle case where the image is a single file (example pattern: "<foo>/<bar>.exr")
                    if (name.StartsWith("../")) name = name.Substring(3);

                    if (filename.EndsWith(".exr", ignoreCase: true, culture: CultureInfo.InvariantCulture)) {
                        foreach (var layerName in Layers.GetLayerNames(fullPath)) {
                            string n = name + (string.IsNullOrEmpty(layerName) ? "" : $".{layerName}");
                            layers.Add(n, new(fullPath, layerName));
                        }
                    } else if(Image.HasSupportedExtension(filename))
                        layers.Add(name, new(fullPath));
                    else
                        looseFiles.Add(fullPath);
                }
                return new ImageDatasetEntry(x.First().SkipLast(1).ToArray(), layers, looseFiles);
            }).ToList();

        ImageNames = Data.SelectMany(x => x.Images.Keys).Distinct().ToHashSet();
    }

    /// <summary>
    /// Creates a dataset of images (and aux files) by scanning the current working directory for all files that
    /// match a regular expression.
    /// Images are grouped hierarchically based on the capture groups in the expression(s). For example,
    /// (.*)/(.*)\.exr
    /// Adds all .exr images that are in a subdirectory of the root.
    /// </summary>
    /// <param name="regex">A regular expression that will be matched against each file.</param>
    public ImageDataSet(Regex regex) : this(".", regex) { }

    /// <summary>
    /// Creates a dataset of images (and aux files) by scanning the given directory for all files that
    /// match a pattern.
    /// The pattern has a form like:
    /// <code>
    /// <![CDATA[ <1>/<2>/**/*.exr ]]>
    /// </code>
    /// On every OS, a slash ("/") should be used as the directory separator. The placeholders like "&lt;1&gt;"
    /// will be captured as a path component in the resulting dataset.
    /// Placeholder names can be arbitrary strings. If, as in the example here, the names are numbers, then these
    /// also define the component order in the resulting dataset.
    /// Multiple patterns can be or-combined via <see cref="AnyPattern" />. Then, placeholders with identical
    /// names will be combined into a component.
    /// </summary>
    /// <param name="basePath">Path to the root directory</param>
    /// <param name="pattern">The pattern that will be matched against each file.</param>
    public ImageDataSet(string basePath, string pattern) : this(basePath, RegexFromPattern(pattern)) { }

    /// <summary>
    /// Creates a dataset of images (and aux files) by scanning the current working directory for all files that
    /// match a pattern.
    /// The pattern has a form like:
    /// <code>
    /// <![CDATA[ <1>/<2>/**/*.exr ]]>
    /// </code>
    /// On every OS, a slash ("/") should be used as the directory separator. The placeholders like "&lt;1&gt;"
    /// will be captured as a path component in the resulting dataset.
    /// Placeholder names can be arbitrary strings. If, as in the example here, the names are numbers, then these
    /// also define the component order in the resulting dataset.
    /// Multiple patterns can be or-combined via <see cref="AnyPattern" />. Then, placeholders with identical
    /// names will be combined into a component.
    /// </summary>
    /// <param name="pattern">The pattern that will be matched against each file.</param>
    public ImageDataSet(string pattern) : this(".", RegexFromPattern(pattern)) { }

    /// <summary>
    /// Retrieves all images with the same name and identical path components. Grouped by the first
    /// path component that was not part of the filter criterion.
    /// If no such path component exists, or the grouping is not unique, those images are ignored.
    /// </summary>
    /// <param name="components">List of path components that must match in all selected images</param>
    /// <param name="imageName">Name of the image</param>
    public Dictionary<string, Image> GetImages(string imageName, IEnumerable<string> components)
    => Data
        .Where(x => x.Path.Zip(components).All(v => v.Item1 == v.Item2))
        .Where(x => x.Images.ContainsKey(imageName))
        .GroupBy(x => x.Path[components.Count()])
        .Where(group => group.Count() == 1)
        .ToDictionary(group => group.Key, group => group.First().Images[imageName].Image);

    /// <summary>
    /// Retrieves all images with the same name and identical path components. Grouped by the first
    /// path component that was not part of the filter criterion.
    /// If no such path component exists, or the grouping is not unique, those images are ignored.
    /// </summary>
    /// <param name="components">List of path components that must match in all selected images</param>
    /// <param name="imageName">Name of the image</param>
    public Dictionary<string, Image> GetImages(string imageName, params string[] components)
    => GetImages(imageName, components as IEnumerable<string>);

    ImageDataSet(IEnumerable<HashSet<string>> path, IEnumerable<ImageDatasetEntry> data) {
        Data = data.ToList();
        PathComponents = path.ToList();
        ImageNames = Data.SelectMany(x => x.Images.Keys).Distinct().ToHashSet();
    }

    /// <summary>
    /// Filters the dataset by extracting all entries with a given first path component
    /// </summary>
    /// <param name="component">The desired first component</param>
    /// <returns>A new dataset with all extracted entries (image data is not duplicated)</returns>
    public ImageDataSet this[string component]
    => new(
        PathComponents.Skip(1),
        Data
            .Where(x => x.Path.Any() && x.Path[0] == component) // elements that match the first component
            .Select(x => new ImageDatasetEntry(x.Path[1..], x.Images, x.AuxFiles)) // shorten path of each
    );

    /// <summary>
    /// The first image in the first entry of this dataset. Useful if a dataset contains only one image.
    /// </summary>
    public LazyImage FirstImage => Data[0].Images.First().Value;

    /// <summary>
    /// Queries all auxiliary files with .json ending of image sets with identical first
    /// path components; grouped by the first path component that is not part of the filtering criterion.
    /// </summary>
    public Dictionary<string, JsonNode> GetAuxJson(IEnumerable<string> components)
    => Data
        .Where(x => x.Path.Zip(components).All(v => v.Item1 == v.Item2))
        .Select(x => {
            var auxJson = x.AuxFiles.Where(y => y.EndsWith(".json"));
            if (auxJson.Count() > 0)
                return (x.Path[components.Count()], JsonNode.Parse(File.ReadAllText(auxJson.First())));
            return (x.Path[components.Count()], null);
        })
        .Where(x => x.Item2 != null)
        .GroupBy(x => x.Item1)
        .Where(group => group.Count() == 1)
        .ToDictionary(
            x => x.Key,
            x => x.First().Item2
        );

    /// <summary>
    /// Queries all auxiliary files with .json ending of image sets with identical first
    /// path components; grouped by the first path component that is not part of the filtering criterion.
    /// </summary>
    public Dictionary<string, JsonNode> GetAuxJson(params string[] components)
    => GetAuxJson(components as IEnumerable<string>);

    /// <summary>
    /// Queries all auxiliary files with the same name across all entries that share the same path prefix.
    /// </summary>
    /// <param name="fileName">File name of the auxiliary file</param>
    /// <param name="components">Path prefix that must be identical across all entries</param>
    /// <returns>Full paths to the auxiliary file, grouped by the first path component after the common prefix.</returns>
    public Dictionary<string, string> GetAuxFiles(string fileName, IEnumerable<string> components)
    => Data
        .Where(entry => entry.Path.Zip(components).All(v => v.Item1 == v.Item2))
        .Where(entry => entry.AuxFiles.Contains(fileName))
        .GroupBy(entry => entry.Path[components.Count()])
        .Where(group => group.Count() == 1)
        .ToDictionary(
            x => x.Key,
            x => x.First().AuxFiles.Find(name => name.EndsWith(fileName))
        );
}
