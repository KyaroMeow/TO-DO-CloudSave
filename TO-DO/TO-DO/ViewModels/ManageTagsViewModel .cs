using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TO_DO.Data;
using TO_DO.Models;

namespace TO_DO.ViewModels
{
	public partial class ManageTagsViewModel : ObservableObject
	{
		private readonly DbRepository _repo = new("Data Source=TaskBase.db");

		public ObservableCollection<TagModel> Tags { get; } = new();

		[ObservableProperty]
		private string? newTagName;

		public ManageTagsViewModel()
		{
			LoadTags();
		}

		private void LoadTags()
		{
			Tags.Clear();
			foreach (var tag in _repo.GetAllTags())
				Tags.Add(tag);
		}

		[RelayCommand]
		private void AddTag()
		{
			if (!string.IsNullOrWhiteSpace(NewTagName))
			{
				_repo.AddTag(NewTagName.Trim());
				NewTagName = string.Empty;
				LoadTags();
			}
		}

		[RelayCommand]
		private void DeleteTag(TagModel? tag)
		{
			if (tag is not null)
			{
				_repo.RemoveTagById(tag.Id);
				LoadTags();
			}
		}
	}
}
