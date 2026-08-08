using GdCli.Application;

namespace GdCli;

internal static class Program
{
    public static int Main(string[] args)
    {
        return new CliApplication(Console.Out, Console.Error).Run(args);
    }
}
