NetworkLoginSystem (Solution)
=================================================
│
├── 1. NetworkLoginSystem.Core (Class Library)
│   ├── Entities/           # Chứa các class đại diện cho DB (User, LoginHistory)
│   ├── Enums/              # Các định nghĩa (UserRole, PacketType)
│   └── DTOs/               # Gói tin gửi qua mạng (LoginRequest, RegisterRequest)
│
├── 2. NetworkLoginSystem.Security (Class Library)
│   └── PasswordHasher.cs   # Chứa hàm mã hóa/giải mã (SHA256, MD5, hoặc BCrypt)
│
├── 3. NetworkLoginSystem.Data (Class Library)
│   ├── Context/            # Chứa DbContext (Entity Framework Core)
│   ├── Migrations/         # Các file tạo DB tự động (Code-first)
│   └── Repositories/       # Các hàm CRUD (Get, Add, Update User)
│
├── 4. NetworkLoginSystem.Protocol (Class Library)
│   ├── SocketClient.cs     # Class hỗ trợ gửi/nhận dữ liệu phía Client
│   └── PacketHelper.cs     # Hỗ trợ đóng gói/giải nén dữ liệu JSON/Binary
│
├── 5. NetworkLoginSystem.Server (Console App)
│   ├── Program.cs          # Chạy Server
│   ├── TcpServer.cs        # Lắng nghe kết nối (TcpListener)
│   └── ClientHandler.cs    # Xử lý logic cho từng Client kết nối vào
│
├── 6. NetworkLoginSystem.Client (WPF App)
│   ├── Views/
│   │   ├── LoginWindow.xaml
│   │   └── RegisterWindow.xaml
│   └── ViewModels/         # Code xử lý logic giao diện
│
└── 7. NetworkLoginSystem.Admin (WPF App)
    ├── Views/
    │   ├── AdminLoginWindow.xaml
    │   └── DashboardWindow.xaml (Quản lý danh sách user đang online/offline)
    └── ViewModels/
 

+++
=========================================================================================
1. NetworkLoginSystem.Core (Thư viện dùng chung)
Loại: Class Library (.NET Core / .NET 8).

Mục đích: Đây là "ngôn ngữ chung". Cả Server, Client và Admin đều cần biết User trông như thế nào, hoặc gói tin Login gồm những trường nào.

Nội dung: Chứa các class User, Role, các Enum PacketType (Login, Register, Success, Fail).

2. NetworkLoginSystem.Security (Bảo mật)
Loại: Class Library.

Mục đích: Tách biệt phần mã hóa để dễ nâng cấp sau này.

Nội dung: Class CryptoHelper chứa hàm HashPassword(string pass) và VerifyPassword(string pass, string hash).

3. NetworkLoginSystem.Data (Cơ sở dữ liệu)
Loại: Class Library.

Mục đích: Chịu trách nhiệm giao tiếp với SQL Server. Project này sẽ cài thư viện Microsoft.EntityFrameworkCore.

Nội dung:

AppDbContext: Cấu hình kết nối DB.

UserRepository: Hàm CheckLogin(username, password), CreateUser(...).

4. NetworkLoginSystem.Protocol (Giao thức mạng)
Loại: Class Library.

Mục đích: Giúp đóng gói dữ liệu để gửi qua dây mạng (TCP).

Nội dung: Chứa logic chuyển đổi từ Object C# -> Byte Array (để gửi đi) và ngược lại. Giúp code ở Client và Server gọn gàng hơn, không phải viết lại lệnh Stream.Write nhiều lần.

5. NetworkLoginSystem.Server (Máy chủ)
Loại: Console Application (Ứng dụng màn hình đen).

Mục đích: Trung tâm xử lý. Nó chạy 24/24.

Luồng chạy:

Mở cổng (Port) lắng nghe.

Khi có Client kết nối -> Tạo luồng xử lý riêng.

Nhận gói tin -> Giải mã -> Gọi xuống tầng Data để kiểm tra DB -> Trả kết quả về Client.

Tham chiếu: Cần Add Reference tới: Core, Security, Data, Protocol.

6. NetworkLoginSystem.Client (Ứng dụng Người dùng)
Loại: WPF Application.

Mục đích: Cho người dùng bình thường đăng ký/đăng nhập.

Luồng chạy: Nhập user/pass -> Gọi Protocol đóng gói -> Gửi lên Server -> Chờ phản hồi -> Hiển thị thông báo.

Tham chiếu: Cần Add Reference tới: Core, Protocol (Không cần Data hay Security vì Client không được chạm trực tiếp vào DB).

7. NetworkLoginSystem.Admin (Ứng dụng Quản trị)
Loại: WPF Application.

Mục đích: Cho Admin đăng nhập và xem ai đang online.

Tham chiếu: Tương tự Client, tham chiếu tới Core, Protocol.

CAI DAT +++
=========================================================================================
??
 	Chuột phải vào project Data -> Manage NuGet Packages -> Cài 2 cái này:

Microsoft.EntityFrameworkCore.SqlServer

Microsoft.EntityFrameworkCore.Tools (để chạy lệnh tạo DB)
[cài đúng phiên bản .net của máy .Data]

Chuột phải vào project NetworkLoginSystem.Server -> chọn Manage NuGet Packages.

Tìm và cài đặt gói: Microsoft.EntityFrameworkCore.Design. (Nhớ chọn tab Browse để tìm).

cài gói: Microsoft.Extensions.Configuration.Json.
cài đặt gói: Microsoft.Extensions.Configuration.Binder
--> làm tương tự với project admin và client 

CHỨC NĂNG CỦA CÁC HÀM SẼ ĐƯỢC NODE TRONG CODE
==================

