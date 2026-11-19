using FocusLab.Application.Dto;

namespace FocusLab.Application.Abstractions;

public interface ICatalogService
{
    Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken ct = default);
    Task<ProjectDto> SaveProjectAsync(ProjectDto project, CancellationToken ct = default);
    Task DeleteProjectAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken ct = default);
    Task<TagDto> SaveTagAsync(TagDto tag, CancellationToken ct = default);
    Task DeleteTagAsync(int id, CancellationToken ct = default);
}
