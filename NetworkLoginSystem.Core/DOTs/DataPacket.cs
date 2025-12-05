using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Core.DOTs
{
    /// <summary>
    /// dùng để đóng gói dữ liệu và type của nó
    /// </summary>
    public class DataPacket
    {
        public PacketType Type { get; set; } // Enum: LoginRequest, RegisterRequest
        public string Data { get; set; }     // Nội dung JSON bên trong
    }
}
