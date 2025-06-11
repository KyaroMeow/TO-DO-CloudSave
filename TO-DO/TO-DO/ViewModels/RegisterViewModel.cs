using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using TO_DO.Services;
using TO_DO.Views;

namespace TO_DO.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string displayName;

        private string password;

        public void SetPassword(string pwd) => password = pwd;

        [RelayCommand]
        private async void Register()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите email и пароль");
                return;
            }

            try
            {
                var authService = App.AuthService;
                var userId = await authService.RegisterAsync(Email, password, DisplayName);

                if (!string.IsNullOrEmpty(userId))
                {
                    authService.SaveUserId(userId);

                    var main = new MainWindow();
                    Application.Current.MainWindow = main;
                    main.Show();
                    CloseCurrentWindow();
                }
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
            {
                MessageBox.Show("Ошибка регистрации: " + ex.Message);
            }
        }

        [RelayCommand]
        private void Back()
        {
            var login = new LoginView();
            login.Show();
            CloseCurrentWindow();
        }

        private void CloseCurrentWindow()
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is RegisterView)
                {
                    w.Close();
                    break;
                }
            }
        }
    }
}