namespace GdCli.Output;

internal sealed class JsonSerializationOutputException : Exception
{
    public JsonSerializationOutputException(Exception innerException)
        : base($"JSON serialization failed: {innerException.Message}", innerException)
    {
    }
}
