using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TO_DO.Models;
using TO_DO.ViewModels;

namespace TO_DO.Views
{
	public partial class MainView : UserControl
	{
		private Point _dragStartPoint;

		public MainView()
		{
			InitializeComponent();
		}

		private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_dragStartPoint = e.GetPosition(null);
		}

		private void ListBox_MouseMove(object sender, MouseEventArgs e)
		{
			Point pos = e.GetPosition(null);
			Vector diff = _dragStartPoint - pos;

			if (e.LeftButton == MouseButtonState.Pressed &&
				(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
				 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
			{
				if (sender is ListBox listBox && listBox.SelectedItem is TaskModel task)
				{
					DragDrop.DoDragDrop(listBox, task, DragDropEffects.Move);
				}
			}
		}

		private void ActiveListBox_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(typeof(TaskModel)))
			{
				var task = (TaskModel)e.Data.GetData(typeof(TaskModel));
				if (task.IsCompleted)
				{
					var vm = DataContext as MainViewModel;
					vm?.ToggleTaskCompletionCommand.Execute(task);
				}
			}
		}

		private void CompletedListBox_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(typeof(TaskModel)))
			{
				var task = (TaskModel)e.Data.GetData(typeof(TaskModel));
				if (!task.IsCompleted)
				{
					var vm = DataContext as MainViewModel;
					vm?.ToggleTaskCompletionCommand.Execute(task);
				}
			}
		}

		private void ListBox_DragOver(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(typeof(TaskModel)))
			{
				e.Effects = DragDropEffects.None;
			}
			e.Handled = true;
		}

		private TaskModel? GetTaskUnderMouse(ListBox listBox, Point position)
		{
			var element = listBox.InputHitTest(position) as DependencyObject;
			while (element != null && !(element is ListBoxItem))
				element = VisualTreeHelper.GetParent(element);

			return element is ListBoxItem item ? item.DataContext as TaskModel : null;
		}
	}
}
