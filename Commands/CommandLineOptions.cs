namespace GdCli.Commands;

internal sealed class CommandLineOptions
{
    public string Command { get; set; } = string.Empty;

    public bool HelpRequested { get; set; }

    public IReadOnlyList<string> CommandPath { get; set; } = [];

    public string? OutputQuery { get; set; }

    public bool All { get; set; }

    public string? Rarity { get; set; }

    public bool RaritySpecified { get; set; }

    public string? ItemClass { get; set; }

    public bool ItemClassSpecified { get; set; }

    public string? Kind { get; set; }

    public bool KindSpecified { get; set; }

    public string? AscendedCategory { get; set; }

    public bool AscendedCategorySpecified { get; set; }

    public bool? IsMi { get; set; }

    public bool MiSpecified { get; set; }

    public int? MinimumLevel { get; set; }

    public int? MaximumLevel { get; set; }

    public int Offset { get; set; }

    public bool OffsetSpecified { get; set; }

    public int Limit { get; set; } = 25;

    public bool LimitSpecified { get; set; }

    public string? RecordId { get; set; }

    public string? SearchQuery { get; set; }

    public string? GameDirectory { get; set; }

    public string GameLanguage { get; set; } = "zh";

    public bool GameLanguageSpecified { get; set; }

    public string? DropQuery { get; set; }

    public bool NoStats { get; set; }
}
