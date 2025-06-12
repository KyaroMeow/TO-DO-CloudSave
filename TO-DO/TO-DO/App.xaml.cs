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
			Batteries.Init(); // SQLitePCLRaw init

			UserId = AuthService.LoadUserId();

			Window window;
			if (string.IsNullOrWhiteSpace(UserId))
			{
				// Открыть окно входа
				window = new LoginView();
			}
			else
			{
				// Открыть основное окно
				window = new MainWindow();
			}

			window.Show();
			Current.MainWindow = window;

			base.OnStartup(e);
		}


	}

}
