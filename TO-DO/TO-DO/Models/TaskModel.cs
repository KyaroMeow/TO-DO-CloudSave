using System;

namespace TO_DO.Models
{
	public class TaskModel
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string? Description { get; set; }
		public bool IsCompleted { get; set; }
		public DateTime? Deadline { get; set; }

		public List<TagModel> Tags { get; set; } = new();
	}
}
