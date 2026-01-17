using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Collections.Generic;

namespace RemoteDesktop.Server.Database
{
    public class DatabaseManager
    {
        // Chuỗi kết nối SQL Server
        private string connectionString = @"Server=.\SQLEXPRESS;Database=RemoteDesktopDB;User Id=sa;Password=your_password;TrustServerCertificate=True;";

        public void InitializeDatabase()
        {
            // Sử dụng DatabaseConnect.GetConnection() thay vì tự tạo
            using (var connection = DatabaseConnect.GetConnection())
            {
                if (connection == null) return;
                connection.Open();
                string createTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                    BEGIN
                        CREATE TABLE Users (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Username NVARCHAR(50) NOT NULL UNIQUE,
                            Password NVARCHAR(255) NOT NULL,
                            Status INT NOT NULL DEFAULT 0
                        );
                        INSERT INTO Users (Username, Password, Status) VALUES ('admin', '123456', 1);
                    END";
                using (var command = new SqlCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool ValidateUser(string username, string password)
        {
            using (var connection = DatabaseConnect.GetConnection())
            {
                if (connection == null) return false;
                connection.Open();
                string query = "SELECT COUNT(*) FROM Users WHERE Username = @user AND Password = @pass AND Status = 1";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@user", username);
                    command.Parameters.AddWithValue("@pass", password);
                    return (int)command.ExecuteScalar() > 0;
                }
            }
        }

        public bool RegisterUser(string username, string password)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO Users (Username, Password, Status) VALUES (@user, @pass, 0)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@user", username);
                        command.Parameters.AddWithValue("@pass", password);

                        return command.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi đăng ký tài khoản: " + ex.Message);
                return false;
            }
        }

        // Lấy danh sách tài khoản đang chờ phê duyệt (Dùng cho giao diện Server)
        public DataTable GetPendingUsers()
        {
            DataTable dt = new DataTable();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Username, CreatedAt FROM Users WHERE Status = 0";
                    using (var adapter = new SqlDataAdapter(query, connection))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch { }
            return dt;
        }

        // Phê duyệt tài khoản (Sự đồng thuận từ Server)
        public bool ApproveUser(string username, int status)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE Users SET Status = @status WHERE Username = @user";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@status", status);
                        command.Parameters.AddWithValue("@user", username);

                        return command.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}