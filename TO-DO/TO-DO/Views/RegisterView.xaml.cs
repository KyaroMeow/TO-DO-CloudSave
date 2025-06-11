using System.Windows;
using System.Windows.Controls;
using TO_DO.ViewModels;

namespace TO_DO.Views
{
    public partial class RegisterView : Window
    {
        public RegisterView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
                vm.SetPassword(((PasswordBox)sender).Password);
        }
    }
}