using FocusLab.Application.Abstractions;
using FocusLab.Application.Dto;
using FocusLab.Application.Services;
using FocusLab.Domain.Entities;
using FocusLab.Domain.Enums;
using Moq;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.Application.Tests;

public class TaskServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ITaskRepository> _repo = new();
    private readonly FakeClock _clock = new(Now);
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(_repo.Object, _clock);
    }

    private static TaskItem MakeEntity(
        int id = 1,
        string title = "Стара назва",
        TaskStatus status = TaskStatus.Todo,
        DateTime? completedAt = null) => new()
    {
        Id = id,
        AccountId = Account.LocalAccountId,
        Title = title,
        Description = "Старий опис",
        Deadline = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
        Priority = Priority.Low,
        Status = status,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CompletedAt = completedAt
    };

    // ---------------------------------------------------------------- GetAllAsync

    [Fact]
    public async Task GetAllAsync_WhenRepositoryEmpty_ReturnsEmptyList()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<TaskItem>());

        var result = await _sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_MapsAllEntityFieldsToDto()
    {
        var entity = MakeEntity(id: 7, title: "Задача", status: TaskStatus.Done,
                                completedAt: Now);
        entity.Priority = Priority.Critical;
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<TaskItem> { entity });

        var result = await _sut.GetAllAsync();

        var dto = Assert.Single(result);
        Assert.Equal(7, dto.Id);
        Assert.Equal(entity.AccountId, dto.AccountId);
        Assert.Equal("Задача", dto.Title);
        Assert.Equal(entity.Description, dto.Description);
        Assert.Equal(entity.Deadline, dto.Deadline);
        Assert.Equal(Priority.Critical, dto.Priority);
        Assert.Equal(TaskStatus.Done, dto.Status);
        Assert.Equal(entity.CreatedAt, dto.CreatedAt);
        Assert.Equal(Now, dto.CompletedAt);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllItemsInOrder()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<TaskItem>
             {
                 MakeEntity(1, "A"), MakeEntity(2, "B"), MakeEntity(3, "C")
             });

        var result = await _sut.GetAllAsync();

        Assert.Equal(new[] { 1, 2, 3 }, result.Select(x => x.Id));
        Assert.Equal(new[] { "A", "B", "C" }, result.Select(x => x.Title));
    }

    [Fact]
    public async Task GetAllAsync_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetAllAsync(cts.Token)).ReturnsAsync(new List<TaskItem>());

        await _sut.GetAllAsync(cts.Token);

        _repo.Verify(r => r.GetAllAsync(cts.Token), Times.Once);
    }

    // ---------------------------------------------------------------- CreateAsync

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.CreateAsync(null!));

        _repo.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_AddsEntityToRepositoryOnce()
    {
        var dto = new TaskDto { Title = "Нова задача" };

        await _sut.CreateAsync(dto);

        _repo.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CopiesFieldsFromDtoToEntity()
    {
        TaskItem? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
             .Callback<TaskItem, CancellationToken>((t, _) => saved = t)
             .Returns(Task.CompletedTask);

        var deadline = new DateTime(2026, 11, 15, 9, 0, 0, DateTimeKind.Utc);
        var dto = new TaskDto
        {
            AccountId = 5,
            Title = "Підготувати звіт",
            Description = "Квартальний",
            Deadline = deadline,
            Priority = Priority.High
        };

        await _sut.CreateAsync(dto);

        Assert.NotNull(saved);
        Assert.Equal(5, saved!.AccountId);
        Assert.Equal("Підготувати звіт", saved.Title);
        Assert.Equal("Квартальний", saved.Description);
        Assert.Equal(deadline, saved.Deadline);
        Assert.Equal(Priority.High, saved.Priority);
    }

    [Theory]
    [InlineData(TaskStatus.InProgress)]
    [InlineData(TaskStatus.Review)]
    [InlineData(TaskStatus.Done)]
    public async Task CreateAsync_AlwaysStartsWithTodoStatus_RegardlessOfDtoStatus(TaskStatus dtoStatus)
    {
        var dto = new TaskDto { Title = "Задача", Status = dtoStatus };

        var result = await _sut.CreateAsync(dto);

        Assert.Equal(TaskStatus.Todo, result.Status);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAtFromClock_NotFromDto()
    {
        var dto = new TaskDto
        {
            Title = "Задача",
            CreatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = await _sut.CreateAsync(dto);

        Assert.Equal(Now, result.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_UsesCurrentClockValue()
    {
        _clock.UtcNow = Now.AddDays(3);

        var result = await _sut.CreateAsync(new TaskDto { Title = "Задача" });

        Assert.Equal(Now.AddDays(3), result.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_DoesNotSetCompletedAt()
    {
        var dto = new TaskDto { Title = "Задача", CompletedAt = Now };

        var result = await _sut.CreateAsync(dto);

        Assert.Null(result.CompletedAt);
    }

    [Fact]
    public async Task CreateAsync_TrimsTitle()
    {
        var result = await _sut.CreateAsync(new TaskDto { Title = "   Назва з пробілами  " });

        Assert.Equal("Назва з пробілами", result.Title);
    }

    [Fact]
    public async Task CreateAsync_ReturnsIdAssignedByRepository()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
             .Callback<TaskItem, CancellationToken>((t, _) => t.Id = 42)
             .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new TaskDto { Title = "Задача" });

        Assert.Equal(42, result.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task CreateAsync_EmptyOrWhitespaceTitle_ThrowsAndDoesNotSave(string? title)
    {
        var dto = new TaskDto { Title = title! };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(dto));

        _repo.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_TitleOfMaxLength_IsAccepted()
    {
        var title = new string('a', TaskService.MaxTitleLength);

        var result = await _sut.CreateAsync(new TaskDto { Title = title });

        Assert.Equal(TaskService.MaxTitleLength, result.Title.Length);
    }

    [Fact]
    public async Task CreateAsync_TitleLongerThanMax_ThrowsAndDoesNotSave()
    {
        var title = new string('a', TaskService.MaxTitleLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateAsync(new TaskDto { Title = title }));

        _repo.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_TitleExceedsMaxOnlyBecauseOfSpaces_IsAcceptedAfterTrim()
    {
        // Довжина перевіряється ПІСЛЯ Trim(), тож зайві пробіли по краях не рахуються.
        var title = "  " + new string('a', TaskService.MaxTitleLength) + "  ";

        var result = await _sut.CreateAsync(new TaskDto { Title = title });

        Assert.Equal(TaskService.MaxTitleLength, result.Title.Length);
    }

    [Fact]
    public async Task CreateAsync_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();

        await _sut.CreateAsync(new TaskDto { Title = "Задача" }, cts.Token);

        _repo.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), cts.Token), Times.Once);
    }

    // ---------------------------------------------------------------- UpdateAsync

    [Fact]
    public async Task UpdateAsync_NullDto_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.UpdateAsync(null!));

        _repo.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_TaskNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
             .ReturnsAsync((TaskItem?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UpdateAsync(new TaskDto { Id = 99, Title = "Задача" }));

        Assert.Contains("99", ex.Message);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEditableFieldsAndSavesOnce()
    {
        var entity = MakeEntity(id: 1);
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var newDeadline = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dto = new TaskDto
        {
            Id = 1,
            Title = "  Нова назва  ",
            Description = "Новий опис",
            Deadline = newDeadline,
            Priority = Priority.Critical,
            Status = TaskStatus.Todo
        };

        var result = await _sut.UpdateAsync(dto);

        Assert.Equal("Нова назва", result.Title);
        Assert.Equal("Новий опис", result.Description);
        Assert.Equal(newDeadline, result.Deadline);
        Assert.Equal(Priority.Critical, result.Priority);
        _repo.Verify(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotChangeCreatedAtOrAccountId()
    {
        var entity = MakeEntity(id: 1);
        var originalCreatedAt = entity.CreatedAt;
        var originalAccountId = entity.AccountId;
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var dto = new TaskDto
        {
            Id = 1,
            Title = "Назва",
            AccountId = 999,
            CreatedAt = new DateTime(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = await _sut.UpdateAsync(dto);

        Assert.Equal(originalCreatedAt, result.CreatedAt);
        Assert.Equal(originalAccountId, result.AccountId);
    }

    [Fact]
    public async Task UpdateAsync_StatusChangedToDone_SetsCompletedAtFromClock()
    {
        var entity = MakeEntity(status: TaskStatus.InProgress);
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _sut.UpdateAsync(new TaskDto { Id = 1, Title = "Назва", Status = TaskStatus.Done });

        Assert.Equal(TaskStatus.Done, result.Status);
        Assert.Equal(Now, result.CompletedAt);
    }

    [Theory]
    [InlineData(TaskStatus.Todo)]
    [InlineData(TaskStatus.InProgress)]
    [InlineData(TaskStatus.Review)]
    public async Task UpdateAsync_StatusChangedFromDoneToOther_ClearsCompletedAt(TaskStatus newStatus)
    {
        var entity = MakeEntity(status: TaskStatus.Done, completedAt: Now.AddDays(-1));
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _sut.UpdateAsync(new TaskDto { Id = 1, Title = "Назва", Status = newStatus });

        Assert.Equal(newStatus, result.Status);
        Assert.Null(result.CompletedAt);
    }

    [Fact]
    public async Task UpdateAsync_StatusUnchangedAndDone_KeepsOriginalCompletedAt()
    {
        var originalCompletedAt = Now.AddDays(-5);
        var entity = MakeEntity(status: TaskStatus.Done, completedAt: originalCompletedAt);
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _sut.UpdateAsync(new TaskDto { Id = 1, Title = "Нова назва", Status = TaskStatus.Done });

        Assert.Equal(originalCompletedAt, result.CompletedAt);
    }

    [Fact]
    public async Task UpdateAsync_StatusChangedBetweenNonDoneStatuses_CompletedAtStaysNull()
    {
        var entity = MakeEntity(status: TaskStatus.Todo);
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _sut.UpdateAsync(new TaskDto { Id = 1, Title = "Назва", Status = TaskStatus.Review });

        Assert.Equal(TaskStatus.Review, result.Status);
        Assert.Null(result.CompletedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_EmptyTitle_ThrowsAndDoesNotSave(string? title)
    {
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeEntity());

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.UpdateAsync(new TaskDto { Id = 1, Title = title! }));

        _repo.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_TitleLongerThanMax_ThrowsAndDoesNotSave()
    {
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(MakeEntity());
        var title = new string('a', TaskService.MaxTitleLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.UpdateAsync(new TaskDto { Id = 1, Title = title }));

        _repo.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        var entity = MakeEntity();
        _repo.Setup(r => r.GetByIdAsync(1, cts.Token)).ReturnsAsync(entity);

        await _sut.UpdateAsync(new TaskDto { Id = 1, Title = "Назва" }, cts.Token);

        _repo.Verify(r => r.GetByIdAsync(1, cts.Token), Times.Once);
        _repo.Verify(r => r.UpdateAsync(entity, cts.Token), Times.Once);
    }

    // ---------------------------------------------------------------- DeleteAsync

    [Fact]
    public async Task DeleteAsync_DelegatesToRepositoryWithSameId()
    {
        await _sut.DeleteAsync(15);

        _repo.Verify(r => r.DeleteAsync(15, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();

        await _sut.DeleteAsync(15, cts.Token);

        _repo.Verify(r => r.DeleteAsync(15, cts.Token), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryThrows_PropagatesException()
    {
        _repo.Setup(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidOperationException("db error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DeleteAsync(1));
    }
}
