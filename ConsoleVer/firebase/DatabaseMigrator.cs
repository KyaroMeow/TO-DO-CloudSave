using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace firebase
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
        public async Task MigrateDatabaseAsync(string userCode)
        {
            if (string.IsNullOrWhiteSpace(userCode))
                throw new ArgumentException("Код пользователя не может быть пустым.", nameof(userCode));

            // Получение списка таблиц из SQLite
            var tableNames = new List<string>();
            using (var connection = new SQLiteConnection(_sqliteConnectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tableNames.Add(reader.GetString(0));
                        }
                    }
                }
            }

            // Перебор таблиц и миграция данных для конкретного пользователя
            foreach (var tableName in tableNames)
            {
                Console.WriteLine($"Миграция таблицы: {tableName}");
                await MigrateTableAsync(tableName, userCode);
            }

            Console.WriteLine("Миграция базы данных завершена.");
        }

        private async Task MigrateTableAsync(string tableName, string userCode)
        {
            var data = new List<Dictionary<string, object>>();

            // Подключение к SQLite и извлечение данных
            using (var connection = new SQLiteConnection(_sqliteConnectionString))
            {
                await connection.OpenAsync();
                string query = $"SELECT * FROM {tableName}";
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var columnNames = new List<string>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            columnNames.Add(reader.GetName(i));
                        }

                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            foreach (var column in columnNames)
                            {
                                var value = reader[column];
                                row[column] = value == DBNull.Value ? null : value;
                            }
                            // Добавляем код пользователя в данные
                            row["UserCode"] = userCode;
                            data.Add(row);
                        }
                    }
                }
            }

            // Отправка данных в Cloud Firestore
            var collection = _firestoreDb.Collection(userCode);
            var batch = _firestoreDb.StartBatch();

            foreach (var document in data)
            {
                var docRef = collection.Document();
                batch.Create(docRef, document);
            }

            await batch.CommitAsync();
        }
    }
}
