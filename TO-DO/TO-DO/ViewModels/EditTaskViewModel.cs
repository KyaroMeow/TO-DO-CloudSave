using CommunityToolkit.Mvvm.ComponentModel;
using TO_DO.Models;
using System;
using System.Collections.ObjectModel;
using TO_DO.Data;
using System.Linq;
using System.ComponentModel;

namespace TO_DO.ViewModels
{
    public partial class EditTaskViewModel : ObservableObject
    {
        
        private readonly DbRepository _repo = new("Data Source=TaskBase.db");
        private readonly TaskModel _task;
        private readonly Action? _reload;

        public ObservableCollection<TagModel> AllTags { get; set; } = new();

        public EditTaskViewModel(TaskModel task, Action? reloadTasks = null)
        {
            _task = task;
            _reload = reloadTasks;

            Title = task.Title;
            Description = task.Description;
            Deadline = task.Deadline;

            var tags = _repo.GetAllTags();
            foreach (var tag in tags)
            {
                tag.IsSelected = task.Tags.Any(t => t.Id == tag.Id);
                tag.PropertyChanged += OnTagChanged;
                AllTags.Add(tag);
            }
        }

        [ObservableProperty]
        private string? title;

        partial void OnTitleChanged(string? value)
        {
            _task.Title = value ?? "";
            _repo.UpdateName(_task.Id, _task.Title);
            _reload?.Invoke();

        }

        [ObservableProperty]
        private string? description;

        partial void OnDescriptionChanged(string? value)
        {
            _task.Description = value;
            _repo.UpdateDescription(_task.Id, value ?? "");
            _reload?.Invoke();

        }

        [ObservableProperty]
        private DateTime? deadline;

        partial void OnDeadlineChanged(DateTime? value)
        {
            _task.Deadline = value;
            _repo.UpdateDeadline(_task.Id, value);
            _reload?.Invoke();

        }

        private void OnTagChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not TagModel tag || e.PropertyName != nameof(TagModel.IsSelected))
                return;

            bool alreadyLinked = _task.Tags.Any(t => t.Id == tag.Id);

            if (tag.IsSelected && !alreadyLinked)
                _repo.AddTagToTask(_task.Id, tag.Id);
            else if (!tag.IsSelected && alreadyLinked)
                _repo.RemoveTagFromTask(_task.Id, tag.Id);

            _reload?.Invoke();

        }
    }
}