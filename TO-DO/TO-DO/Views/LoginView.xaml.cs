using System.Windows;
using System.Windows.Controls;
using TO_DO.ViewModels;

namespace TO_DO.Views
{
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
                vm.SetPassword(((PasswordBox)sender).Password);
        }
    }
}