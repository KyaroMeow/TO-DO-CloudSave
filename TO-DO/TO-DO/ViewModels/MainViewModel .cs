using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.Windows;
using TO_DO.Data;
using TO_DO.Enums;
using TO_DO.Models;
using TO_DO.Services;
using BaseTheme = MaterialDesignThemes.Wpf.BaseTheme;
using MaterialThemeHelper = MaterialDesignThemes.Wpf.PaletteHelper;
using Application = System.Windows.Application;

namespace TO_DO.ViewModels
{
	public partial class MainViewModel : ObservableObject
	{
		public MainViewModel()
		{
			ApplyTheme();
			cloudDataManager.SyncFromCloudAsync();
			LoadTasks();
		}
		private readonly DbRepository _repo = new("Data Source=TaskBase.db;Pooling=True;Cache=Shared");
        private readonly CloudDataManager cloudDataManager = new("Data Source=TaskBase.db", "to-do-1ad85", App.AuthService.LoadUserId());


        [ObservableProperty]
		private string? newTaskTitle;
		[ObservableProperty]
		private string? searchQuery;
		[ObservableProperty]
		private TaskFilter selectedFilter = TaskFilter.All;
		public IEnumerable<TaskModel> ActiveTasks => Tasks.Where(t => !t.IsCompleted);
		public IEnumerable<TaskModel> CompletedTasks => Tasks.Where(t => t.IsCompleted);


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

			var vm = new EditTaskViewModel(task, LoadTasks);
			var window = new Views.EditTaskWindow(task, vm)
			{
				Owner = Application.Current.MainWindow
			};

			window.ShowDialog(); // изменения уже реактивны
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

		public void MoveTask(TaskModel from, TaskModel to)
		{
			int oldIndex = Tasks.IndexOf(from);
			int newIndex = Tasks.IndexOf(to);

			if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
			{
				Tasks.Move(oldIndex, newIndex);
				OnPropertyChanged(nameof(FilteredTasks));
			}
			RefreshTasks();
		}
		[RelayCommand]
		private void ToggleTheme()
		{
			var paletteHelper = new MaterialThemeHelper();
			var theme = paletteHelper.GetTheme();

			if (theme.GetBaseTheme() == BaseTheme.Dark)
			{
				theme.SetBaseTheme(BaseTheme.Light);

				Properties.Settings.Default["IsDarkTheme"] = false;
			}
			else
			{
				theme.SetBaseTheme(BaseTheme.Dark);
				Properties.Settings.Default["IsDarkTheme"] = true;
			}

			paletteHelper.SetTheme(theme);
			Properties.Settings.Default.Save();
		}



		[RelayCommand]
		private void Logout()
		{
			var auth = new AuthService("to-do-1ad85");
			auth.Logout();
			Application.Current.Shutdown(); // или показать LoginWindow
		}
		private void ApplyTheme()
		{
			var paletteHelper = new MaterialThemeHelper();
			var theme = paletteHelper.GetTheme();

			var isDark = (bool)(Properties.Settings.Default["IsDarkTheme"] ?? false);
			theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);

			paletteHelper.SetTheme(theme);
		}

		private void LoadCloudTasks(string userId)
		{

		}

		public void LoadTasks()
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
			RefreshTasks();
		}
		private void RefreshTasks()
		{
			OnPropertyChanged(nameof(ActiveTasks));
			OnPropertyChanged(nameof(CompletedTasks));
		}


	}
}
