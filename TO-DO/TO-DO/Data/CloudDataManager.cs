using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using Google.Cloud.Firestore;
using Microsoft.Data.Sqlite;

namespace TO_DO.Data
{
    public class CloudDataManager
    {
        private readonly string _sqliteConnectionString;
        private readonly FirestoreDb _firestoreDb;
        private readonly string _userId;

        public CloudDataManager(string sqliteConnectionString, string firestoreProjectId, string userId)
        {
            _sqliteConnectionString = sqliteConnectionString;
            _firestoreDb = FirestoreDb.Create(firestoreProjectId);
            _userId = userId;
        }

        public async Task<bool> SyncFromCloudAsync()
        {
            if (!HasInternetConnection())
            {
                MessageBox.Show("Нет соединения с интернетом");
                return false;
            }

            try
            {
                await ClearLocalDatabaseAsync();
                await LoadTagsFromCloudAsync();
                await LoadTasksFromCloudAsync();
                await LoadTaskTagsFromCloudAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка синхронизации: {ex.Message}");
                return false;
            }
        }

        private bool HasInternetConnection()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        private async Task ClearLocalDatabaseAsync()
        {
            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();

            var commands = new[]
            {
                "DELETE FROM Tags",
                "DELETE FROM Tasks",
                "DELETE FROM TasksTag"
            };

            foreach (var cmdText in commands)
            {
                using var command = new SqliteCommand(cmdText, connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task LoadTagsFromCloudAsync()
        {
            var collectionRef = _firestoreDb.Collection("users").Document(_userId).Collection("Tags");
            var snapshot = await collectionRef.GetSnapshotAsync();

            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();

            foreach (var document in snapshot.Documents)
            {
                using var command = new SqliteCommand(
                    "INSERT INTO Tags (id, Name) VALUES ($id, $name)", connection);

                command.Parameters.AddWithValue("$id", Convert.ToInt32(document.Id));
                command.Parameters.AddWithValue("$name", document.GetValue<string>("Name"));

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task LoadTasksFromCloudAsync()
        {
            var collectionRef = _firestoreDb.Collection("users").Document(_userId).Collection("Tasks");
            var snapshot = await collectionRef.GetSnapshotAsync();

            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();

            foreach (var document in snapshot.Documents)
            {
                using var command = new SqliteCommand(
                    @"INSERT INTO Tasks (id, Name, Description, IsDone, DeadLine) 
                      VALUES ($id, $name, $desc, $isDone, $deadline)", connection);

                command.Parameters.AddWithValue("$id", Convert.ToInt32(document.Id));
                command.Parameters.AddWithValue("$name", document.GetValue<string>("Name"));
                command.Parameters.AddWithValue("$desc", document.GetValue<string>("Description"));
                command.Parameters.AddWithValue("$isDone", document.GetValue<bool>("IsDone") ? 1 : 0);
                command.Parameters.AddWithValue("$deadline", document.GetValue<string>("DeadLine"));

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task LoadTaskTagsFromCloudAsync()
        {
            var collectionRef = _firestoreDb.Collection("users").Document(_userId).Collection("TasksTag");
            var snapshot = await collectionRef.GetSnapshotAsync();

            using var connection = new SqliteConnection(_sqliteConnectionString);
            await connection.OpenAsync();

            foreach (var document in snapshot.Documents)
            {
                using var command = new SqliteCommand(
                    "INSERT INTO TasksTag (TaskId, TagId) VALUES ($taskId, $tagId)", connection);

                command.Parameters.AddWithValue("$taskId", Convert.ToInt32(document.GetValue<string>("TaskId")));
                command.Parameters.AddWithValue("$tagId", Convert.ToInt32(document.GetValue<string>("TagId")));

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}