namespace TLib.Logging.RequestResponseLogging;

public class RequestResponseLoggerOptions
{
    public bool LogHeaders { get; set; } = true;
    public bool LogQueryString { get; set; } = true;
    public bool LogRequestBody { get; set; } = true;
    public bool LogResponseBody { get; set; } = true;
    public bool MaskSensitiveData { get; set; } = false;

    public List<string> SensitiveFields { get; set; } = new()
    {
        "password", "token", "authorization", "apiKey"
    };

    // Correlation ID options
    public bool EnableCorrelationId { get; set; } = true;
    public bool UseExistingCorrelationId { get; set; } = true;
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// Paths that should NOT be logged
    /// Example:
    /// [ "/health", "/metrics", "/swagger" ]
    /// Supports BeginsWith matching.
    /// </summary>
    public List<string> IgnoredPaths { get; set; } = new();
}
