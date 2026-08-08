namespace GdCli.Database;

internal sealed class DatabaseNotFoundException : Exception
{
    public DatabaseNotFoundException(string message) : base(message)
    {
    }
}
