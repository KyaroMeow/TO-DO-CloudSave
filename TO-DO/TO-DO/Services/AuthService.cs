using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FirebaseAdmin;

namespace TO_DO.Services
{
    public class AuthService
    {
        private readonly FirestoreDb _db;
        private const string AuthFile = "auth.json";

        public AuthService(string projectId)
        {
            InitializeFirebase(projectId);
            _db = FirestoreDb.Create(projectId);
        }

        private void InitializeFirebase(string projectId)
        {
            try
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",
                    "to-do-1ad85-firebase-adminsdk-fbsvc-d0b0dc23ac.json");

                if (FirebaseApp.DefaultInstance == null)
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.GetApplicationDefault(),
                    });
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибки инициализации Firebase
                throw new Exception($"Error initializing Firebase: {ex.Message}");
            }
        }

        public async Task<string> RegisterAsync(string email, string password, string displayName)
        {
            // Создаем пользователя в Authentication
            var userArgs = new UserRecordArgs()
            {
                Email = email,
                Password = password,
                DisplayName = displayName
            };

            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);

            // Создаем документ пользователя с подколлекциями
            var userDoc = _db.Collection("users").Document(userRecord.Uid);
            await userDoc.SetAsync(new
            {
                Email = email,
                DisplayName = displayName,
                CreatedAt = Timestamp.GetCurrentTimestamp()
            });

            // Инициализируем подколлекции начальными значениями
            await InitializeUserCollections(userDoc);

            return userRecord.Uid;
        }

        private async Task InitializeUserCollections(DocumentReference userDoc)
        {
            // Создаем пустые коллекции для тегов, задач и связей
            var batch = _db.StartBatch();

            // Tags
            var tagRef = userDoc.Collection("Tags").Document("placeholder");
            batch.Set(tagRef, new { IsPlaceholder = true });

            // Tasks
            var taskRef = userDoc.Collection("Tasks").Document("placeholder");
            batch.Set(taskRef, new { IsPlaceholder = true });

            // TasksTag
            var taskTagRef = userDoc.Collection("TasksTag").Document("placeholder");
            batch.Set(taskTagRef, new { IsPlaceholder = true });

            await batch.CommitAsync();

            // Удаляем placeholder-документы
            await Task.WhenAll(
                tagRef.DeleteAsync(),
                taskRef.DeleteAsync(),
                taskTagRef.DeleteAsync()
            );
        }

        public async Task<string> LoginAsync(string email, string password)
        {
            var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
            return userRecord.Uid;
        }

        public void SaveUserId(string userId)
        {
            File.WriteAllText(AuthFile, JsonSerializer.Serialize(new { UserId = userId }));
        }

        public string LoadUserId()
        {
            if (File.Exists(AuthFile))
            {
                var json = File.ReadAllText(AuthFile);
                return JsonSerializer.Deserialize<AuthData>(json).UserId;
            }
            return string.Empty;
        }

        public void Logout()
        {
            if (File.Exists(AuthFile))
                File.Delete(AuthFile);
        }

        public class AuthData
        {
            public string UserId { get; set; }
        }
    }
}