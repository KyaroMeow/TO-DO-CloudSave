using System;
using System.Collections.Generic;
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
        }

        public async Task<bool> MigrateToCloudAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty");

            if (!HasInternetConnection()) return false;

            try
            {
                await ClearUserCollectionsAsync(userId);
                await MigrateTagsAsync(userId);
                await MigrateTasksAsync(userId);
                await MigrateTaskTagsAsync(userId);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool HasInternetConnection()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        private async Task ClearUserCollectionsAsync(string userId)
        {
            var userDocRef = _firestoreDb.Collection("users").Document(userId);
            var collections = await userDocRef.ListCollectionsAsync().ToListAsync();

            foreach (var collection in collections)
            {
                var snapshot = await collection.Limit(500).GetSnapshotAsync();
                while (snapshot.Count > 0)
                {
                    var batch = _firestoreDb.StartBatch();
                    foreach (var doc in snapshot.Documents)
                    {
                        batch.Delete(doc.Reference);
                    }
                    await batch.CommitAsync();
                    snapshot = await collection.Limit(500).GetSnapshotAsync();
                }
            }
        }

        private async Task MigrateTagsAsync(string userId)
        {
            var tags = new List<Dictionary<string, object>>();

            using (var connection = new SqliteConnection(_sqliteConnectionString))
            {
                await connection.OpenAsync();
                using var command = new SqliteCommand("SELECT id, Name FROM Tags", connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tags.Add(new Dictionary<string, object>
                    {
                        ["Id"] = reader.GetInt32(0).ToString(),
                        ["Name"] = reader.GetString(1)
                    });
                }
            }

            var collectionRef = _firestoreDb.Collection("users").Document(userId).Collection("Tags");
            var batch = _firestoreDb.StartBatch();

            foreach (var tag in tags)
            {
                var docRef = collectionRef.Document(tag["Id"].ToString());
                batch.Set(docRef, tag);
            }

            await batch.CommitAsync();
        }

        private async Task MigrateTasksAsync(string userId)
        {
            var tasks = new List<Dictionary<string, object>>();

            using (var connection = new SqliteConnection(_sqliteConnectionString))
            {
                await connection.OpenAsync();
                using var command = new SqliteCommand(
                    "SELECT id, Name, Description, IsDone, DeadLine FROM Tasks", connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    tasks.Add(new Dictionary<string, object>
                    {
                        ["Id"] = reader.GetInt32(0).ToString(),
                        ["Name"] = reader.GetString(1),
                        ["Description"] = reader.IsDBNull(2) ? null : reader.GetString(2),
                        ["IsDone"] = reader.GetInt32(3) == 1,
                        ["DeadLine"] = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }
            }

            var collectionRef = _firestoreDb.Collection("users").Document(userId).Collection("Tasks");
            var batch = _firestoreDb.StartBatch();

            foreach (var task in tasks)
            {
                var docRef = collectionRef.Document(task["Id"].ToString());
                batch.Set(docRef, task);
            }

            await batch.CommitAsync();
        }

        private async Task MigrateTaskTagsAsync(string userId)
        {
            var taskTags = new List<Dictionary<string, object>>();

            using (var connection = new SqliteConnection(_sqliteConnectionString))
            {
                await connection.OpenAsync();
                using var command = new SqliteCommand(
                    "SELECT TaskId, TagId FROM TasksTag", connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    taskTags.Add(new Dictionary<string, object>
                    {
                        ["Id"] = $"{reader.GetInt32(0)}_{reader.GetInt32(1)}",
                        ["TaskId"] = reader.GetInt32(0).ToString(),
                        ["TagId"] = reader.GetInt32(1).ToString()
                    });
                }
            }

            var collectionRef = _firestoreDb.Collection("users").Document(userId).Collection("TasksTag");
            var batch = _firestoreDb.StartBatch();

            foreach (var taskTag in taskTags)
            {
                var docRef = collectionRef.Document(taskTag["Id"].ToString());
                batch.Set(docRef, taskTag);
            }

            await batch.CommitAsync();
        }
    }
}