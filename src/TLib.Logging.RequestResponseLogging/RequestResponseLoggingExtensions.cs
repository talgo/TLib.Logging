using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace TLib.Logging.RequestResponseLogging;

public static class RequestResponseLoggingExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(
        this IApplicationBuilder builder,
        IConfiguration config,
        string sectionName = "Logging:RequestResponse",
        Action<RequestResponseLoggerOptions>? configure = null)
    {
        var options = new RequestResponseLoggerOptions();

        // Bind from appsettings.json if section exists
        var section = config.GetSection(sectionName);
        if (section.Exists())
        {
            section.Bind(options);
        }

        // Optional override from code
        configure?.Invoke(options);

        return builder.UseMiddleware<RequestResponseLoggingMiddleware>(options);
    }
}

