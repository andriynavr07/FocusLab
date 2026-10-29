using FocusLab.Application.Dto;
using FocusLab.Domain.Entities;
using FocusLab.Domain.Enums;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Application.Tests;

/// <summary>
/// У Domain зараз немає бізнес-логіки, лише POCO зі значеннями за замовчуванням.
/// Ці тести фіксують ці значення, щоб випадкова зміна не пройшла непоміченою.
/// </summary>
public class DomainDefaultsTests
{
    [Fact]
    public void TaskItem_HasExpectedDefaults()
    {
        var task = new TaskItem();

        Assert.Equal(string.Empty, task.Title);
        Assert.Equal(Priority.Medium, task.Priority);
        Assert.Equal(TaskStatus.Todo, task.Status);
        Assert.Null(task.CompletedAt);
        Assert.NotNull(task.TaskTags);
        Assert.NotNull(task.Resources);
        Assert.NotNull(task.FocusSessions);
        Assert.Empty(task.TaskTags);
        Assert.Empty(task.Resources);
        Assert.Empty(task.FocusSessions);
    }

    [Fact]
    public void Account_LocalAccountId_IsOne()
    {
        Assert.Equal(1, Account.LocalAccountId);
    }

    [Fact]
    public void TaskDto_HasExpectedDefaults()
    {
        var dto = new TaskDto();

        Assert.Equal(Account.LocalAccountId, dto.AccountId);
        Assert.Equal(string.Empty, dto.Title);
        Assert.Equal(Priority.Medium, dto.Priority);
        Assert.Equal(TaskStatus.Todo, dto.Status);
    }

    [Theory]
    [InlineData(TaskStatus.Todo, 0)]
    [InlineData(TaskStatus.InProgress, 1)]
    [InlineData(TaskStatus.Review, 2)]
    [InlineData(TaskStatus.Done, 3)]
    public void TaskStatus_NumericValues_AreStable(TaskStatus status, int expected)
    {
        // Значення можуть бути збережені в БД, тому їх змінювати не можна.
        Assert.Equal(expected, (int)status);
    }

    [Theory]
    [InlineData(Priority.Low, 0)]
    [InlineData(Priority.Medium, 1)]
    [InlineData(Priority.High, 2)]
    [InlineData(Priority.Critical, 3)]
    public void Priority_NumericValues_AreStable(Priority priority, int expected)
    {
        Assert.Equal(expected, (int)priority);
    }
}
