using DotMake.CommandLine;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace TraefikReplace;

[CliCommand(
    Description = "Replace Shell Variables in Traefik Config Files",
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormPrefixConvention = CliNamePrefixConvention.SingleHyphen)]
public class RootCliCommand
{
    [CliArgument(
        Name = "path",
        Description = "The path to search for Traefik config files.",
        ValidationRules = CliValidationRules.ExistingDirectory)]
    public required string Path { get; set; } = default!;

    [CliOption(Name = "-r", Description = "Perform a recursive search using the provided path.")]
    public bool RecursiveFileSearch { get; set; } = false;

    [CliOption(Name = "--mappings", Description = "Key-value mappings in the form of \"key1=value2;key2=value2\".")]
    public string? Mappings { get; set; } = null;

    [CliOption(Name = "--debug", Description = "Enable debug logging.")]
    public bool DebugLoggingEnabled { get; set; } = false;

    public int Run()
    {
        List<KeyValuePair<string, string>>? mappings = null;

        if (DebugLoggingEnabled)
        {
            Utilities.MinimumLevel = new LoggingLevelSwitch(LogEventLevel.Debug);
        }

        if (Mappings != null)
        {
            mappings = Utilities.ParseMappings(Mappings);
        }

        try
        {
            var generator = new Generator(Path, RecursiveFileSearch, mappings);
            var result = generator.FindAndReplaceFiles();
            return result ? 0 : 1;
        }
        catch (Exception e)
        {
            Log.Logger.Error(e.Message);
            return -1;
        }
    }
}
