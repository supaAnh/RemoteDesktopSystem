using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace RemoteDesktop.Server.Database
{
    public class DatabaseManager
    {
        // Chuỗi kết nối dự phòng (cho các hàm User cũ)
        private string connectionString = @"Server=127.0.0.1,1433;Database=RemoteDesktopDB;User Id=sa;Password=@Supanh123;TrustServerCertificate=True;";

        // 1. LƯU ẢNH RECORD
        public void SaveScreenRecord(string sessionID, string ip, byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0) return;
            try
            {
                using (var connection = DatabaseConnect.GetConnection())
                {
                    connection.Open();
                    string query = "INSERT INTO ServerLogs (SessionID, IPAddress, Action, ImageContent, CreatedAt) VALUES (@sid, @ip, '[Screen Record]', @img, GETDATE())";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@sid", sessionID ?? "UNKNOWN");
                        command.Parameters.AddWithValue("@ip", ip);
                        command.Parameters.Add("@img", SqlDbType.VarBinary, -1).Value = imageBytes;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        // 2. LƯU LOG HOẠT ĐỘNG
        public void SaveLog(string sessionID, string ip, string action)
        {
            try
            {
                using (var connection = DatabaseConnect.GetConnection())
                {
                    connection.Open();
                    string query = "INSERT INTO ServerLogs (SessionID, IPAddress, Action, CreatedAt) VALUES (@sid, @ip, @action, GETDATE())";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@sid", sessionID ?? "SYSTEM");
                        command.Parameters.AddWithValue("@ip", ip);
                        command.Parameters.AddWithValue("@action", action);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        // 3. LẤY DANH SÁCH PHIÊN (Cho Form History)
        public DataTable GetSessionList()
        {
            DataTable dt = new DataTable();
            try
            {
                using (var conn = DatabaseConnect.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT SessionID, MIN(CreatedAt) as StartTime, MAX(IPAddress) as IP 
                        FROM ServerLogs 
                        WHERE SessionID IS NOT NULL 
                        GROUP BY SessionID 
                        ORDER BY StartTime DESC";
                    using (var adapter = new SqlDataAdapter(query, conn)) adapter.Fill(dt);
                }
            }
            catch { }
            return dt;
        }

        // 4. LẤY ẢNH THEO PHIÊN
        public DataTable GetRecordsBySession(string sessionID)
        {
            DataTable dt = new DataTable();
            try
            {
                using (var conn = DatabaseConnect.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT Id, CreatedAt FROM ServerLogs WHERE SessionID = @sid AND ImageContent IS NOT NULL ORDER BY CreatedAt DESC";
                    using (var adapter = new SqlDataAdapter(query, conn))
                    {
                        adapter.SelectCommand.Parameters.AddWithValue("@sid", sessionID);
                        adapter.Fill(dt);
                    }
                }
            }
            catch { }
            return dt;
        }

        // 5. LẤY 1 TẤM ẢNH CỤ THỂ
        public byte[] GetRecordImage(int id)
        {
            try
            {
                using (var conn = DatabaseConnect.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT ImageContent FROM ServerLogs WHERE Id = @id";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value) return (byte[])result;
                    }
                }
            }
            catch { }
            return null;
        }

        // --- CÁC HÀM QUẢN LÝ USER (ĐÃ KHÔI PHỤC ĐẦY ĐỦ) ---

        public void InitializeDatabase()
        {
            // Hàm này đã chạy ở frmConnect, giữ nguyên logic cũ hoặc để trống nếu đã chạy SQL tay
        }

        public bool ValidateUser(string username, string password)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
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
            catch { return false; }
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
            catch { return false; }
        }

        public DataTable GetPendingUsers()
        {
            DataTable dt = new DataTable();
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var adapter = new SqlDataAdapter("SELECT Username, CreatedAt FROM Users WHERE Status = 0", connection))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch { }
            return dt;
        }

        public bool ApproveUser(string username, int status)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("UPDATE Users SET Status = @status WHERE Username = @user", connection))
                    {
                        command.Parameters.AddWithValue("@status", status);
                        command.Parameters.AddWithValue("@user", username);
                        return command.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch { return false; }
        }
    }
}