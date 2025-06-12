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
                using (var connection = new SqliteConnection(_sqliteConnectionString))
                {
                    await connection.OpenAsync();

                    // Основные операции в транзакции
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            await ClearTablesAsync(connection, transaction);
                            await LoadTagsFromCloudAsync(connection, transaction);
                            await LoadTasksFromCloudAsync(connection, transaction);
                            await LoadTaskTagsFromCloudAsync(connection, transaction);

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка синхронизации: {ex.Message}");
                            return false;
                        }
                    }

                    // VACUUM выполняется отдельно после транзакции
                    await VacuumDatabaseAsync(connection);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе: {ex.Message}");
                return false;
            }
        }

        private bool HasInternetConnection()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return false;
            }
        }

        private async Task ClearTablesAsync(SqliteConnection connection, SqliteTransaction transaction)
        {
            var commands = new[]
            {
                "DELETE FROM Tags",
                "DELETE FROM Tasks",
                "DELETE FROM TasksTag"
            };

            foreach (var cmdText in commands)
            {
                using var command = new SqliteCommand(cmdText, connection, transaction);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task VacuumDatabaseAsync(SqliteConnection connection)
        {
            try
            {
                using var command = new SqliteCommand("VACUUM", connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении VACUUM: {ex.Message}");
            }
        }

        private async Task LoadTagsFromCloudAsync(SqliteConnection connection, SqliteTransaction transaction)
        {
            var collectionRef = _firestoreDb.Collection("users").Document(_userId).Collection("Tags");
            var snapshot = await collectionRef.GetSnapshotAsync();

            foreach (var document in snapshot.Documents)
            {
                var id = document.Id;
                var name = document.GetValue<string>("Name");

                using var command = new SqliteCommand(
                    "INSERT OR REPLACE INTO Tags (id, Name) VALUES (@id, @name)",
                    connection,
                    transaction);

                command.Parameters.AddWithValue("@id", int.Parse(id));
                command.Parameters.AddWithValue("@name", name);

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task LoadTasksFromCloudAsync(SqliteConnection connection, SqliteTransaction transaction)
        {
            var collectionRef = _firestoreDb.Collection("users").Document(_userId).Collection("Tasks");
            var snapshot = await collectionRef.GetSnapshotAsync();

            foreach (var document in snapshot.Documents)
            {
                var id = document.Id;
                var name = document.GetValue<string>("Name");
                var description = document.ContainsField("Description") ? document.GetValue<string>("Description") : null;
                var isDone = document.GetValue<bool>("IsDone");
                var deadLine = document.ContainsField("DeadLine") ? document.GetValue<string>("DeadLine") : null;

                using var command = new SqliteCommand(
                    @"INSERT OR REPLACE INTO Tasks 
                      (id, Name, Description, IsDone, DeadLine) 
                      VALUES (@id, @name, @desc, @isDone, @deadline)",
                    connection,
                    transaction);

                command.Parameters.AddWithValue("@id", int.Parse(id));
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@desc", description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@isDone", isDone ? 1 : 0);
                command.Parameters.AddWithValue("@deadline", deadLine ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task LoadTaskTagsFromCloudAsync(SqliteConnection connection, SqliteTransaction transaction)
        {
            var collectionRef = _firestoreDb.Collection("users").Document(_userId).Collection("TasksTag");
            var snapshot = await collectionRef.GetSnapshotAsync();

            foreach (var document in snapshot.Documents)
            {
                var taskId = document.GetValue<string>("TaskId");
                var tagId = document.GetValue<string>("TagId");

                using var command = new SqliteCommand(
                    "INSERT OR IGNORE INTO TasksTag (TaskId, TagId) VALUES (@taskId, @tagId)",
                    connection,
                    transaction);

                command.Parameters.AddWithValue("@taskId", int.Parse(taskId));
                command.Parameters.AddWithValue("@tagId", int.Parse(tagId));

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}