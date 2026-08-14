using System.Text;
using GdCli.GameData.Quests;

namespace GdCli.Tests.GameData.Quests;

public sealed class QuestReaderTests
{
    [Fact]
    public void ReadPreservesTasksOperationsAndLocalizedText()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write("QST2"u8);
            writer.Write(9);
            writer.Write(0u);
            writer.Write(100u);
            writer.Write(3u);
            writer.Write(1);
            writer.Write(200u);
            writer.Write(4u);
            writer.Write(0);
            writer.Write(1);
            writer.Write(300u);
            writer.Write(5u);
            writer.Write(0);
            writer.Write(1);
            writer.Write(1);
            writer.Write((byte)0);
            _writeString(writer, "Quests/Test/Next.qst");
            writer.Write(400u);
            writer.Write(0);
            writer.Write(false);
            writer.Write(true);
            writer.Write(1);
            _writeString(writer, "en");
            writer.Write(5);
            foreach (var value in new[] { "Region", "Quest", "Task", "Description", "Objective" })
                _writeString(writer, value);
        }
        stream.Position = 0;

        var quest = QuestReader.Read(stream, "Test/Quest.qst", "base");

        Assert.Equal("quests/test/quest.qst", quest.Path);
        Assert.Equal(100u, quest.Uid);
        Assert.Equal("Region", quest.Region);
        Assert.Equal("Quest", quest.Name);
        var task = Assert.Single(quest.Tasks);
        Assert.Equal("Task", task.Name);
        Assert.Equal("Description", task.Description);
        Assert.True(task.DontPropagate);
        var objective = Assert.Single(task.Objectives);
        Assert.Equal("Objective", objective.Name);
        var action = Assert.Single(objective.Actions);
        Assert.Equal("BeginQuestTask", action.Kind);
        Assert.Equal("quests/test/next.qst", action.QuestPath);
        Assert.Equal(400u, action.TaskUid);
    }

    private static void _writeString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write((uint)bytes.Length);
        writer.Write(bytes);
    }
}
