using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace RemoteDesktop.Server.Networking
{
    public class ConnectionGuard
    {
        // Dictionary lưu trữ Client và Username tương ứng
        private readonly Dictionary<TcpClient, string> _connectedClients = new Dictionary<TcpClient, string>();
        private readonly object _lock = new object();

        // Thêm Client kèm Username vào danh sách quản lý
        public void AddClient(TcpClient client, string username)
        {
            lock (_lock)
            {
                if (!_connectedClients.ContainsKey(client))
                {
                    _connectedClients.Add(client, username);
                }
            }
        }

        // Xóa Client khi ngắt kết nối
        public void RemoveClient(TcpClient client)
        {
            lock (_lock)
            {
                if (_connectedClients.ContainsKey(client))
                {
                    _connectedClients.Remove(client);
                }
            }
        }

        // Kiểm tra xem Username đã có ai sử dụng chưa (Không phân biệt hoa thường)
        // Ví dụ: "Admin" và "admin" coi như là một
        public bool IsUsernameOnline(string username)
        {
            lock (_lock)
            {
                return _connectedClients.Values.Any(u => u.Equals(username, StringComparison.OrdinalIgnoreCase));
            }
        }

        public List<TcpClient> GetConnectedClients()
        {
            lock (_lock)
            {
                return _connectedClients.Keys.ToList();
            }
        }

        // Kiểm tra xem Client này có quyền điều khiển không (đã đăng nhập chưa)
        public bool IsController(TcpClient client)
        {
            lock (_lock)
            {
                return _connectedClients.ContainsKey(client);
            }
        }

        public void ClearAll()
        {
            lock (_lock)
            {
                foreach (var client in _connectedClients.Keys)
                {
                    try { client.Close(); } catch { }
                }
                _connectedClients.Clear();
            }
        }
    }
}