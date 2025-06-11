using System;
using System.Collections.Generic;
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

        public async Task MigrateDatabaseAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId не может быть пустым.");

            // Определяем таблицы, которые нужно перенести
            string[] tableNames = { "Tags", "Tasks", "TasksTag" };

            foreach (var tableName in tableNames)
            {
                await MigrateTableAsync(tableName, userId);
            }

        }

        private async Task MigrateTableAsync(string tableName, string userId)
        {
            var rows = new List<Dictionary<string, object>>();

            // Извлекаем все строки из SQLite таблицы
            using (var connection = new SqliteConnection(_sqliteConnectionString))
            {
                await connection.OpenAsync();
                var query = $"SELECT * FROM {tableName}";
                using (var command = new SqliteCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var columns = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        columns.Add(reader.GetName(i));

                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        foreach (var col in columns)
                        {
                            var value = reader[col];
                            row[col] = value == DBNull.Value ? null : value;
                        }
                        rows.Add(row);
                    }
                }
            }

            // Сохраняем в Firestore: users/userId/{tableName}
            var collectionRef = _firestoreDb.Collection("users").Document(userId).Collection(tableName);
            var batch = _firestoreDb.StartBatch();

            foreach (var row in rows)
            {
                var docRef = collectionRef.Document(); // сгенерировать id
                batch.Set(docRef, row);
            }

            await batch.CommitAsync();
        }
    }
}