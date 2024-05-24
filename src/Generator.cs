using System.Text;
using System.Text.RegularExpressions;
using Serilog;

namespace TraefikReplace;

public partial class Generator
{

    [GeneratedRegex(@"\${?(\w+)}?", RegexOptions.CultureInvariant)]
    private static partial Regex FindPlaceholder();

    private readonly ILogger _logger = Log.ForContext<Generator>();

    private readonly string _path;
    private readonly Dictionary<string, string> _mappings = new(StringComparer.InvariantCultureIgnoreCase);

    public Generator(string path, bool recursiveFileSearch = true, IEnumerable<KeyValuePair<string, string>>? mappings = null)
    {
        // Normalize slashes for environment
        _path = Path.GetFullPath(path);

        if (mappings == null)
        {
            return;
        }

        foreach (var (key, value) in mappings)
        {
            if (!_mappings.TryAdd(key, value))
            {
                throw new ArgumentException($"Key '{key}' has duplicates.");
            }
        }
    }

    public bool FindAndReplaceFiles()
    {
        var extensions = new[] { ".yml", ".toml" };
        var files = Directory.EnumerateFiles(_path, "*", SearchOption.AllDirectories)
            .Where(file =>
                !Path.GetFileNameWithoutExtension(file).EndsWith(".g") &&
                extensions.Contains(Path.GetExtension(file)));

        foreach (var file in files)
        {
            var result = FindAndReplace(file);
            if (!result)
            {
                return false;
            }
        }

        return true;
    }

    private bool FindAndReplace(string filePath)
    {
        _logger.Information("Processing file {FilePath}", filePath);

        var contents = File.ReadAllText(filePath);
        var matches = FindPlaceholder().Matches(contents);
        var outputBuilder = new StringBuilder(contents.Length);

        var i = 0;

        foreach (Match match in matches)
        {
            if (!match.Groups.TryGetValue("1", out var keyGroup))
            {
                _logger.Error("Could not find capture group for {Match}.", keyGroup);
                return false;
            }

            var key = keyGroup.Value;
            _logger.Debug("Found key {Key}.", key);

            if (!TryGetMapValue(key, out var value))
            {
                _logger.Error("No mapping was found for '{Key}'.", key);
                return false;
            }

            _logger.Debug("Replacing {Key} with {Value}.", key, value);

            outputBuilder.Append(contents, i, match.Index);
            outputBuilder.Append(value);
            i = match.Index + match.Length;
        }

        if (i == 0)
        {
            _logger.Debug("No placeholders found in file {FilePath}.", filePath);
            return true;
        }

        if (i > 0 && i < contents.Length)
        {
            outputBuilder.Append(contents, i, contents.Length - i);
        }

        var generatedFilePath = GetGeneratedOutputPath(filePath);
        WriteGeneratedConfig(generatedFilePath, outputBuilder);

        _logger.Information("Generated file {GeneratedFilePath}", generatedFilePath);
        _logger.Debug("Done processing file {FilePath}.", filePath);

        return true;
    }

    private bool TryGetMapValue(string key, out string? value)
    {
        if (_mappings.TryGetValue(key, out value))
        {
            return true;
        }

        value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        _mappings.Add(key, value);
        return true;
    }
    private static string GetGeneratedOutputPath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath)!;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var ext = Path.GetExtension(filePath);

        var generatedFileName = $"{fileName}.g{ext}";
        return Path.Combine(directory, generatedFileName);
    }

    private static void WriteGeneratedConfig(string filePath, StringBuilder stringBuilder)
    {
        using var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write);
        var writer = new StreamWriter(file, new UTF8Encoding(false));
        writer.Write(stringBuilder);
    }
}
