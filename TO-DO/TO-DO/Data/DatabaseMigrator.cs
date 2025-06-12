using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Data.Sqlite;

namespace TO_DO.Data
{
    public class DatabaseMigrator
    {
        private readonly string _sqliteConnectionString;
        private readonly FirestoreDb _firestoreDb;

        public DatabaseMigrator(string sqliteConnectionString, string firestoreProjectId)
        {
            _sqliteConnectionString = sqliteConnectionString;
            _firestoreDb = FirestoreDb.Create(firestoreProjectId);
            Debug.WriteLine("DatabaseMigrator инициализирован");
        }

        public async Task<bool> MigrateToCloudAsync(string userId)
        {
            Debug.WriteLine($"Начало миграции для пользователя: {userId}");

            if (string.IsNullOrWhiteSpace(userId))
            {
                Debug.WriteLine("Ошибка: userId не может быть пустым");
                throw new ArgumentException("User ID cannot be empty");
            }

            if (!HasInternetConnection())
            {
                Debug.WriteLine("Нет интернет-соединения");
                return false;
            }

            try
            {
                Debug.WriteLine("Получение данных из SQLite...");
                var (tags, tasks, taskTags) = await GetLocalDataAsync();

                Debug.WriteLine($"Найдено: {tags.Count} тегов, {tasks.Count} задач, {taskTags.Count} связей");

                if (tags.Count == 0 && tasks.Count == 0 && taskTags.Count == 0)
                {
                    Debug.WriteLine("Нет данных для миграции");
                    return true;
                }

                Debug.WriteLine("Очистка коллекций в облаке...");
                await ClearCloudCollectionsAsync(userId);

                Debug.WriteLine("Отправка данных в облако...");
                await UploadDataToCloudAsync(userId, tags, tasks, taskTags);

                Debug.WriteLine("Миграция завершена успешно");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка миграции: {ex}");
                return false;
            }
        }

        private bool HasInternetConnection()
        {
            try
            {
                var result = NetworkInterface.GetIsNetworkAvailable();
                Debug.WriteLine($"Проверка соединения: {(result ? "Есть соединение" : "Нет соединения")}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка проверки соединения: {ex}");
                return false;
            }
        }

        private async Task<(List<Dictionary<string, object>> tags,
                          List<Dictionary<string, object>> tasks,
                          List<Dictionary<string, object>> taskTags)> GetLocalDataAsync()
        {
            var tags = new List<Dictionary<string, object>>();
            var tasks = new List<Dictionary<string, object>>();
            var taskTags = new List<Dictionary<string, object>>();

            using (var connection = new SqliteConnection(_sqliteConnectionString))
            {
                await connection.OpenAsync();

                Debug.WriteLine("Загрузка тегов...");
                tags = await LoadTagsAsync(connection);

                Debug.WriteLine("Загрузка задач...");
                tasks = await LoadTasksAsync(connection);

                Debug.WriteLine("Загрузка связей...");
                taskTags = await LoadTaskTagsAsync(connection);
            }

            return (tags, tasks, taskTags);
        }

        private async Task<List<Dictionary<string, object>>> LoadTagsAsync(SqliteConnection connection)
        {
            var result = new List<Dictionary<string, object>>();

            using (var command = new SqliteCommand("SELECT id, Name FROM Tags", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new Dictionary<string, object>
                    {
                        ["Id"] = reader.GetInt32(0).ToString(),
                        ["Name"] = reader.GetString(1)
                    });
                }
            }
            return result;
        }

        private async Task<List<Dictionary<string, object>>> LoadTasksAsync(SqliteConnection connection)
        {
            var result = new List<Dictionary<string, object>>();

            using (var command = new SqliteCommand(
                "SELECT id, Name, Description, IsDone, DeadLine FROM Tasks", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var task = new Dictionary<string, object>
                    {
                        ["Id"] = reader.GetInt32(0).ToString(),
                        ["Name"] = reader.GetString(1),
                        ["IsDone"] = reader.GetInt32(3) == 1
                    };

                    if (!reader.IsDBNull(2))
                        task["Description"] = reader.GetString(2);

                    if (!reader.IsDBNull(4))
                        task["DeadLine"] = reader.GetString(4);

                    result.Add(task);
                }
            }
            return result;
        }

        private async Task<List<Dictionary<string, object>>> LoadTaskTagsAsync(SqliteConnection connection)
        {
            var result = new List<Dictionary<string, object>>();

            using (var command = new SqliteCommand("SELECT TaskId, TagId FROM TasksTag", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new Dictionary<string, object>
                    {
                        ["Id"] = $"{reader.GetInt32(0)}_{reader.GetInt32(1)}",
                        ["TaskId"] = reader.GetInt32(0).ToString(),
                        ["TagId"] = reader.GetInt32(1).ToString()
                    });
                }
            }
            return result;
        }

        private async Task ClearCloudCollectionsAsync(string userId)
        {
            try
            {
                var userDocRef = _firestoreDb.Collection("users").Document(userId);
                var collections = await userDocRef.ListCollectionsAsync().ToListAsync();
                Debug.WriteLine($"Найдено коллекций для очистки: {collections.Count}");

                foreach (var collection in collections)
                {
                    Debug.WriteLine($"Очистка коллекции: {collection.Id}");
                    var snapshot = await collection.Limit(500).GetSnapshotAsync();

                    while (snapshot.Count > 0)
                    {
                        var batch = _firestoreDb.StartBatch();
                        foreach (var doc in snapshot.Documents)
                        {
                            batch.Delete(doc.Reference);
                        }

                        var deletedCount = await batch.CommitAsync();
                        Debug.WriteLine($"Удалено документов: {snapshot.Count}");
                        snapshot = await collection.Limit(500).GetSnapshotAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка очистки коллекций: {ex}");
                throw;
            }
        }

        private async Task UploadDataToCloudAsync(
            string userId,
            List<Dictionary<string, object>> tags,
            List<Dictionary<string, object>> tasks,
            List<Dictionary<string, object>> taskTags)
        {
            try
            {
                Debug.WriteLine("Начало загрузки данных в облако...");

                // Загрузка тегов
                if (tags.Count > 0)
                {
                    Debug.WriteLine($"Загрузка {tags.Count} тегов...");
                    await UploadCollectionAsync(userId, "Tags", tags);
                }

                // Загрузка задач
                if (tasks.Count > 0)
                {
                    Debug.WriteLine($"Загрузка {tasks.Count} задач...");
                    await UploadCollectionAsync(userId, "Tasks", tasks);
                }

                // Загрузка связей
                if (taskTags.Count > 0)
                {
                    Debug.WriteLine($"Загрузка {taskTags.Count} связей...");
                    await UploadCollectionAsync(userId, "TasksTag", taskTags);
                }

                Debug.WriteLine("Все данные успешно загружены");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки данных: {ex}");
                throw;
            }
        }

        private async Task UploadCollectionAsync(
            string userId,
            string collectionName,
            List<Dictionary<string, object>> items)
        {
            try
            {
                Debug.WriteLine($"Начало загрузки коллекции {collectionName} ({items.Count} элементов)");

                var collectionRef = _firestoreDb.Collection("users").Document(userId).Collection(collectionName);
                var batch = _firestoreDb.StartBatch();
                int batchCounter = 0;
                int totalCounter = 0;

                foreach (var item in items)
                {
                    var docId = item.ContainsKey("Id") ? item["Id"].ToString() : null;
                    var docRef = string.IsNullOrEmpty(docId)
                        ? collectionRef.Document()
                        : collectionRef.Document(docId);

                    batch.Set(docRef, item);
                    batchCounter++;
                    totalCounter++;

                    if (batchCounter >= 400) // Ограничение Firestore на размер пакета
                    {
                        Debug.WriteLine($"Коммит пакета ({batchCounter} операций)");
                        await batch.CommitAsync();
                        batch = _firestoreDb.StartBatch();
                        batchCounter = 0;
                    }
                }

                if (batchCounter > 0)
                {
                    Debug.WriteLine($"Финальный коммит ({batchCounter} операций)");
                    await batch.CommitAsync();
                }

                Debug.WriteLine($"Коллекция {collectionName} успешно обновлена. Всего элементов: {totalCounter}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки коллекции {collectionName}: {ex}");
                throw;
            }
        }
    }
}