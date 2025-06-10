using System.Data.SQLite;
using System.Runtime.CompilerServices;
using firebase;
using Google.Cloud.Firestore;

namespace FirestoreDemo
{
    class Program
    {
        public static string dbPath = "TaskBase.db";
        public static string connectionString = $"Data Source={dbPath};Version=3;";
        static async Task Main(string[] args)
        {
            // Укажите путь к JSON-ключу сервисного аккаунта
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "to-do-1ad85-firebase-adminsdk-fbsvc-d0b0dc23ac.json");

            // Инициализация Firestore
            string projectId = "to-do-1ad85";
            FirestoreDb db = FirestoreDb.Create(projectId);

            Console.WriteLine("Firestore инициализирован!");
            DbRepository dbRepository = new DbRepository(connectionString);
            DatabaseMigrator databaseMigrator = new DatabaseMigrator(connectionString,projectId);
            dbRepository.CreateTask("полить цветы", "холодной водой", DateTime.Now);
            dbRepository.AddTag("Неважно");
            dbRepository.AddTagToTask(1,1);
            await databaseMigrator.MigrateDatabaseAsync("onX62jJ902t3zGwqWsNK");
            Console.WriteLine("all good");
        }
    }
}