using Serilog;
using Serilog.Events;

namespace QACopilot.API.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            // Override especifico: los logs de diagnostico de hosting incluyen
            // la URL completa del request (incluyendo query string), lo cual
            // expone access_token en las conexiones de SignalR. Se restringe
            // a Error para que nunca se impriman en Information/Warning.
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Error)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logsPath, "qacopilot-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logsPath, "errors", "error-.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                retainedFileCountLimit: 90,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        // CRITICO: sin esto, el logger por defecto de ASP.NET Core
        // (Microsoft.Extensions.Logging.Console) sigue activo EN PARALELO
        // a Serilog, ignorando por completo los overrides configurados arriba.
        // Ese logger por defecto es el que imprimia las URLs completas de
        // SignalR con el access_token JWT en texto plano.
        builder.Logging.ClearProviders();
        builder.Host.UseSerilog();

        return builder;
    }
}
