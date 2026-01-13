using Microsoft.Data.SqlClient;
using System;
using RemoteDesktop.Server.Database;
using System.Data;

namespace RemoteDesktop.Server.Database
{
    public class DatabaseManager
    {
        // Thay đổi chuỗi kết nối này cho phù hợp với SQL Server của bạn
        // Nếu dùng Windows Authentication: "Server=.;Database=RemoteDesktopDB;Trusted_Connection=True;TrustServerCertificate=True;"
        private string connectionString = @"Server=.\SQLEXPRESS;Database=RemoteDesktopDB;User Id=sa;Password=your_password;TrustServerCertificate=True;";

        public void InitializeDatabase()
        {
            try
            {
                // SQL Server không tự tạo file .mdf như SQLite, nên bạn cần tạo Database thủ công trước 
                // hoặc dùng code kiểm tra và tạo bảng như sau:
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string createTableQuery = @"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                        BEGIN
                            CREATE TABLE Users (
                                Id INT PRIMARY KEY IDENTITY(1,1),
                                Username NVARCHAR(50) NOT NULL UNIQUE,
                                Password NVARCHAR(255) NOT NULL
                            );
                            
                            -- Thêm tài khoản mẫu
                            INSERT INTO Users (Username, Password) VALUES ('admin', '123456');
                        END";

                    using (var command = new SqlCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi kết nối SQL Server
                Console.WriteLine("Lỗi Database: " + ex.Message);
            }
        }

        public bool ValidateUser(string username, string password)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Users WHERE Username = @user AND Password = @pass";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@user", username);
                        command.Parameters.AddWithValue("@pass", password);

                        int count = (int)command.ExecuteScalar();
                        return count > 0;
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