using Google.Cloud.Firestore;

namespace FirestoreDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Укажите путь к JSON-ключу сервисного аккаунта
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "to-do-1ad85-firebase-adminsdk-fbsvc-d0b0dc23ac.json");

            // Инициализация Firestore
            string projectId = "to-do-1ad85"; // Замените на ваш projectId из firebase-config.json
            FirestoreDb db = FirestoreDb.Create(projectId);

            Console.WriteLine("Firestore инициализирован!");
            await AddUser(db,"123","vova",34);

        }
        static async Task AddUser(FirestoreDb db, string userId, string name, int age)
        {
            try
            {
                DocumentReference docRef = db.Collection("users").Document(userId);
                var user = new
                {
                    Name = name,
                    Age = age,
                    CreatedAt = Timestamp.GetCurrentTimestamp()
                };
                await docRef.SetAsync(user);
                Console.WriteLine($"Пользователь {name} добавлен с ID: {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении: {ex.Message}");
            }
        }
    }
}