using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;

namespace TLib.Logging.RequestResponseLogging;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestResponseLoggerOptions _options;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(
        RequestDelegate next,
        RequestResponseLoggerOptions options,
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip logging for ignored paths
        if (_options.IgnoredPaths.Any(ignore =>
                path.StartsWith(ignore, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var correlationId = GetOrCreateCorrelationId(context);
        if (!string.IsNullOrEmpty(correlationId))
            context.Items["CorrelationId"] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        // === Log Request ===
        var requestInfo = await CaptureRequest(context);

        // Swap body to capture response
        var originalBody = context.Response.Body;
        using var newBody = new MemoryStream();
        context.Response.Body = newBody;

        await _next(context);

        stopwatch.Stop();

        // === Log Response ===
        var responseInfo = await CaptureResponse(context, newBody);

        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature == null)
        {

            _logger.LogInformation(
                "{Method} {Path} responded status code {StatusCode} in {Elapsed}ms | CorrelationId={CorrelationId} | Request= {Request} | Response= {Response}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                requestInfo,
                responseInfo
            );
        }
        else
        {
            _logger.LogError(
                exceptionFeature.Error,
                "{Method} {Path} responded status code {StatusCode} in {Elapsed}ms | CorrelationId={CorrelationId} | Request= {Request} | Response=  {Response}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                requestInfo,
                responseInfo
            );
        }

        await newBody.CopyToAsync(originalBody);
    }

    private async Task<string> CaptureRequest(HttpContext context)
    {
        var request = context.Request;
        var sb = new StringBuilder();

        if (_options.LogHeaders)
        {
            foreach (var header in request.Headers)
                sb.Append($"Header: {header.Key}={header.Value} ");
        }

        if (_options.LogQueryString && request.QueryString.HasValue)
        {
            sb.Append($"QueryString: {request.QueryString.Value} ");
        }

        if (_options.LogRequestBody)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            if (_options.MaskSensitiveData)
                body = SensitiveDataMasker.Mask(body, _options.SensitiveFields);

            sb.AppendLine($"Body: {body}");
        }

        return sb.ToString();
    }

    private async Task<string> CaptureResponse(HttpContext context, MemoryStream bodyStream)
    {
        if (!_options.LogResponseBody)
            return "Response body logging disabled";

        bodyStream.Position = 0;
        var text = await new StreamReader(bodyStream).ReadToEndAsync();
        bodyStream.Position = 0;

        if (_options.MaskSensitiveData)
            text = SensitiveDataMasker.Mask(text, _options.SensitiveFields);

        return text;
    }

    private string GetOrCreateCorrelationId(HttpContext context)
    {
        if (_options.UseExistingCorrelationId)
        {
            if (context.Request.Headers.TryGetValue(_options.CorrelationIdHeader, out var existing))
                return existing!;

            string[] commonHeaders = { "X-Request-ID", "Correlation-Id", "Request-Id", "traceparent" };
            foreach (var header in commonHeaders)
            {
                if (context.Request.Headers.TryGetValue(header, out var value))
                    return value!;
            }
        }

        if (!_options.EnableCorrelationId)
            return string.Empty;

        var newId = Guid.NewGuid().ToString();
        context.Response.Headers[_options.CorrelationIdHeader] = newId;
        return newId;
    }

}

