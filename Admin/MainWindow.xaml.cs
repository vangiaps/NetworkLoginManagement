using NetworkLoginSystem.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.Json;
using NetworkLoginSystem.Core.DOTs;
using System.Collections.ObjectModel;

namespace Admin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    // Class phụ để hiển thị lên List View
    public class LoginRequestItem
    {
        public int Id { get; set; } // ID yêu cầu trong DB
        public string Username { get; set; }
        public string IpAddress { get; set; }
        public string RequestTime { get; set; }
    }

    public partial class MainWindow : Window
    {
        private SocketClient _socketClient;
        public bool isConnected;

        public ObservableCollection<LoginRequestItem> PendingRequests { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _socketClient = new SocketClient();
            _ = ConnectToServerAsync();
            _= ListenForServerMessages();

            PendingRequests = new ObservableCollection<LoginRequestItem>();
            LoginRequestsItemsControl.ItemsSource = PendingRequests;
        }
        private async Task ConnectToServerAsync()
        {
            // giu ket noi lien tuc
            AdminStatusText.Text = "Connecting...";
            isConnected = await _socketClient.ConnectAsync("192.168.155.110", 9000);

            if (isConnected)
            {
                AdminStatusText.Text = "Connected";
                 AdminStatusIndicator.Fill = new SolidColorBrush(Colors.Green);

            // can gui lai socket admin 
            var loginData = new LoginRequest { 
                Username = "admin", 
                Password = "admin123" 
            };
            var packet = new DataPacket
            {
                Type = PacketType.AdminReconnection,
                Data = JsonSerializer.Serialize(loginData)
            };
            string jsonLogin = JsonSerializer.Serialize(packet);
            await _socketClient.SendAsync(jsonLogin);// gui di k can nhan lai

            _ = Task.Run(() => ListenForServerMessages());
            }
            else
            {
                AdminStatusText.Text = "Disconnected";
                AdminStatusIndicator.Fill = new SolidColorBrush(Colors.Red);

            }
        }
        private async Task ListenForServerMessages()
        {
            while (isConnected)
            {
                try
                {
                    string message = await _socketClient.ReceiveOnlyAsync();

                    if (string.IsNullOrEmpty(message)) break; // Mất kết nối

                    // Giải mã gói tin
                    var packet = JsonSerializer.Deserialize<DataPacket>(message);

                    if (packet.Type == PacketType.NewLoginRequest)
                    {
                        var newItem = JsonSerializer.Deserialize<LoginRequestItem>(packet.Data);

                        // Thêm vào giao diện
                        AddNewRequest(newItem);

                        AddToLog($"Yêu cầu mới từ: {newItem.Username} ({newItem.IpAddress})", 2);
                    }
                }
                catch
                {
                    // Bỏ qua lỗi nhỏ hoặc ngắt kết nối nếu lỗi nặng
                    break;
                }
            }
        }

        // --- XỬ LÝ NÚT CHẤP NHẬN ---
        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var requestItem = button.Tag as LoginRequestItem; // Lấy dữ liệu từ Tag

            if (requestItem != null)
            {
                // 1. Gửi tin hiệu ACCEPT lên Server
                await SendDecisionToServer(requestItem.Id, 1); // 1 = Approved

                // 2. Xóa khỏi danh sách hiển thị ngay lập tức (cho mượt)
                PendingRequests.Remove(requestItem);

                AddToLog($"Đã CHẤP NHẬN user: {requestItem.Username}",2);
                AddToLog($"{requestItem.Username}",1);
            }
        }
        // --- XỬ LÝ NÚT TỪ CHỐI ---
        private async void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var requestItem = button.Tag as LoginRequestItem;

            if (requestItem != null)
            {
                // 1. Gửi tin hiệu REJECT lên Server
                await SendDecisionToServer(requestItem.Id, 2); // 2 = Rejected

                // 2. Xóa khỏi danh sách
                PendingRequests.Remove(requestItem);

                AddToLog($"Đã TỪ CHỐI user: {requestItem.Username}", 2);
            }

        }
        // --- HÀM GỬI QUYẾT ĐỊNH CHUNG ---
        private async Task SendDecisionToServer(int requestId, int status)
        {
            try
            {
                // Tạo gói tin quyết định (Cấu trúc này phải khớp với Server mong đợi)
                var decisionData = new AdminDecisionPacket
                {
                    RequestId = requestId,
                    IsApproved = (status == 1)
                };

                // Đóng gói
                var packet = new DataPacket
                {
                    Type = PacketType.AcceptLogin, // Hoặc dùng chung 1 Type là AdminDecision
                    Data = JsonSerializer.Serialize(decisionData)
                };

                // Gửi đi
                string jsonToSend = JsonSerializer.Serialize(packet);
                await _socketClient.SendAsync(jsonToSend);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi quyết định: {ex.Message}");
            }
        }
        public void AddNewRequest(LoginRequestItem newItem)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PendingRequests.Add(newItem);
            });
        }

        private void AddToLog(string message, int status)
        {
            // Vì đụng vào giao diện từ luồng khác nên phải dùng Dispatcher
            Application.Current.Dispatcher.Invoke(() =>
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{time}] {message}";
                
                if(status == 2)
                {
                ActivityLogTextBlock.Text += logEntry + "\n------------------\n" + HistoryTextBlock.Text;
                }
                else if(status == 1)
                {
                    HistoryTextBlock.Text += logEntry + "\n------------------\n" + HistoryTextBlock.Text;
                }
            });
        }
    }
}
