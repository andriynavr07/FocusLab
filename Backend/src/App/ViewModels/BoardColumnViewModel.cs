using System.Collections.ObjectModel;
using FocusLab.Application.Dto;
using TaskStatus = FocusLab.Domain.Enums.TaskStatus;

namespace FocusLab.App.ViewModels;

public class BoardColumnViewModel : ViewModelBase
{
    public BoardColumnViewModel(TaskStatus status, string title)
    {
        Status = status;
        Title = title;
    }

    public TaskStatus Status { get; }

    public string Title { get; }

    public ObservableCollection<TaskDto> Tasks { get; } = new();
}
