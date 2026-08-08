namespace GdCli.Commands;

internal sealed class InvalidFilterValueException : Exception
{
    public InvalidFilterValueException(string argument, string value, IReadOnlyList<string> allowedValues)
        : base($"Invalid value for {argument}: {value}")
    {
        Argument = argument;
        Value = value;
        AllowedValues = allowedValues;
    }

    public string Argument { get; }

    public string Value { get; }

    public IReadOnlyList<string> AllowedValues { get; }
}
