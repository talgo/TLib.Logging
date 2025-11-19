using System.Text.Json;

namespace TLib.Logging.RequestResponseLogging;

internal class SensitiveDataMasker
{
    public static string Mask(string json, List<string> fields)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var result = MaskElement(doc.RootElement, fields);
            return JsonSerializer.Serialize(result);
        }
        catch
        {

            return json;
        }
    }

    private static object MaskElement(JsonElement element, List<string> fields)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in element.EnumerateObject())
            {
                if (fields.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                {
                    dict[prop.Name] = "***MASKED***";
                }
                else
                {
                    dict[prop.Name] = MaskElement(prop.Value, fields);
                }
            }

            return dict;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray()
                .Select(e => MaskElement(e, fields))
                .ToList();
        }   

        return element.GetRawText();
    }
}
