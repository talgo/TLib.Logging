# 📦 RequestResponseLogging

A **reusable ASP.NET Core middleware library** for logging HTTP requests and responses with optional correlation IDs, sensitive data masking, and configurable options. Ideal for APIs, microservices, and distributed systems.

---

## Features

- Logs **request headers**, **query string**, and **request body**  
- Logs **response body**  
- Optional **correlation ID generation** with custom header name  
- Supports using **existing correlation IDs** from incoming headers  
- **Sensitive data masking** (passwords, tokens, etc.)  
- Ability to ignore specific paths (e.g., /health, /swagger, /metrics) to reduce noise
- Fully configurable via **appsettings.json** or code overrides  
- Lightweight, reusable, and easy to integrate  

---

## Installation

1. Add the library project to your solution, or build and reference it as a NuGet package.  
2. Ensure your Web API project has Serilog or your preferred logging provider configured.

---

## Configuration

### `appsettings.json` Example

```json
{
  "Logging": {
    "RequestResponse": {
      "LogHeaders": true,
      "LogQueryString": true,
      "LogRequestBody": true,
      "LogResponseBody": true,
      "MaskSensitiveData": true,
      "SensitiveFields": [ "password", "token", "authorization" ],
      "EnableCorrelationId": true,
      "UseExistingCorrelationId": true,
      "CorrelationIdHeader": "X-Correlation-ID",
      "IgnoredPaths": [
        "/health",
        "/metrics",
        "/swagger",
        "/favicon.ico",
        "/scalar",
        "/openapi"
      ]
    }
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| LogHeaders | bool | true | Log request headers |
| LogQueryString | bool | true | Log query string parameters |
| LogRequestBody | bool | true | Log request body |
| LogResponseBody | bool | true | Log response body |
| MaskSensitiveData | bool | false | Mask sensitive fields in logs |
| SensitiveFields | List<string> | `password, token, authorization, apiKey` | Fields to mask if masking enabled |
| EnableCorrelationId | bool | true | Generate a correlation ID if none exists |
| UseExistingCorrelationId | bool | true | Use correlation ID from incoming headers if present |
| CorrelationIdHeader | string | `X-Correlation-ID` | Header name for correlation ID |
| IgnoredPaths | List<string> | empty | Paths to exclude from logging (supports prefix matching; e.g., /swagger ignores all Swagger endpoints) |

---

## Usage

### Program.cs Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog (optional)
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

var app = builder.Build();

// Use request-response logging middleware
app.UseRequestResponseLogging(builder.Configuration);

// Map controllers
app.MapControllers();

app.Run();
```

---

### Optional Code Override

```csharp
app.UseRequestResponseLogging(builder.Configuration, options =>
{
    options.MaskSensitiveData = false;  // override appsettings.json
    options.CorrelationIdHeader = "X-Trace-ID";
});
```

---

## Example Output

### Request Logged

```
Header: Content-Type=application/json
QueryString: ?user=123
Body: {"username":"john","password":"***MASKED***"}
```

### Response Logged

```
Body: {"status":"ok","token":"***MASKED***"}
```

---

## License

MIT License – free to use and modify.  

---

## Optional Enhancements

- Add database logging (SQL, MongoDB)  
- Add Kafka/RabbitMQ logging sinks  
- Integrate with OpenTelemetry or distributed tracing systems  
- Filter logs by route or controller via attributes  
