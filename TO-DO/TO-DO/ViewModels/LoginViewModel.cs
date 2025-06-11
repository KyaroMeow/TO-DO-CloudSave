using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using System.Windows;
using TO_DO.Services;
using TO_DO.Views;

namespace TO_DO.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string email;

        private string password;

        public void SetPassword(string pwd) => password = pwd;

        [RelayCommand]
        private async void Login()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите email и пароль");
                return;
            }

            try
            {
                var authService = App.AuthService;
                var userId = await authService.LoginAsync(Email, password);

                if (!string.IsNullOrEmpty(userId))
                {
                    authService.SaveUserId(userId);
                    

                    var main = new MainWindow();
                    Application.Current.MainWindow = main;
                    main.Show();
                    CloseCurrentWindow();
                }
                else
                {
                    MessageBox.Show("Пользователь не найден");
                }
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
            {
                MessageBox.Show("Ошибка входа: " + ex.Message);
            }
        }

        [RelayCommand]
        private void OpenRegister()
        {
            var reg = new RegisterView();
            reg.Show();
            CloseCurrentWindow();
        }

        private void CloseCurrentWindow()
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is Views.LoginView)
                {
                    w.Close();
                    break;
                }
            }
        }
    }
}