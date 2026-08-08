namespace GdCli.Features.Affixes.Engine;

internal static class StatJitter
{
    public static double ApplyIntegerRoll(double value, double jitterPercent, IRollSource rollSource)
    {
        if (value == 0.0 || jitterPercent == 0.0)
            return value;

        var spread = (int)(value * jitterPercent * 0.01);
        if (spread == 0)
            spread = 1;

        var roll = rollSource.NextRange(2 * spread);
        var rolledValue = roll - spread + value;
        return Math.Abs(rolledValue) < 1.0 ? value : rolledValue;
    }

    public static double ApplySkillRoll(double value, double jitterPercent, IRollSource rollSource)
    {
        if (value == 0.0)
            return value;

        var spread = (int)(value * jitterPercent * 0.01);
        if (spread == 0)
        {
            rollSource.Consume();
            return value;
        }

        var roll = rollSource.NextRange(2 * spread);
        var rolledValue = roll - spread + value;
        return Math.Abs(rolledValue) < 1.0 ? value : rolledValue;
    }

    public static double ApplyScale(double rolledValue, double scalePercent)
    {
        var numerator = (float)((float)rolledValue * (float)(100.0 + scalePercent));
        return (int)(numerator / 100.0f);
    }

    public static double ApplyConversionRoll(double value, double jitterPercent, IRollSource rollSource)
    {
        if (jitterPercent <= 0.0)
            return value;

        var jitter = jitterPercent * 0.01;
        var unit = rollSource.NextUnit();
        var factor = (float)(unit * (2.0 * jitter) + (1.0 - jitter));
        return Math.Clamp(value * factor, 0.0, 100.0);
    }
}
