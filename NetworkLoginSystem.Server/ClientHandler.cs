using NetworkLoginSystem.Core.DOTs;
using NetworkLoginSystem.Data.Context;
using NetworkLoginSystem.Security;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Server
{
    // nhan xu ly rieng cho moi client
    public class ClientHandler
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public ClientHandler(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
        }

        // hàm kiểm tra xem client yêu cầu gì dựa vào PacketType
        public async Task RunAsync()
        {
            try
            {
                // Tạo bộ đệm để chứa dữ liệu
                byte[] buffer = new byte[8192];
                int byteRead;

                // vong lap vo tan
                while ((byteRead = await _stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    // nhan json tu client
                    string jsonString = Encoding.UTF8.GetString(buffer, 0, byteRead);
                    Console.WriteLine($"[REQUEST]: {jsonString}");

                    // check kieu y/c
                    var packet = JsonSerializer.Deserialize<DataPacket>(jsonString);

                    object responseData = null;
                    // phan loai 
                    switch (packet.Type)
                    {
                        case PacketType.LoginRequest:
                            Console.WriteLine("-> Xu ly LOGIN");
                            responseData = await ProcessLoginAsync(packet.Data);
                            break;

                        case PacketType.RegisterRequest:
                            Console.WriteLine("-> Xu ly REGISTER");
                            responseData = await ProcessRegisterAsync(packet.Data);
                            break;
                        case PacketType.AcceptLogin:
                            Console.WriteLine("-> Admin gui quyet dinh");
                            await ProcessAdminDecisionAsync(packet.Data);
                            break;
                        case PacketType.AdminReconnection:
                            Console.WriteLine("-> Admin Re-connected (Background)");
                            await ProcessAdminReconnectAsync(packet.Data);
                            break;
                        case PacketType.GetLoginHistory:
                            Console.WriteLine("-> Admin yeu cau lay lich su DB");
                            await SendHistoryListToAdmin();
                            break;
                        case PacketType.GetClientList:
                            await SendClientListToAdmin();
                            break;
                        case PacketType.UpdateUserStatus:
                            await ProcessUpdateUserStatus(packet.Data);
                            break;
                        case PacketType.DeleteUser:
                            Console.WriteLine("-> Admin yeu cau XOA user");
                            await ProcessDeleteUser(packet.Data);
                            break;
                        default:
                            Console.WriteLine("-> Loai goi tin khong ho tro");
                            break;
                    }

                    if (responseData != null)
                    {
                        // Chuyển kết quả thành JSON để gửi lại
                        string jsonResponse = JsonSerializer.Serialize(responseData);
                        byte[] replyBytes = Encoding.UTF8.GetBytes(jsonResponse);

                        await _stream.WriteAsync(replyBytes, 0, replyBytes.Length);
                        Console.WriteLine($"[RESPONSE]: {jsonResponse}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"client ngat ket noi {ex.Message}");
            }
            finally
            {
                // Client thoát thì đóng kết nối
                Console.WriteLine("finally");
            }
        }

        // Hàm Admin kết nối lại khi đã đăng nhâpj được vào trang admin
        private async Task ProcessAdminReconnectAsync(string json)
        {
            try
            {
                // giai mã gói tin 
                var request = JsonSerializer.Deserialize<LoginRequest>(json);

                if (!ConnectionManager.ActiveAdmins.Contains(_client))
                {
                    ConnectionManager.ActiveAdmins.Add(_client);
                }

                // nếu kết nói lại được sẽ gửi danh sách các client đang chờ duyệt đăng nhập
                _ = Task.Run(() => SyncPendingRequestsToNewAdmin(_client));

                Console.WriteLine($"+++ ADMIN {request.Username} DA KET NOI LAI (MainWindow) +++");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Loi Admin Reconnect: " + ex.Message);
            }
        }

        // ham dang ki
        private async Task<LoginResponse> ProcessRegisterAsync(string json)
        {
            try
            {
                var request = JsonSerializer.Deserialize<RegisterRequest>(json);

                using (var db = new AppDbContext(Program.ConnectionString))
                {
                    // 1. Kiểm tra trùng Username
                    bool exists = await db.Users.AnyAsync(u => u.Username == request.Username);
                    if (exists)
                    {
                        return new LoginResponse { IsSuccess = false, Message = "Username da ton tai!" };
                    }

                    // 2. Tạo User mới
                    var newUser = new Core.Entities.User
                    {
                        Username = request.Username,
                        //Hash mật khẩu trước khi lưu
                        PasswordHash = PasswordHasher.HashPassword(request.Password),
                        Role = "Client", // Mặc định đăng ký là Client
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    db.Users.Add(newUser);
                    await db.SaveChangesAsync();

                    return new LoginResponse { IsSuccess = true, Message = "Dang ky thanh cong!" };
                }
            }
            catch (Exception ex)
            {
                return new LoginResponse { IsSuccess = false, Message = "Loi DB: " + ex.Message };
            }
        }

        // ham dang nhap
        private async Task<LoginResponse> ProcessLoginAsync(string json)
        {
            try
            {
                // Dịch chuỗi JSON thành Object
                var request = JsonSerializer.Deserialize<LoginRequest>(json);

                if (request == null || string.IsNullOrEmpty(request.Username))
                {
                    return new LoginResponse { IsSuccess = false, Message = "Du lieu khong hop le" };
                }

                //Mở Database để kiểm tra
                using (var db = new AppDbContext(Program.ConnectionString))
                {
                    // Tìm user theo Username
                    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

                    // C. Kiểm tra User có tồn tại không
                    if (user == null)
                    {
                        return new LoginResponse { IsSuccess = false, Message = "Tai khoan khong ton tai!" };
                    }
                    if (!user.IsActive)
                    {
                        Console.WriteLine($"-> Tu choi dang nhap: User {user.Username} da bi khoa.");
                        return new LoginResponse
                        {
                            IsSuccess = false,
                            Message = "Tai khoan cua ban da bi KHOA!",
                            Role = "Locked" // Đánh dấu để Client biết
                        };
                    }

                    // Kiểm tra Mật khẩu (Hash pass gửi lên so với Hash trong DB)
                    bool isPassOk = PasswordHasher.VerifyPassword(request.Password, user.PasswordHash);

                    if (isPassOk)
                    {
                        if (user.Role == "Admin")
                        {
                            ConnectionManager.ActiveAdmins.Add(_client);
                            Console.WriteLine("+++ Admin da ket noi +++");
                            // lich su
                            var history = new Core.Entities.LoginHistory
                            {
                                UserId = user.Id,
                                LoginTime = DateTime.Now,
                                IpAddress = _client.Client.RemoteEndPoint?.ToString(),
                                DeviceInfo = "Admin",
                                IsSuccess = true
                            };
                            db.LoginHistories.Add(history);
                            await db.SaveChangesAsync();

                            return new LoginResponse
                            {
                                IsSuccess = true,
                                Message = "Chao Admin",
                                Role = "Admin"
                            };
                        }
                        else
                        {
                            // Lưu yêu cầu vào Database
                            var loginReq = new Core.Entities.LoginRequest
                            {
                                Username = user.Username,
                                IpAddress = _client.Client.RemoteEndPoint?.ToString(),
                                Status = 0 // Pending
                            };
                            db.loginRequest.Add(loginReq);
                            await db.SaveChangesAsync();

                            // Lưu Socket của Client này lại để chờ trả lời sau
                            ConnectionManager.PendingClients[loginReq.Id] = _client;

                            // Báo tin cho admin
                            try
                            {

                                var requestInfo = new
                                {
                                    Id = loginReq.Id,              // ID để Admin biết duyệt cái nào
                                    Username = user.Username,      // Tên để hiện lên
                                    IpAddress = _client.Client.RemoteEndPoint?.ToString(),
                                    RequestTime = DateTime.Now.ToString("HH:mm:ss")
                                };
                                var packet = new DataPacket
                                {
                                    Type = PacketType.NewLoginRequest,
                                    Data = JsonSerializer.Serialize(requestInfo)
                                };

                                byte[] dataToSend = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packet));

                                foreach (var adminSocket in ConnectionManager.ActiveAdmins.ToList())
                                {
                                    if (adminSocket.Connected)
                                    {
                                        await adminSocket.GetStream().WriteAsync(dataToSend, 0, dataToSend.Length);
                                    }
                                }
                                Console.WriteLine($"-> Da gui yeu cau cho Admin.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Loi gui tin cho Admin: " + ex.Message);
                            }

                            //Trả lời tạm thời cho Client (để nó hiện Waiting)
                            return new LoginResponse
                            {
                                IsSuccess = false, // Chưa thành công ngay
                                Message = "WAITING:Tai khoan dung, vui long cho Admin duyet...",
                                Role = "Pending" // Đánh dấu trạng thái chờ
                            };
                        }
                    }
                    return new LoginResponse
                    {
                        IsSuccess = false,
                        Message = "Sai mat khau",
                        Role = ""
                    };
                }
            }
            catch (Exception ex)
            {
                string errorDetails = ex.Message;

                // Lấy lỗi gốc rễ từ bên trong
                if (ex.InnerException != null)
                {
                    errorDetails += " || INNER: " + ex.InnerException.Message;
                }

                Console.WriteLine($"❌ LOI DB CHI TIET: {errorDetails}"); // In ra màn hình đen Server để đọc

                return new LoginResponse
                {
                    IsSuccess = false,
                    Message = "Loi Server: " + errorDetails // Gửi về Client để debug tạm
                };
            }
        }

        // hứng du lieu tu admin (chap nhan / huy)
        private async Task ProcessAdminDecisionAsync(string json)
        {
            try
            {
                // 1. Dịch gói tin quyết định
                var decision = JsonSerializer.Deserialize<AdminDecisionPacket>(json);

                using (var db = new AppDbContext(Program.ConnectionString))
                {

                    // 2. Cập nhật Database
                    var req = await db.loginRequest.FindAsync(decision.RequestId);
                    if (req != null)
                    {
                        db.loginRequest.Remove(req);
                        await db.SaveChangesAsync();
                        Console.WriteLine($"-> Da xoa yeu cau ID {decision.RequestId} khoi DB.");
                    }
                    // 3. Tìm Client đang chờ 
                    if (ConnectionManager.PendingClients.TryGetValue(decision.RequestId, out TcpClient clientSocket))
                    {

                        if (clientSocket.Connected)
                        {

                            if (decision.IsApproved)
                            {
                                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);

                                if (user != null)
                                {
                                    var history = new Core.Entities.LoginHistory
                                    {
                                        UserId = user.Id,
                                        LoginTime = DateTime.Now,
                                        IpAddress = req.IpAddress,
                                        DeviceInfo = "Client",
                                        IsSuccess = true
                                    };
                                    db.LoginHistories.Add(history);
                                    await db.SaveChangesAsync();
                                    Console.WriteLine($"-> Da luu lich su dang nhap cho {user.Username}");
                                }
                            }
                            var response = new LoginResponse
                            {
                                IsSuccess = decision.IsApproved,
                                Message = decision.IsApproved ? "Admin da dong y!" : "Admin da tu choi!",
                                Role = decision.IsApproved ? "Client" : "Rejected",
                            };

                            string jsonRes = JsonSerializer.Serialize(response);
                            byte[] data = Encoding.UTF8.GetBytes(jsonRes);

                            // Gửi kết quả cuối cùng cho Client
                            await clientSocket.GetStream().WriteAsync(data, 0, data.Length);
                            Console.WriteLine($"-> Da gui ket qua cho Client (ID: {decision.RequestId})");
                        }

                        // 4. Xóa khỏi danh sách chờ
                        ConnectionManager.PendingClients.Remove(decision.RequestId);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Loi xu ly Admin Decision: {ex.Message}");
            }
        }

        // Hàm gửi toàn bộ danh sách chờ cho Admin vừa kết nối
        private async Task SyncPendingRequestsToNewAdmin(TcpClient adminClient)
        {
            try
            {
                using (var db = new AppDbContext(Program.ConnectionString))
                {
                    // Lấy tất cả yêu cầu đang có trong DB (Status = 0)
                    var pendingList = await db.loginRequest.Where(x => x.Status == 0).ToListAsync();

                    foreach (var req in pendingList)
                    {
                        // Kiểm tra xem Socket của Client này còn sống không
                        if (ConnectionManager.PendingClients.ContainsKey(req.Id))
                        {
                            // Tạo dữ liệu hiển thị
                            var itemDisplay = new
                            {
                                Id = req.Id,
                                Username = req.Username,
                                IpAddress = req.IpAddress,
                                RequestTime = req.RequestTime.ToString("HH:mm:ss")
                            };

                            // Đóng gói
                            var packet = new DataPacket
                            {
                                Type = PacketType.NewLoginRequest,
                                Data = JsonSerializer.Serialize(itemDisplay)
                            };

                            byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packet));

                            // Gửi riêng cho Admin này thôi
                            await adminClient.GetStream().WriteAsync(data, 0, data.Length);
                            await Task.Delay(50);
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Loi sync admin: " + ex.Message); }
        }

        // ham lay lich su dang nhap 
        private async Task SendHistoryListToAdmin()
        {
            Console.WriteLine("--- BAT DAU TRUY VAN HISTORY ---");
            try
            {
                using (var db = new AppDbContext(Program.ConnectionString))
                {
                    // 1. Kiểm tra số lượng thô
                    int rawCount = await db.LoginHistories.CountAsync();
                    Console.WriteLine($"[DEBUG] Tong so dong trong bang LoginHistories: {rawCount}");

                    if (rawCount == 0)
                    {
                        Console.WriteLine("[DEBUG] Bang rong -> Gui danh sach rong ve Admin.");
                        // Vẫn gửi gói tin rỗng để Admin biết mà xóa loading
                        var emptyPacket = new DataPacket { Type = PacketType.LoginHistoryData, Data = "[]" };
                        byte[] emptyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(emptyPacket));
                        await _stream.WriteAsync(emptyBytes, 0, emptyBytes.Length);
                        return;
                    }

                    // 2. Truy vấn và Mapping
                    var historyList = await db.LoginHistories
                        .Include(h => h.User) // Join bảng User
                        .OrderByDescending(h => h.LoginTime)
                        .Take(50)
                        .Select(h => new HistoryItemDto
                        {
                            // Dùng toán tử ? để tránh lỗi Null nếu User bị xóa
                            Username = (h.User != null) ? h.User.Username : "Unknown User",
                            Action = "Login",
                            Time = h.LoginTime.ToString("dd/MM/yyyy HH:mm:ss"),
                            Status = h.IsSuccess ? "Success" : "Failed",
                            IpAddress = h.IpAddress ?? "N/A"
                        })
                        .ToListAsync();

                    Console.WriteLine($"[DEBUG] Da lay duoc {historyList.Count} dong va map sang DTO.");

                    string jsonData = JsonSerializer.Serialize(historyList);
                    //Console.WriteLine($"[DEBUG] JSON gui di: {jsonData}");

                    // 4. Đóng gói
                    var packet = new DataPacket
                    {
                        Type = PacketType.LoginHistoryData,
                        Data = jsonData
                    };

                    string finalJson = JsonSerializer.Serialize(packet);
                    byte[] data = Encoding.UTF8.GetBytes(finalJson);

                    await _stream.WriteAsync(data, 0, data.Length);
                    //Console.WriteLine($"[SUCCESS] Da gui {data.Length} bytes ve Admin.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ LOI SERVER (SendHistory): {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
            Console.WriteLine("--------------------------------");
        }

        // ---GỬI DANH SÁCH CLIENT ---
        private async Task SendClientListToAdmin()
        {
            try
            {
                using (var db = new AppDbContext(Program.ConnectionString))
                {
                    // Chỉ lấy những người có Role là "Client"
                    var clients = await db.Users
                        .Where(u => u.Role == "Client")
                        .Select(u => new ClientItemDto
                        {
                            Id = u.Id,
                            Username = u.Username,
                            IsActive = u.IsActive
                        })
                        .ToListAsync();

                    var packet = new DataPacket
                    {
                        Type = PacketType.ClientListData,
                        Data = JsonSerializer.Serialize(clients)
                    };

                    byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(packet));
                    await _stream.WriteAsync(data, 0, data.Length);
                }
            }
            catch (Exception ex) { Console.WriteLine("Loi lay danh sach Client: " + ex.Message); }
        }

        // ---CẬP NHẬT TRẠNG THÁI KHÓA/MỞ ---
        private async Task ProcessUpdateUserStatus(string json)
        {
            try
            {
                var updateReq = JsonSerializer.Deserialize<UpdateStatusDto>(json);

                using (var db = new AppDbContext(Program.ConnectionString))
                {
                    var user = await db.Users.FindAsync(updateReq.UserId);
                    if (user != null)
                    {
                        user.IsActive = updateReq.NewStatus;
                        await db.SaveChangesAsync();
                        Console.WriteLine($"-> Da cap nhat User {user.Username} thanh {(user.IsActive ? "Active" : "Locked")}");

                        // (Tùy chọn) Có thể gửi tin nhắn báo lại cho Admin là "Thành công" nếu muốn
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Loi Update Status: " + ex.Message); }
        }

        // XÓA TÀI KHOẢN
        private async Task ProcessDeleteUser(string json)
        {
            try
            {
                var deleteDto = JsonSerializer.Deserialize<DeleteUserDto>(json);

                using (var db = new AppDbContext(Program.ConnectionString))
                {
                    var user = await db.Users.FindAsync(deleteDto.UserId);
                    if (user != null)
                    {
                        // EF Core thường sẽ tự xóa các bảng con liên quan (History) nếu cấu hình đúng
                        db.Users.Remove(user);
                        await db.SaveChangesAsync();
                        Console.WriteLine($"-> DA XOA VINH VIEN USER: {user.Username}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loi Xoa User: " + ex.Message);
                // Nếu lỗi do khóa ngoại (Foreign Key), bạn cần xóa LoginHistories của user này trước
            }
        }
    }
}
