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
        LoginRequest, // yêu cầu đăng nhập
        LoginResponse,// phản hồi đăng nhập
        RegisterRequest,// y/c đăng kí
        RegisterResponse,// phản hồi đăng kí
        AcceptLogin,// chấp nhận đăng nhập
        NewLoginRequest,// y/c đăng nhập mới
        AdminDecision,
        AdminReconnection,// admin kết nối lại sau khi đăng nhập thành công
        GetLoginHistory,    // y/c lấy ds lịch sử đăng nhập
        LoginHistoryData, // đóng gói gửi cho admin 
        GetClientList,      // Admin xin danh sách Client
        ClientListData,     // Server trả danh sách
        UpdateUserStatus,    // Admin yêu cầu Khóa/Mở khóa
        DeleteUser // xóa tk
    }
}
