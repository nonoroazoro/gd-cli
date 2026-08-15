namespace GdCli.Database;

internal sealed record ItemFamilyFilter(
    bool? HasMiRecord,
    string? Availability = null,
    bool IncludeUnavailable = false);
