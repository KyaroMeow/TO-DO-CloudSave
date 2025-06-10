using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Google.Cloud.Firestore;

namespace To_Do_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private readonly GoogleAuthService _authService;
        private readonly UserService _userService;

        public MainWindow()
        {
            InitializeComponent();
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "to-do-1ad85-firebase-adminsdk-fbsvc-d0b0dc23ac.json");
            string applicationName = "To-Do";
            string credentialsPath = "to-do-1ad85-firebase-adminsdk-fbsvc-d0b0dc23ac.json";
            string projectId = "to-do-1ad85";

            _authService = new GoogleAuthService(applicationName, credentialsPath);
            _userService = new UserService(projectId);
            LoginButton_Click();
        }

         async void LoginButton_Click()
        {
            try
            {
                var credential = await _authService.AuthenticateAsync();
                string userToken = await _userService.GetOrCreateUserTokenAsync(credential);
                MessageBox.Show($"Вы успешно вошли в систему. Ваш токен: {userToken}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при аутентификации: {ex.Message}");
            }
        }
    }
}