using System.Windows;
using TO_DO.Models;
using TO_DO.ViewModels;

namespace TO_DO.Views
{
    public partial class EditTaskWindow : Window
    {
        public EditTaskWindow(TaskModel task, EditTaskViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close(); // все уже сохранено реактивно
        }
    }
}