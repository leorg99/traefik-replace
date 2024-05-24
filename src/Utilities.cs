using System.Text.RegularExpressions;
using Serilog;
using Serilog.Core;

namespace TraefikReplace;

public static partial class Utilities
{
    [GeneratedRegex(@"(?<key>[^=]+)=(?<value>[^;,]+)", RegexOptions.CultureInvariant)]
    private static partial Regex ParseKeyValue();
    public static LoggingLevelSwitch MinimumLevel { get; set; } = new();

    public static List<KeyValuePair<string, string>> ParseMappings(string mappings)
    {
        var logger = Log.Logger;
        var list = new List<KeyValuePair<string, string>>();
        var matches = ParseKeyValue().Matches(mappings);

        foreach (Match match in matches)
        {
            match.Groups.TryGetValue("key", out var key);
            match.Groups.TryGetValue("value", out var value);

            if (key == null || value == null)
            {
                logger.Error("Could not parse ");
                continue;
            }

            var keyCapture = key.Captures.FirstOrDefault();
            var valueCapture = value.Captures.FirstOrDefault();

            if (keyCapture == null || valueCapture == null)
            {
                logger.Error("Could not parse ");
                continue;
            }

            list.Add(new KeyValuePair<string, string>(keyCapture.Value, valueCapture.Value));
        }

        return list;
    }
}
