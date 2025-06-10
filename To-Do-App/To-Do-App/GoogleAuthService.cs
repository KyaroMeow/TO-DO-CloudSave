using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading.Tasks;

public class GoogleAuthService
{
    private readonly string _applicationName;
    private readonly string _credentialsPath;

    public GoogleAuthService(string applicationName, string credentialsPath)
    {
        _applicationName = applicationName;
        _credentialsPath = credentialsPath;
    }

    /// <summary>
    /// Метод для аутентификации пользователя с помощью Google.
    /// </summary>
    /// <returns>Объект пользователя с токеном доступа.</returns>
    public async Task<UserCredential> AuthenticateAsync()
    {
        // Загрузка учетных данных
        using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
        {
            string[] scopes = { PeopleServiceService.Scope.UserinfoProfile, PeopleServiceService.Scope.UserinfoEmail };
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore("Store")
            );

            return credential;
        }
    }
}