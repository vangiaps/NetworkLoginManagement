using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace NetworkLoginSystem.Protocol
{
    /// <summary>
    /// gửi , nhận dữ liệu dùng cho cả admin và client
    /// </summary>
    public class SocketClient
    {
        private TcpClient _client;
        private NetworkStream _stream;

        // ket noi toi server
        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);
                _stream = _client.GetStream();
                return true;
            }
            catch
            {
                return false;
            }
        }
        // gui du lieu va cho phan hoi
        public async Task<string> SendAndReceiveAsync(string message)
        {
            if(_client == null|| !_client.Connected)
            {
                return "loi: khong ket noi";
            }
            try
            {
                // gui
                byte[] dataToSend = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(dataToSend, 0, dataToSend.Length);

                //nhan
                byte[] buffer = new byte[65536];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                return response;
            }
            catch(Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // ham chi nhan
        public async Task<string> ReceiveOnlyAsync()
        {
            try
            {
                if (_client == null || !_client.Connected) return null;

                // 1. Tăng bộ đệm lên 64KB (đủ chứa 65536 ký tự một lần hốt)
                // 9943 bytes sẽ nằm gọn trong này, không bị cắt.
                byte[] buffer = new byte[65536];

                // 2. Sử dụng StringBuilder để nối chuỗi (Phòng trường hợp dữ liệu lớn hơn 64KB)
                StringBuilder messageBuilder = new StringBuilder();
                int bytesRead = 0;

                do
                {
                    // Đọc dữ liệu từ dòng chảy (Stream)
                    bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0) return null; // Mất kết nối

                    // Nối phần vừa đọc được vào cục tổng
                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                    // Mẹo nhỏ: Nghỉ 1 tích tắc để đảm bảo toàn bộ gói tin đã về đến nơi
                    // (Giúp thuộc tính DataAvailable chính xác hơn)
                    await Task.Delay(10);

                }
                // 3. Vòng lặp: Nếu trên đường truyền vẫn còn tín hiệu -> Đọc tiếp
                while (_stream.DataAvailable);

                return messageBuilder.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Loi nhan tin: " + ex.Message);
                return null;
            }
        }
        // ham chi gui
        public async Task SendAsync(string message)
        {
            if (_client != null && _client.Connected)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync(); // Đẩy tin đi ngay
            }
        }
        public void Disconnect()
        {
            _client?.Close();
        }
    }
}
