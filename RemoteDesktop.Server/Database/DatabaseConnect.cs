using Microsoft.Data.SqlClient;
using System;

namespace RemoteDesktop.Server.Database
{
    public static class DatabaseConnect
    {

        private static readonly string _connectionString = @"Server=.\SQLEXPRESS;Database=RemoteDesktopDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            try
            {
                return new SqlConnection(_connectionString);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi tạo kết nối: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Thuộc tính để lấy chuỗi kết nối nếu cần cho SqlDataAdapter
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}