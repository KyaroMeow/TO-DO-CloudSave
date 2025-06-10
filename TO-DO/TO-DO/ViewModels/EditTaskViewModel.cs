using CommunityToolkit.Mvvm.ComponentModel;
using TO_DO.Models;
using System;
using System.Collections.ObjectModel;
using TO_DO.Data;

namespace TO_DO.ViewModels
{
	public partial class EditTaskViewModel : ObservableObject
	{
		[ObservableProperty]
		private string? title;

		[ObservableProperty]
		private string? description;

		[ObservableProperty]
		private DateTime? deadline;
		public ObservableCollection<TagModel> AllTags { get; set; } = new();
		public ObservableCollection<int> SelectedTagIds { get; set; } = new();



		public EditTaskViewModel(TaskModel task)
		{
			Title = task.Title;
			Description = task.Description;
			Deadline = task.Deadline;

			// Загружаем все теги из репозитория
			var repo = new DbRepository("Data Source=TaskBase.db");
			var tags = repo.GetAllTags(); // метод напишем ниже
			foreach (var tag in tags)
				AllTags.Add(tag);

			// Устанавливаем выбранные теги
			foreach (var tag in task.Tags)
				SelectedTagIds.Add(tag.Id);
		}

		public void ApplyTags(TaskModel task)
		{
			var repo = new DbRepository("Data Source=TaskBase.db");

			// Получаем текущие ID в базе
			var currentTagIds = task.Tags.Select(t => t.Id).ToList();

			// Новые выбранные теги
			foreach (var tagId in SelectedTagIds)
			{
				if (!currentTagIds.Contains(tagId))
					repo.AddTagToTask(task.Id, tagId);
			}

			// Удалённые теги
			foreach (var tagId in currentTagIds)
			{
				if (!SelectedTagIds.Contains(tagId))
					repo.RemoveTagFromTask(task.Id, tagId);
			}
		}

		public void ApplyChanges(TaskModel task)
		{
			task.Title = Title ?? "";
			task.Description = Description;
			task.Deadline = Deadline;
		}
	}
}
