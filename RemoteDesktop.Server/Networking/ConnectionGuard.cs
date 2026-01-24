using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace RemoteDesktop.Server.Networking
{
    public class ConnectionGuard
    {
        // Danh sách chứa tất cả mọi người (người đầu tiên là Admin)
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private readonly object _lock = new object();

        /// <summary>
        /// Thêm client mới vào danh sách (luôn chấp nhận)
        /// </summary>
        public void AddClient(TcpClient newClient)
        {
            lock (_lock)
            {
                _clients.Add(newClient);
            }
        }

        /// <summary>
        /// Xóa client khi họ thoát
        /// </summary>
        public void RemoveClient(TcpClient client)
        {
            lock (_lock)
            {
                if (_clients.Contains(client))
                {
                    _clients.Remove(client);
                }
            }
        }

        /// <summary>
        /// [QUAN TRỌNG] Kiểm tra xem client này có quyền điều khiển không?
        /// Logic: Chỉ người đứng đầu danh sách (Index 0) mới được quyền.
        /// </summary>
        public bool IsController(TcpClient client)
        {
            lock (_lock)
            {
                // Nếu danh sách có người VÀ client này là người đầu tiên
                return _clients.Count > 0 && _clients[0] == client;
            }
        }

        public List<TcpClient> GetConnectedClients()
        {
            lock (_lock)
            {
                return _clients.ToList();
            }
        }

        public void ClearAll()
        {
            lock (_lock)
            {
                foreach (var c in _clients) c.Close();
                _clients.Clear();
            }
        }
    }
}