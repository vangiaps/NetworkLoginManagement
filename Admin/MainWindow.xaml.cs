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
using System.Configuration;
using Microsoft.Extensions.Configuration;
using System.IO;

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

        private LoginRequestsControl _requestsView;
        private History _historyView;
        private ClientManagementControl _clientView;
        private List<Button> menuButtons;
    
        public IConfiguration Configuration { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            // đọc file cấu hình
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("Admin.json", optional: false, reloadOnChange: true);

                Configuration = builder.Build();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đọc file setting.json: {ex.Message}");
                return;
            }
            _socketClient = new SocketClient();

            _requestsView = new LoginRequestsControl();
            _historyView = new History();
            _requestsView.SetSocket(_socketClient);
            MainContentArea.Content = _requestsView;
            _clientView = new ClientManagementControl();
            _clientView.SetSocket(_socketClient);

            _ = ConnectToServerAsync();

            menuButtons = new()
            {
                btnRequests,
                btnHistory,
                btnClients
            };

            Loaded += MainWindow_Loaded;
        }
        private async Task ConnectToServerAsync()
        {
            string ip = Configuration["ServerSettings:IpAddress"];
            int port = Configuration.GetValue<int>("ServerSettings:Port");

            // giu ket noi lien tuc
            AdminStatusText.Text = "Connecting...";
            isConnected = await _socketClient.ConnectAsync(ip, port);

            if (isConnected)
            {
                AdminStatusText.Text = "Connected";
                AdminStatusIndicator.Fill = new SolidColorBrush(Colors.Green);

                // can gui lai socket admin 
                var loginData = new LoginRequest
                {
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
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var packet = JsonSerializer.Deserialize<DataPacket>(message);
                    //MessageBox.Show($"Loại gói tin: {packet.Type}");

                    if (packet.Type == PacketType.NewLoginRequest)
                    {
                        var newItem = JsonSerializer.Deserialize<LoginRequestItem>(packet.Data);

                        // Gọi vào màn hình con để thêm dữ liệu
                        // Dùng Dispatcher để vẽ lên giao diện
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _requestsView.AddNewRequest(newItem);
                        });
                    }
                    else if (packet.Type == PacketType.LoginHistoryData)
                    {
                        // Giải mã danh sách
                        var listHistory = JsonSerializer.Deserialize<List<HistoryItemDto>>(packet.Data, options);

                        // Cập nhật UI (Dùng Dispatcher)
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (_historyView != null)
                            {
                                _historyView.UpdateHistory(listHistory);
                            }
                        });
                    }
                    else if (packet.Type == PacketType.ClientListData)
                    {
                        var list = JsonSerializer.Deserialize<List<ClientItemDto>>(packet.Data, options);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _clientView.UpdateClientList(list);
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi Lắng nghe: " + ex.Message);
                    // Bỏ qua lỗi nhỏ hoặc ngắt kết nối nếu lỗi nặng
                    break;
                }
            }
        }
        private void MenuRequests_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = _requestsView;
            SetActiveMenuButton(sender as Button);
        }
        private async void MenuHistory_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = _historyView;
            SetActiveMenuButton(sender as Button);

            // Gửi lệnh xin dữ liệu lên Server
            var packet = new DataPacket
            {
                Type = PacketType.GetLoginHistory,
                Data = ""
            };
            string json = JsonSerializer.Serialize(packet);

            if (_socketClient != null)
                await _socketClient.SendAsync(json);
        }
        private void MenuClients_Click(object sender, RoutedEventArgs e)
        {
            MainContentArea.Content = _clientView;
            _clientView.RefreshData(); // Lấy dữ liệu mới nhất
            SetActiveMenuButton(sender as Button);
        }

        public void AddToLog(string message, int status)
        {
            // Vì đụng vào giao diện từ luồng khác nên phải dùng Dispatcher
            Application.Current.Dispatcher.Invoke(() =>
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{time}] {message}";

                if (status == 2)
                {
                    ActivityLogTextBlock.Text += logEntry + "\n------------------\n" + HistoryTextBlock.Text;
                }
                else if (status == 1)
                {
                    HistoryTextBlock.Text += logEntry + "\n------------------\n" + HistoryTextBlock.Text;
                }
            });
        }

        // ham doi mau button
        private void SetActiveMenuButton(Button activeButton)
        {
            // Gradient khi được chọn
            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0.5, 0);
            gradient.EndPoint = new Point(0.5, 1);
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FF667EEA"), 0));
            gradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FF764BA2"), 1));

            // Reset tất cả về màu trắng
            foreach (var btn in menuButtons)
            {
                btn.Background = new SolidColorBrush(Colors.White);
            }

            // Set màu cho button đang được click
            activeButton.Background = gradient;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Chọn button mặc định
            SetActiveMenuButton(btnRequests);
        }

    }
}
