namespace GdCli;

internal sealed class IncompatibleDatabaseException : Exception
{
    public IncompatibleDatabaseException(string message) : base(message)
    {
    }
}
