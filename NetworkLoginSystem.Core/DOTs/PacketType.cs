using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Core.DOTs
{
    // Để sau này Server biết Client đang muốn làm gì
    public enum PacketType
    {
        LoginRequest,
        LoginResponse,
        RegisterRequest,
        RegisterResponse,
        AcceptLogin,
        NewLoginRequest,
        AdminDecision,
        AdminReconnection,
        Message // Dành cho chat hoặc thông báo
    }
}
