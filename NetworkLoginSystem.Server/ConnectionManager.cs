using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Server
{
    // kiem tra xem adim hay client
    public static class ConnectionManager
    {
        // Danh sách các Admin đang Online
        public static List<TcpClient> ActiveAdmins = new List<TcpClient>();

        // Danh sách Client đang chờ duyệt (Key: LoginRequestId, Value: Socket của Client đó)
        public static Dictionary<int, TcpClient> PendingClients = new Dictionary<int, TcpClient>();
    }
}
