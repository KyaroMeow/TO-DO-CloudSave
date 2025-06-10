using System.Windows;

namespace TO_DO.Views
{
	public partial class EditTaskWindow : Window
	{
		public EditTaskWindow(object viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
	}
}
