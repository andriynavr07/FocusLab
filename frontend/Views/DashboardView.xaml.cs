using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Frontend.ViewModels;

namespace Frontend.Views;

public partial class DashboardView : UserControl
{
    private const string DragFormat = "FocusLab.DashboardTask";

    private DashboardTask? _pressedCard;
    private Point _dragStart;
    private bool _dragStarted;

    public DashboardView() => InitializeComponent();

    private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

    // ───── Картка: клік відкриває деталі, рух із натиснутою кнопкою — починає перетягування ─────

    private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DashboardTask card })
        {
            _pressedCard = card;
            _dragStart = e.GetPosition(null);
            _dragStarted = false;
        }
    }

    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _pressedCard is null || _dragStarted)
            return;

        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _dragStarted = true;
        var card = _pressedCard;
        _pressedCard = null;

        try
        {
            DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(DragFormat, card), DragDropEffects.Move);
        }
        finally
        {
            ViewModel?.ClearDragHighlight();
        }
    }

    private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var card = _pressedCard;
        _pressedCard = null;

        // Якщо перетягування не починалося — це звичайний клік.
        if (!_dragStarted && card is not null && sender is FrameworkElement { DataContext: DashboardTask clicked }
            && ReferenceEquals(card, clicked) && card.OpenCommand.CanExecute(null))
        {
            card.OpenCommand.Execute(null);
        }
    }

    // ───── Колонка: приймає картки ─────

    private void Column_DragOver(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DashboardColumn column } && e.Data.GetDataPresent(DragFormat))
        {
            column.IsDragOver = true;
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void Column_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DashboardColumn column })
            column.IsDragOver = false;
    }

    private void Column_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (sender is not FrameworkElement { DataContext: DashboardColumn column }
            || e.Data.GetData(DragFormat) is not DashboardTask card
            || ViewModel is not { } viewModel)
            return;

        column.IsDragOver = false;

        // Колонки перебудовуються після зміни статусу — робимо це вже після завершення Drop.
        Dispatcher.InvokeAsync(() => viewModel.MoveTask(card, column));
    }
}
