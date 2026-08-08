using System.Globalization;
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
}
