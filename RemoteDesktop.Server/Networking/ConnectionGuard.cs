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
        /// [CẬP NHẬT] Xóa client và trả về người điều khiển mới (nếu có sự chuyển giao).
        /// Trả về null nếu không có sự thay đổi Admin.
        /// </summary>
        public TcpClient? RemoveClient(TcpClient client)
        {
            lock (_lock)
            {
                int index = _clients.IndexOf(client);
                if (index != -1)
                {
                    _clients.Remove(client);

                    // Nếu người vừa bị xóa là Admin (đứng đầu list - index 0) 
                    // VÀ sau khi xóa vẫn còn người khác trong list (người thứ 2 lên thế chỗ)
                    if (index == 0 && _clients.Count > 0)
                    {
                        return _clients[0]; // Trả về tân Admin
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra xem client này có quyền điều khiển không?
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