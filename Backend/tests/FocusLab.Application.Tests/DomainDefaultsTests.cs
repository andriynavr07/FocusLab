using FluentAssertions;
using FocusLab.Domain.Entities;
using FocusLab.Domain.Enums;
using Xunit;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Application.Tests;

public class DomainDefaultsTests
{
    [Fact]
    public void TaskItem_New_HasTodoMediumAndEmptyChecklist()
    {
        var task = new TaskItem();

        task.Status.Should().Be(TaskStatus.Todo);
        task.Priority.Should().Be(Priority.Medium);
        task.ChecklistJson.Should().Be("[]");
    }

    [Theory]
    [InlineData(Priority.Low, 0, "Low")]
    [InlineData(Priority.Medium, 1, "Medium")]
    [InlineData(Priority.High, 2, "High")]
    [InlineData(Priority.Critical, 3, "Critical")]
    public void Priority_PersistedValues_AreStable(Priority value, int number, string name)
    {
        ((int)value).Should().Be(number);
        value.ToString().Should().Be(name); // у БД зберігається рядок
    }

    [Theory]
    [InlineData(TaskStatus.Todo, 0, "Todo")]
    [InlineData(TaskStatus.InProgress, 1, "InProgress")]
    [InlineData(TaskStatus.Review, 2, "Review")]
    [InlineData(TaskStatus.Done, 3, "Done")]
    [InlineData(TaskStatus.Deferred, 4, "Deferred")]
    public void TaskStatus_PersistedValues_AreStable(TaskStatus value, int number, string name)
    {
        ((int)value).Should().Be(number);
        value.ToString().Should().Be(name);
    }
}
