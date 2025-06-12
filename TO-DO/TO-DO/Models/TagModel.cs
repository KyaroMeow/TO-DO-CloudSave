using CommunityToolkit.Mvvm.ComponentModel;

namespace TO_DO.Models
{
	public partial class TagModel : ObservableObject
    {
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;

        //public bool isSelected { get; set; }

        [ObservableProperty]
        private bool isSelected;
        //AIzaSyAwbCrOw7rwDUOU9VVl0JvO694MoI6YSIo
        // to-do-1ad85
    }
}
