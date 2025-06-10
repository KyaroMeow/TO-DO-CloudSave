using System.Windows;
using SQLitePCL;
namespace TO_DO
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			Batteries.Init(); // Инициализация SQLitePCLRaw
			base.OnStartup(e);
		}
	}

}
