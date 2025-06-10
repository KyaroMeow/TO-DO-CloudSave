using System.Windows;
using TO_DO.ViewModels;

namespace TO_DO.Views
{
	public partial class ManageTagsWindow : Window
	{
		public ManageTagsWindow()
		{
			InitializeComponent();
			DataContext = new ManageTagsViewModel();
		}
	}
}
