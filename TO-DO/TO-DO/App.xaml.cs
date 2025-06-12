using System.Windows;
using SQLitePCL;
using TO_DO.Services;
using TO_DO.Views;

namespace TO_DO
{
	public partial class App : Application
	{
        public static string UserId { get; private set; } = "";
        public static AuthService AuthService;
        protected override void OnStartup(StartupEventArgs e)
		{
            AuthService = new AuthService("to-do-1ad85");
            Batteries.Init(); // Инициализация SQLitePCLRaw
            UserId = AuthService.LoadUserId();
            base.OnStartup(e);
            if (string.IsNullOrWhiteSpace(UserId))
            {
                var login = new Views.LoginView();
                Current.MainWindow = login;
                login.ShowDialog();
            }
           

        }

    }

}
