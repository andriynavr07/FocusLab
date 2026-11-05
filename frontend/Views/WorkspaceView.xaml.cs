using System.Windows.Controls;
using Frontend.ViewModels;

namespace Frontend.Views;

public partial class WorkspaceView : UserControl
{
    public WorkspaceView()
    {
        InitializeComponent();
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not WorkspaceViewModel viewModel
            || e.AddedItems.Count == 0
            || e.AddedItems[0] is not ListBoxItem { Tag: string section })
        {
            return;
        }

        var command = section switch
        {
            "home" => viewModel.ShowDashboardCommand,
            "tasks" => viewModel.ShowTasksCommand,
            "calendar" => viewModel.ShowCalendarCommand,
            "timer" => viewModel.ShowTimerCommand,
            "settings" => viewModel.ShowSettingsCommand,
            _ => null
        };

        if (command?.CanExecute(null) == true)
            command.Execute(null);
    }
}
