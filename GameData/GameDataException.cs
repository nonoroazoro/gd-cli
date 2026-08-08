namespace GdCli.GameData;

internal sealed class GameDataException : Exception
{
    public GameDataException(string message)
        : base(message)
    {
    }

    public GameDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
