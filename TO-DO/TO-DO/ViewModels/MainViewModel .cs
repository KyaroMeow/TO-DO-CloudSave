using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TO_DO.Models;
using System.Windows;
using Microsoft.Data.Sqlite;
using TO_DO.Data;
using TO_DO.Enums;

namespace TO_DO.ViewModels
{
	public partial class MainViewModel : ObservableObject
	{
		public MainViewModel()
		{
			LoadTasks();
		}
		private readonly DbRepository _repo = new("Data Source=TaskBase.db;Pooling=True;Cache=Shared");

		[ObservableProperty]
		private string? newTaskTitle;
		[ObservableProperty]
		private string? searchQuery;
		[ObservableProperty]
		private TaskFilter selectedFilter = TaskFilter.All;


		public IEnumerable<TaskModel> FilteredTasks =>
		Tasks
		.Where(t =>
			string.IsNullOrWhiteSpace(SearchQuery) ||
			t.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
		.Where(t =>
			SelectedFilter == TaskFilter.All ||
			(SelectedFilter == TaskFilter.Active && !t.IsCompleted) ||
			(SelectedFilter == TaskFilter.Completed && t.IsCompleted));

		partial void OnSearchQueryChanged(string? value)
		{
			OnPropertyChanged(nameof(FilteredTasks));
		}
		partial void OnSelectedFilterChanged(TaskFilter value)
		{
			OnPropertyChanged(nameof(FilteredTasks));
		}


		public ObservableCollection<TaskModel> Tasks { get; } = new();

		[RelayCommand]
		private void AddTask()
		{
			if (!string.IsNullOrWhiteSpace(NewTaskTitle))
			{
				_repo.CreateTask(NewTaskTitle);
				LoadTasks();
				NewTaskTitle = string.Empty;
			}
		}

		[RelayCommand]
		private void ToggleTaskCompletion(TaskModel? task)
		{
			if (task is not null)
			{
				_repo.ToggleIsDone(task.Id);
				LoadTasks();
			}
		}


		[RelayCommand]
		private void RemoveTask(TaskModel? task)
		{
			if (task is not null)
			{
				_repo.DeleteTask(task.Id);
				LoadTasks();
			}
		}

		[RelayCommand]
		private void EditTask(TaskModel? task)
		{
			if (task is null)
				return;

			var vm = new EditTaskViewModel(task);
			var window = new Views.EditTaskWindow(vm)
			{
				Owner = Application.Current.MainWindow
			};

			if (window.ShowDialog() == true)
			{
				vm.ApplyChanges(task);
				vm.ApplyTags(task); // 🎯 применяем выбранные теги

				_repo.UpdateName(task.Id, task.Title);
				_repo.UpdateDescription(task.Id, task.Description ?? "");
				_repo.UpdateDeadline(task.Id, task.Deadline);

				LoadTasks(); // обновляем
			}

		}
		[RelayCommand]
		private void OpenTagsManager()
		{
			var window = new Views.ManageTagsWindow
			{
				Owner = Application.Current.MainWindow
			};
			window.ShowDialog();

			// после закрытия — можно перезагрузить задачи, чтобы обновить теги
			LoadTasks();
		}

		private void LoadTasks()
		{
			Tasks.Clear();

			using var connection = new SqliteConnection("Data Source=TaskBase.db");
			connection.Open();

			var cmd = new SqliteCommand("SELECT t.id, t.Name, t.Description, t.IsDone, t.DeadLine FROM Tasks t", connection);
			using var reader = cmd.ExecuteReader();

			while (reader.Read())
			{
				var task = new TaskModel
				{
					Id = reader.GetInt32(0),
					Title = reader.GetString(1),
					Description = reader.IsDBNull(2) ? null : reader.GetString(2),
					IsCompleted = reader.GetInt32(3) == 1,
					Deadline = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
					Tags = new List<TagModel>()
				};

				Tasks.Add(task);
			}

			// Загружаем теги
			foreach (var task in Tasks)
			{
				var tagCmd = new SqliteCommand(@"
            SELECT Tags.id, Tags.Name
            FROM Tags
            JOIN TasksTag ON Tags.id = TasksTag.TagId
            WHERE TasksTag.TaskId = @TaskId", connection);

				tagCmd.Parameters.AddWithValue("@TaskId", task.Id);
				using var tagReader = tagCmd.ExecuteReader();

				while (tagReader.Read())
				{
					task.Tags.Add(new TagModel
					{
						Id = tagReader.GetInt32(0),
						Name = tagReader.GetString(1)
					});
				}
			}
		}


	}
}
