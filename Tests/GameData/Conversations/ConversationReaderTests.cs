using System.Text;
using GdCli.GameData.Conversations;

namespace GdCli.Tests.GameData.Conversations;

public sealed class ConversationReaderTests
{
    [Fact]
    public void ReadPreservesBranchesAndQuestActions()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write("CNV "u8);
            writer.Write(6);
            writer.Write(0u);
            writer.Write(0u);
            _writeStep(writer, 2, 0, []);
            _writeStep(writer, 0, 3, ["quests/test/quest.qst"]);
            _writeStep(writer, 0, 7, [], 42);
            writer.Write(0);
        }
        stream.Position = 0;

        var conversation = ConversationReader.Read(stream, "test/conversation.cnv");

        Assert.Equal("conversations/test/conversation.cnv", conversation.Path);
        var steps = ConversationReader.Traverse(conversation.Root).ToArray();
        Assert.Equal(3, steps.Length);
        Assert.Equal("node", steps[0].Type);
        Assert.Equal("accept", steps[1].Type);
        var action = Assert.Single(steps[1].Actions);
        Assert.Equal("BeginQuest", action.Kind);
        Assert.Equal("quests/test/quest.qst", action.QuestPath);
        Assert.Equal("link", steps[2].Type);
        Assert.Equal(42, steps[2].LinkId);
    }

    private static void _writeStep(
        BinaryWriter writer,
        int childCount,
        int type,
        IReadOnlyList<string> begunQuests,
        int? linkId = null)
    {
        writer.Write(childCount);
        writer.Write(type);
        writer.Write(0u);
        if (type == 7)
            writer.Write(linkId ?? 0);
        _writeString(writer, string.Empty);
        writer.Write(0);
        writer.Write(begunQuests.Count);
        foreach (var quest in begunQuests)
        {
            writer.Write(0);
            writer.Write((byte)0);
            _writeString(writer, quest);
        }
    }

    private static void _writeString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write((uint)bytes.Length);
        writer.Write(bytes);
    }
}
