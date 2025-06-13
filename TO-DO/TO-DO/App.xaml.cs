using System.Configuration;
using System.Windows;
using MaterialDesignThemes.Wpf;
using SQLitePCL;
using TO_DO.Services;
using TO_DO.Views;
using TO_DO.Properties;

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

			// Применяем тему
			ApplySavedTheme();

			UserId = AuthService.LoadUserId();

			Window window = string.IsNullOrWhiteSpace(UserId)
				? new LoginView()
				: new MainWindow();

			window.Show();
			Current.MainWindow = window;

			base.OnStartup(e);
		}

		private void ApplySavedTheme()
		{
			if (Application.Current.Resources["Theme"] is BundledTheme theme)
			{
				// Читаем из пользовательских настроек (user.config)
				bool isDark = Settings.Default.IsDarkTheme;
				theme.BaseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light;
			}
		}
	}

}
