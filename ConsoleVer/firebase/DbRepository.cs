using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace firebase
{
    public class DbRepository
    {
        private readonly string connectionString;
        public DbRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }
        public int AddTag(string name)
        {
            // Проверка входных данных
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название тега не может быть пустым.", nameof(name));

            // SQL-запрос для добавления тега
            string sql = @"
            INSERT INTO Tags (Name)
            VALUES (@Name);
            SELECT last_insert_rowid();
        ";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Добавление параметра
                    command.Parameters.AddWithValue("@Name", name);

                    // Выполнение запроса и получение идентификатора нового тега
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }
        public int RemoveTagById(int id)
        {
            // Проверка входных данных
            if (id <= 0)
                throw new ArgumentException("Идентификатор тега должен быть положительным числом.", nameof(id));

            // SQL-запрос для удаления тега
            string sql = @"
            DELETE FROM Tags
            WHERE id = @Id;
        ";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Добавление параметра
                    command.Parameters.AddWithValue("@Id", id);

                    // Выполнение запроса и получение количества затронутых строк
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }
            }
        }
        public int CreateTask(string name, string description = null, DateTime? deadline = null)
        {
            // Проверка входных данных
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название задачи не может быть пустым.", nameof(name));

            // Форматирование даты и времени для SQLite
            string deadlineString = deadline.HasValue ? deadline.Value.ToString("yyyy-MM-dd HH:mm:ss") : null;

            // Строка SQL-запроса с параметрами
            string sql = @"
            INSERT INTO Tasks (Name, Description, DeadLine)
            VALUES (@Name, @Description, @DeadLine);
            SELECT last_insert_rowid();
        ";

            // Использование using для автоматического управления ресурсами
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Добавление параметров
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? (object)DBNull.Value : description);
                    command.Parameters.AddWithValue("@DeadLine", string.IsNullOrWhiteSpace(deadlineString) ? (object)DBNull.Value : deadlineString);

                    // Выполнение запроса и получение идентификатора новой задачи
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }
        public int ToggleIsDone(int id)
        {
            // Проверка входных данных
            if (id <= 0)
                throw new ArgumentException("Идентификатор задачи должен быть положительным числом.", nameof(id));

            // SQL-запрос для обновления статуса
            string sql = @"
            UPDATE Tasks
            SET IsDone = CASE 
                            WHEN IsDone = 1 THEN 0 
                            ELSE 1 
                         END
            WHERE id = @Id;
        ";

            // Использование using для автоматического управления ресурсами
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Добавление параметра
                    command.Parameters.AddWithValue("@Id", id);

                    // Выполнение запроса и получение количества затронутых строк
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }
            }
        }
        public int UpdateName(int id, string newName)
        {
            // Проверка входных данных
            if (id <= 0)
                throw new ArgumentException("Идентификатор задачи должен быть положительным числом.", nameof(id));
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Новое название задачи не может быть пустым.", nameof(newName));

            // SQL-запрос для обновления названия
            string sql = @"
        UPDATE Tasks
        SET Name = @Name
        WHERE id = @Id;
    ";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Добавление параметров
                    command.Parameters.AddWithValue("@Name", newName);
                    command.Parameters.AddWithValue("@Id", id);

                    // Выполнение запроса и получение количества затронутых строк
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }
            }
        }
        public int UpdateDeadline(int id, DateTime? newDeadline)
        {
            // Проверка входных данных
            if (id <= 0)
                throw new ArgumentException("Идентификатор задачи должен быть положительным числом.", nameof(id));

            // Форматирование даты и времени для SQLite
            string deadlineString = newDeadline.HasValue ? newDeadline.Value.ToString("yyyy-MM-dd HH:mm:ss") : null;

            // SQL-запрос для обновления даты дедлайна
            string sql = @"
        UPDATE Tasks
        SET DeadLine = @DeadLine
        WHERE id = @Id;
    ";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Добавление параметров
                    command.Parameters.AddWithValue("@DeadLine", string.IsNullOrWhiteSpace(deadlineString) ? (object)DBNull.Value : deadlineString);
                    command.Parameters.AddWithValue("@Id", id);

                    // Выполнение запроса и получение количества затронутых строк
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }
            }
        }
        public int UpdateDescription(int id, string newDescription)
        {
            // Проверка входных данных
            if (id <= 0)
                throw new ArgumentException("Идентификатор задачи должен быть положительным числом.", nameof(id));

            // SQL-запрос для обновления описания
            string sql = @"
        UPDATE Tasks
        SET Description = @Description
        WHERE id = @Id;
    ";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Добавление параметров
                    command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(newDescription) ? (object)DBNull.Value : newDescription);
                    command.Parameters.AddWithValue("@Id", id);

                    // Выполнение запроса и получение количества затронутых строк
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }
            }
        }
        public int DeleteTask(int id)
        {
            // Проверка входных данных
            if (id <= 0)
                throw new ArgumentException("Идентификатор задачи должен быть положительным числом.", nameof(id));

            // SQL-запрос для удаления задачи
            string sql = @"
        DELETE FROM Tasks
        WHERE id = @Id;
    ";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Добавление параметра
                    command.Parameters.AddWithValue("@Id", id);

                    // Выполнение запроса и получение количества затронутых строк
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }
            }
        }
        public int AddTagToTask(int taskId, int tagId)
        {
            // Проверка входных данных
            if (taskId <= 0)
                throw new ArgumentException("Идентификатор задачи должен быть положительным числом.", nameof(taskId));
            if (tagId <= 0)
                throw new ArgumentException("Идентификатор тега должен быть положительным числом.", nameof(tagId));

            // SQL-запрос для добавления связи
            string sql = @"
            INSERT INTO TasksTag (TaskId, TagId)
            VALUES (@TaskId, @TagId);
        ";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Добавление параметров
                    command.Parameters.AddWithValue("@TaskId", taskId);
                    command.Parameters.AddWithValue("@TagId", tagId);

                    // Выполнение запроса и получение количества затронутых строк
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }
            }
        }
        public int RemoveTagFromTask(int taskId, int tagId)
        {
            // Проверка входных данных
            if (taskId <= 0)
                throw new ArgumentException("Идентификатор задачи должен быть положительным числом.", nameof(taskId));
            if (tagId <= 0)
                throw new ArgumentException("Идентификатор тега должен быть положительным числом.", nameof(tagId));

            // SQL-запрос для удаления связи
            string sql = @"
            DELETE FROM TasksTag
            WHERE TaskId = @TaskId AND TagId = @TagId;
        ";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    // Добавление параметров
                    command.Parameters.AddWithValue("@TaskId", taskId);
                    command.Parameters.AddWithValue("@TagId", tagId);

                    // Выполнение запроса и получение количества затронутых строк
                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected;
                }
            }
        }
    }
}
