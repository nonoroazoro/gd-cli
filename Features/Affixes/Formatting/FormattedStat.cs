using System.Globalization;

namespace GdCli.Features.Affixes.Formatting;

internal sealed class FormattedStat
{
    public string? Text { get; set; }

    public float? Param0 { get; set; }

    public float? Param1 { get; set; }

    public float? Param2 { get; set; }

    public string? Param3 { get; set; }

    public float? Param4 { get; set; }

    public string? Param5 { get; set; }

    public string? Param6 { get; set; }

    public StatSection Type { get; set; }

    public FormattedStat? Extra { get; set; }

    public override string ToString()
    {
        if (Text == null)
            return string.Empty;

        var result = _replaceNumeric(Text, "{0}", Param0);
        result = _replaceNumeric(result, "{1}", Param1);
        result = _replaceNumeric(result, "{2}", Param2);
        result = result.Replace("{3}", Param3 ?? string.Empty, StringComparison.Ordinal);
        result = _replaceNumeric(result, "{4}", Param4);
        result = result.Replace("{5}", Param5 ?? string.Empty, StringComparison.Ordinal);
        return result.Replace("{6}", Param6 ?? string.Empty, StringComparison.Ordinal);
    }

    private static string _replaceNumeric(string text, string placeholder, float? value)
    {
        if (value == null)
            return text;

        var index = text.IndexOf(placeholder, StringComparison.Ordinal);
        var isPercentage = index >= 0
            && index + placeholder.Length < text.Length
            && text[index + placeholder.Length] == '%';
        var formatted = isPercentage
            ? Math.Round(value.Value, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture)
            : value.Value.ToString(CultureInfo.InvariantCulture);
        return text.Replace(placeholder, formatted, StringComparison.Ordinal);
    }
}
