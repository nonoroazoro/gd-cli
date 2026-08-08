namespace GdCli.Commands;

internal sealed class CommandLineException : Exception
{
    public CommandLineException(string message) : base(message)
    {
    }
}
