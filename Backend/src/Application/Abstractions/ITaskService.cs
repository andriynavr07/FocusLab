using FocusLab.Application.Dto;

namespace FocusLab.Application.Abstractions;

public interface ITaskService
{
    Task<IReadOnlyList<TaskDto>> GetAllAsync(CancellationToken ct = default);
    Task<TaskDto> CreateAsync(TaskDto task, CancellationToken ct = default);
    Task<TaskDto> UpdateAsync(TaskDto task, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
