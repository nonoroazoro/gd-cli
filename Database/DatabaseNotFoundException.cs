namespace GdCli;

internal sealed class DatabaseNotFoundException : Exception
{
    public DatabaseNotFoundException(string message) : base(message)
    {
    }
}
