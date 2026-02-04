using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace RemoteDesktop.Server.Networking
{
    public class ConnectionGuard
    {
        // [THAY ĐỔI 1] Sử dụng List để duy trì thứ tự kết nối (Hàng đợi)
        // Người vào trước sẽ nằm ở đầu danh sách (Index 0)
        private readonly List<TcpClient> _connectionQueue = new List<TcpClient>();

        // Dictionary để lưu Username phục vụ hiển thị và kiểm tra đăng nhập
        private readonly Dictionary<TcpClient, string> _clientUsernames = new Dictionary<TcpClient, string>();

        private readonly object _lock = new object();

        // Thêm Client vào hệ thống
        public void AddClient(TcpClient client, string username)
        {
            lock (_lock)
            {
                if (!_clientUsernames.ContainsKey(client))
                {
                    _connectionQueue.Add(client); // Thêm vào cuối hàng đợi
                    _clientUsernames.Add(client, username);
                }
            }
        }

        // Xóa Client khi ngắt kết nối
        public void RemoveClient(TcpClient client)
        {
            lock (_lock)
            {
                if (_clientUsernames.ContainsKey(client))
                {
                    // Khi người đứng đầu (người điều khiển) thoát, họ bị xóa khỏi List.
                    // Người đứng thứ 2 sẽ tự động được đẩy lên vị trí 0 -> Trở thành người điều khiển mới.
                    _connectionQueue.Remove(client);
                    _clientUsernames.Remove(client);
                }
            }
        }

        // Kiểm tra Username có đang online không
        public bool IsUsernameOnline(string username)
        {
            lock (_lock)
            {
                return _clientUsernames.Values.Any(u => u.Equals(username, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Lấy danh sách tất cả Client để gửi hình ảnh màn hình (Broadcast)
        // Ai cũng nhận được hình ảnh, kể cả người xem
        public List<TcpClient> GetConnectedClients()
        {
            lock (_lock)
            {
                return _connectionQueue.ToList();
            }
        }

        // [QUAN TRỌNG] Logic kiểm tra quyền điều khiển
        public bool IsController(TcpClient client)
        {
            lock (_lock)
            {
                // Chỉ người đứng đầu hàng đợi (kết nối sớm nhất) mới được quyền điều khiển
                if (_connectionQueue.Count > 0)
                {
                    return _connectionQueue[0] == client;
                }
                return false;
            }
        }

        public void ClearAll()
        {
            lock (_lock)
            {
                foreach (var client in _connectionQueue)
                {
                    try { client.Close(); } catch { }
                }
                _connectionQueue.Clear();
                _clientUsernames.Clear();
            }
        }
    }
}