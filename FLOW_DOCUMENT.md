

Network Login System (TCP/IP & SQL Server)
==========================
Hệ thống quản lý đăng nhập tập trung sử dụng mô hình Client-Server, giao tiếp qua TCP Sockets, bảo mật mật khẩu và lưu trữ dữ liệu bằng SQL Server (Entity Framework Core).

🏗 Kiến trúc Hệ thống (N-Tier)
Server (Console): Trung tâm xử lý, kết nối Database, lắng nghe TCP Port 9000.

Client (WPF): Người dùng đăng ký/đăng nhập.

Admin (WPF): Quản trị viên duyệt yêu cầu đăng nhập, xem lịch sử, khóa tài khoản.

Core & Protocol: Thư viện dùng chung (DTOs, PacketType, Socket Helper).


Các Luồng Hoạt Động (Workflows)
=
Hệ thống hoạt động dựa trên việc trao đổi các gói tin DataPacket (JSON). Dưới đây là chi tiết từng kịch bản:

1/ Luồng Đăng Ký (Client Registration)
===================================
Người dùng tạo tài khoản mới.

Client: Nhập thông tin -> Đóng gói RegisterRequest -> Gửi gói tin Type: RegisterRequest.

Server:

Nhận gói tin -> Kiểm tra Username có trùng trong Database không?

Nếu trùng: Trả về IsSuccess = false, Message = "Username đã tồn tại".

Nếu không trùng:

Mã hóa mật khẩu (SHA256).

Lưu vào bảng Users với Role = "Client", IsActive = true.

Trả về IsSuccess = true.

Client: Nhận phản hồi -> Hiển thị thông báo -> Chuyển về màn hình Login.

2/ Luồng Đăng Nhập & Chờ Duyệt (Client Login Flow)
==
Client: Nhập User/Pass -> Gửi gói tin Type: LoginRequest.

Server (Kiểm tra sơ bộ):

Tìm User trong DB. Nếu không thấy -> Báo lỗi.

Kiểm tra Khóa: Nếu User.IsActive == false -> Trả về gói tin lỗi "Tài khoản bị khóa" -> Ngắt kết nối.

Kiểm tra Mật khẩu (Hash). Nếu sai -> Báo lỗi.

Server (Xử lý hàng chờ):

Nếu User/Pass đúng và là Client:

Lưu yêu cầu vào bảng LoginRequests (SQL).

Lưu Socket của Client vào Dictionary PendingClients.

Gửi thông báo Type: NewLoginRequest tới tất cả Admin đang Online.

Gửi phản hồi tạm thời cho Client: Role = "Pending".

Client: Nhận được Pending -> Chuyển sang chế độ "Lắng nghe thụ động" (ReceiveStringAsync) -> Hiện vòng quay chờ đợi.

3/Luồng Admin Đăng Nhập & Giám Sát
==
Admin kết nối để quản lý hệ thống.

Admin (Màn hình Login): Gửi LoginRequest.

Server:

Kiểm tra User/Pass đúng và Role == "Admin".

Thêm Socket vào danh sách ActiveAdmins.

Ghi ngay vào bảng LoginHistories.

Trả về IsSuccess = true.

Admin (Màn hình Dashboard):

Sau khi chuyển cảnh, Admin mở một kết nối Socket mới (do chuyển màn hình).

Gửi gói tin âm thầm Type: AdminReconnection để báo danh lại với Server.

Server: Nhận diện lại Admin -> Gửi ngay danh sách các Client đang chờ (SyncPendingRequests) về cho Admin.

4/Luồng Duyệt Yêu Cầu (Approve/Reject)
==
Admin quyết định cho phép hoặc từ chối Client đang chờ.

Admin: Bấm nút Approve (hoặc Reject).

Admin: Gửi gói tin Type: AdminDecision chứa { RequestId, IsApproved }.

Server:

Tìm yêu cầu trong DB theo ID -> Xóa yêu cầu khỏi DB.

Tìm Socket của Client đang chờ trong PendingClients.

Nếu Approve: Ghi vào bảng LoginHistories.

Gửi kết quả cuối cùng (LoginResponse) cho Client đó.

Client: Đang ở chế độ chờ -> Nhận được kết quả -> Hiển thị thông báo -> Vào Dashboard (hoặc thoát nếu bị từ chối).

5/Luồng Xem Lịch Sử (Login History)
==
Admin xem ai đã ra vào hệ thống.

Admin: Bấm menu "Lịch sử".

Admin: Gửi gói tin Type: GetLoginHistory.

Server:

Query bảng LoginHistories (Join với bảng Users để lấy tên).

Lấy 50 dòng mới nhất.

Map sang HistoryItemDto.

Gửi trả gói tin Type: LoginHistoryData.

Admin: Nhận JSON -> Deserialize -> Hiển thị lên ListView.

6/ Luồng Quản Lý Người Dùng (Lock/Unlock)
==
Admin khóa tài khoản vi phạm.

Admin: Bấm menu "Danh sách Clients".

Admin: Gửi gói tin Type: GetClientList.

Server: Trả về danh sách User có Role = "Client".

Admin: Bấm nút LOCK/UNLOCK trên một dòng.

Gửi gói tin Type: UpdateUserStatus chứa { UserId, NewStatus }.

Server:

Tìm User theo ID -> Cập nhật cột IsActive trong DB.

Lưu thay đổi (SaveChanges). (Lưu ý: Nếu User đang treo máy chờ duyệt mà bị khóa, lần đăng nhập sau sẽ bị chặn ngay ở bước 2 của Luồng Đăng Nhập).

Cấu trúc Database (SQL Server)
==
Users: Lưu thông tin tài khoản (Id, Username, PasswordHash, Role, IsActive...).

LoginRequests: Lưu hàng chờ tạm thời (Id, Username, IpAddress, Status...). Dữ liệu bảng này sẽ bị xóa sau khi Admin duyệt.

LoginHistories: Lưu nhật ký ra vào (Id, UserId, LoginTime, IsSuccess...).


Các loại Gói tin (PacketType Enum)
==
0 - LoginRequest		|	Yêu cầu đăng nhập (Gửi User/Pass)
1 - RegisterRequest		|	Yêu cầu đăng ký mới
2 - NewLoginRequest		|	Server báo cho Admin biết có người mới
3 - AdminDecision		|	Admin gửi quyết định (Duyệt/Hủy)
4 - AcceptLogin			|	(Tương tự AdminDecision - Legacy)
5 - AdminReconnection	|	Admin kết nối lại ở màn hình Dashboard
6 - HistoryUpdate		|	Server cập nhật lịch sử Real-time (Optional)
7 - GetLoginHistory		|	Admin xin danh sách lịch sử
8 - LoginHistoryData	|	Server trả dữ liệu lịch sử
9 - GetClientList		|	Admin xin danh sách người dùng
10 - ClientListData		|	Server trả danh sách người dùng
11 - UpdateUserStatus	|	Admin yêu cầu Khóa/Mở khóa User