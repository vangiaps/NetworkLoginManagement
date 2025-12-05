using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NetworkLoginSystem.Protocol;
using System.Text.Json;
using NetworkLoginSystem.Core.DOTs;
using Microsoft.Extensions.Configuration;
using System.IO;



namespace Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private SocketClient _socketClient;
    public bool isConnected;

    public IConfiguration Configuration { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        // đọc file cấu hình
        try
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Client.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi đọc file setting.json: {ex.Message}");
            return;
        }

        _socketClient = new SocketClient();
        _ = ConnectToServerAsync();
    }

    private async Task ConnectToServerAsync()
    {
        string ip = Configuration["ServerSettings:IpAddress"];
        int port = Configuration.GetValue<int>("ServerSettings:Port");

        // giu ket noi lien tuc
        isConnected = await _socketClient.ConnectAsync(ip, port);
        if (!isConnected)
        {
            StatusText.Text = "Disconnected";
            StatusIndicator.Fill = new SolidColorBrush(Colors.Red);
        }
        StatusText.Text = "Connected";
        StatusIndicator.Fill = new SolidColorBrush(Colors.Green);
    }

    private void RegisterLink_Click(object sender, RoutedEventArgs e)
    {
        var register = new Register();
        register.Show();
    }
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // tránh lỗi bẫm liên tục
        LoginButton.IsEnabled = false;

        string username = UsernameTextBox.Text;
        string password = PasswordBox.Password;

        if (!isConnected)
        {
            MessageBox.Show("Loi ket noi Server...");
            LoginButton.IsEnabled = true;
            return;
        }

        // tạo fois tin login (core)
        var loginReq = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var packet = new DataPacket
        {
            Type = PacketType.LoginRequest,
            Data = JsonSerializer.Serialize(loginReq)
        };

        string finalJson = JsonSerializer.Serialize(packet);
        string response = await _socketClient.SendAndReceiveAsync(finalJson);
        // dich json tu server
        try
        {
            var result = JsonSerializer.Deserialize<LoginResponse>(response);

           // Admin duyệt thẳng(hoặc lỗi ngay lập tức)
            if (result.Role != "Pending")
            {
                if (result.IsSuccess)
                {
                    // Chuyển màn hình Client chính
                    MessageBox.Show("Đăng nhập thành công!");
                    // OpenClientDashboard();
                    this.Close();
                }
                else
                {
                    LogTextBlock.Text += "❌ " + result.Message + "\n";
                    LoginButton.IsEnabled = true;
                }
            }
            else
            {
                LogTextBlock.Text += "⏳ " + result.Message + "\n";

                // Hàm này sẽ treo ở đây cho đến khi Admin bấm nút và Server gửi tin về
                string finalDecisionJson = await _socketClient.ReceiveOnlyAsync();

                // Khi dòng trên chạy xong, nghĩa là đã có kết quả
                if (!string.IsNullOrEmpty(finalDecisionJson))
                {
                    var finalResult = JsonSerializer.Deserialize<LoginResponse>(finalDecisionJson);

                    if (finalResult.IsSuccess)
                    {
                        LogTextBlock.Text += "✅ Admin đã chấp nhận! \n";
                        await Task.Delay(1000);

                        Welcome welcome = new Welcome();
                        welcome.Show();
                        this.Close();
                    }
                    else
                    {
                        LogTextBlock.Text += "❌ Admin đã từ chối yêu cầu! \n";
                        LoginButton.IsEnabled = true;
                    }
                }
            }
        }
        catch
        {
            LogTextBlock.Text += "❌ Loi doc du lieu Server! \n";
            LoginButton.IsEnabled = true;
        }
    }
}