using System.Globalization;
using GdCli.Contracts;
using GdCli.Output;

namespace GdCli.Tests.Output;

public sealed class JsonOutputTests
{
    [Fact]
    public void SerializationFailureDoesNotWritePartialStdout()
    {
        var value = new CircularJsonValue();
        value.Value = value;
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        Assert.Throws<JsonSerializationOutputException>(() => JsonOutput.Write(writer, value));
        Assert.Equal(string.Empty, writer.ToString());
    }

    [Fact]
    public void QuestOperationsOmitUnusedNullableFields()
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        JsonOutput.Write(writer, new QuestOperationRecord
        {
            Kind = "BeginQuest",
            QuestPath = "quests/test.qst"
        });

        Assert.Equal(
            "{\"kind\":\"BeginQuest\",\"questPath\":\"quests/test.qst\"}" + Environment.NewLine,
            writer.ToString());
    }

    [Fact]
    public void AcquisitionMethodsOmitFieldsThatDoNotApply()
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        JsonOutput.Write(writer, new AcquisitionMethod { Kind = "unknown" });

        Assert.Equal(
            "{\"kind\":\"unknown\"}" + Environment.NewLine,
            writer.ToString());
    }
}
