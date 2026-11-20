using FluentAssertions;
using Frontend.Services;
using Xunit;
using System.IO;
namespace FocusLab.Frontend.Tests;

public sealed class TaskAttachmentStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "focuslab-tests-" + Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_dir, "attachments.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private static TaskAttachment Link(int taskId, string name) => new()
    {
        TaskId = taskId,
        Kind = AttachmentKind.Link,
        Name = name,
        Target = "https://example.com/" + name
    };

    [Fact]
    public void GetFor_ReturnsOnlyThisTask()
    {
        var store = new TaskAttachmentStore(FilePath);
        store.Add(Link(1, "a"));
        store.Add(Link(2, "b"));

        store.GetFor(1).Select(a => a.Name).Should().Equal("a");
        store.GetFor(3).Should().BeEmpty();
    }

    [Fact]
    public void Save_ThenNewInstance_Persists()
    {
        new TaskAttachmentStore(FilePath).Add(Link(1, "a"));

        File.Exists(FilePath).Should().BeTrue();
        new TaskAttachmentStore(FilePath).GetFor(1).Should().ContainSingle(a => a.Name == "a");
    }

    [Fact]
    public void Remove_DeletesOnlyThatAttachment()
    {
        var store = new TaskAttachmentStore(FilePath);
        var first = Link(1, "a");
        store.Add(first);
        store.Add(Link(1, "b"));

        store.Remove(first.Id);

        store.GetFor(1).Select(a => a.Name).Should().Equal("b");
        new TaskAttachmentStore(FilePath).GetFor(1).Select(a => a.Name).Should().Equal("b");
    }

    [Fact]
    public void RemoveForTask_RemovesAll()
    {
        var store = new TaskAttachmentStore(FilePath);
        store.Add(Link(1, "a"));
        store.Add(Link(1, "b"));
        store.Add(Link(2, "c"));

        store.RemoveForTask(1);

        store.GetFor(1).Should().BeEmpty();
        store.GetFor(2).Should().ContainSingle();
    }

    [Fact]
    public void CorruptedFile_ReturnsEmpty()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(FilePath, "{ not json");

        new TaskAttachmentStore(FilePath).GetFor(1).Should().BeEmpty();
    }

    [Fact]
    public void StoreWithoutPath_KeepsAttachmentsInMemoryOnly()
    {
        var store = new TaskAttachmentStore();

        store.Add(Link(1, "a"));

        store.GetFor(1).Should().ContainSingle();
        Directory.Exists(_dir).Should().BeFalse();
    }
}
