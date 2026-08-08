using System.Text.Encodings.Web;
using System.Text.Json;
using DevLab.JmesPath;

namespace GdCli.Output;

internal static class JsonOutput
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Write(TextWriter writer, object value, string? query = null)
    {
        string json;
        try
        {
            json = JsonSerializer.Serialize(value, _options);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            throw new JsonSerializationOutputException(exception);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            try
            {
                json = new JmesPath().Transform(json, query) ?? "null";
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                throw new OutputQueryException($"Invalid JMESPath query: {exception.Message}", exception);
            }
        }

        writer.WriteLine(json);
    }

    public static void ValidateQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        try
        {
            _ = new JmesPath().Parse(query);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            throw new OutputQueryException(
                $"Invalid JMESPath query: {exception.Message}",
                exception);
        }
    }
}
