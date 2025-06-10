using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Services;
using Google.Cloud.Firestore;

public class UserService
{
    private readonly FirestoreDb _firestoreDb;

    public UserService(string projectId)
    {
        _firestoreDb = FirestoreDb.Create(projectId);
    }

    public async Task<string> GetOrCreateUserTokenAsync(Google.Apis.Auth.OAuth2.UserCredential credential)
    {
        // Создание сервиса People API
        var service = new PeopleServiceService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "To-Do",
        });

        // Запрос информации о пользователе
        string personFields = "emailAddresses";
        var request = service.People.Get("people/me");
        request.PersonFields = personFields;

        try
        {
            Person profile = await request.ExecuteAsync();

            // Получение email пользователя
            string email = null;
            if (profile.EmailAddresses != null && profile.EmailAddresses.Count > 0)
            {
                email = profile.EmailAddresses[0].Value;
            }

            if (string.IsNullOrEmpty(email))
            {
                throw new Exception("Не удалось получить email пользователя.");
            }

            // Поиск пользователя в Firestore
            var usersCollection = _firestoreDb.Collection("users");
            var query = usersCollection.WhereEqualTo("Email", email).Limit(1);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Count > 0)
            {
                // Если пользователь существует, возвращаем его код
                var userDoc = snapshot.FirstOrDefault();
                return userDoc.Id;
            }
            else
            {
                // Если пользователь не существует, создаем его
                var newUser = new Dictionary<string, object>
                {
                    { "Email", email },
                    { "RegisteredAt", DateTime.UtcNow }
                };
                var docRef = await usersCollection.AddAsync(newUser);
                return docRef.Id;
            }
        }
        catch (Google.GoogleApiException ex)
        {
            // Обработка ошибок, связанных с Google API
            throw new Exception($"Ошибка при получении данных пользователя: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            // Обработка других ошибок
            throw new Exception($"Ошибка: {ex.Message}", ex);
        }
    }
}