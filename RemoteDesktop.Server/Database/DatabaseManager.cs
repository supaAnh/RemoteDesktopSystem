using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Collections.Generic;

namespace RemoteDesktop.Server.Database
{
    public class DatabaseManager
    {
        // Chuỗi kết nối dự phòng (dùng cho các hàm cũ chưa chuyển sang DatabaseConnect)
        private string connectionString = @"Server=127.0.0.1,1433;Database=RemoteDesktopDB;User Id=sa;Password=@Supanh123;TrustServerCertificate=True;";

        public void InitializeDatabase()
        {
            // Sử dụng DatabaseConnect.GetConnection() để đảm bảo đồng bộ
            using (var connection = DatabaseConnect.GetConnection())
            {
                if (connection == null) return;
                connection.Open();

                // 1. TẠO BẢNG USERS (Cũ - Giữ nguyên)
                string createTableQuery = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                    BEGIN
                        CREATE TABLE Users (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            Username NVARCHAR(50) NOT NULL UNIQUE,
                            Password NVARCHAR(255) NOT NULL,
                            Status INT NOT NULL DEFAULT 0, -- 0: Chờ duyệt, 1: Đã phê duyệt
                            CreatedAt DATETIME DEFAULT GETDATE()
                        );

                    INSERT INTO Users (Username, Password, Status) 
                    VALUES ('admin', '123456', 1);
                    END;";
                using (var command = new SqlCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                // 2. TẠO BẢNG SERVERLOGS (MỚI - Thêm vào đây)
                string createLogsTable = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ServerLogs')
                    BEGIN
                        CREATE TABLE ServerLogs (
                            Id INT PRIMARY KEY IDENTITY(1,1),
                            IPAddress NVARCHAR(50),
                            Action NVARCHAR(MAX),
                            CreatedAt DATETIME DEFAULT GETDATE()
                        );
                    END;";
                using (var command = new SqlCommand(createLogsTable, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        // --- HÀM MỚI: LƯU LOG VÀO DATABASE ---
        public void SaveLog(string ip, string action)
        {
            try
            {
                using (var connection = DatabaseConnect.GetConnection())
                {
                    if (connection == null) return;
                    connection.Open();
                    string query = "INSERT INTO ServerLogs (IPAddress, Action, CreatedAt) VALUES (@ip, @action, GETDATE())";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ip", ip);
                        command.Parameters.AddWithValue("@action", action);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lưu Log DB: " + ex.Message);
            }
        }
        // -------------------------------------

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