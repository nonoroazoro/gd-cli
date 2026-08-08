using System.Text.Json.Serialization;

namespace GdCli.Contracts;

internal sealed class ErrorResponse
{
    public string SchemaVersion { get; init; } = OutputSchema.Version;

    public required string Code { get; init; }

    public required string Error { get; init; }

    public required int ExitCode { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Argument { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? AllowedValues { get; init; }
}
