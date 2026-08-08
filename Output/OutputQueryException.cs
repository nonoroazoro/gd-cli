namespace GdCli.Output;

internal sealed class OutputQueryException : Exception
{
    public OutputQueryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
