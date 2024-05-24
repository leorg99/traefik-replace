using DotMake.CommandLine;
using Serilog.Events;
using Serilog;
using TraefikReplace;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(Utilities.MinimumLevel)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

return Cli.Run<RootCliCommand>(args);
