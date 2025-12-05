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
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Admin;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class LoginUi : Window
{
    private SocketClient _socketClient;

    public IConfiguration Configuration { get; private set; }
    public LoginUi()
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
    }
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }   
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        // tránh lỗi bẫm liên tục
        LoginButton.IsEnabled = false;
        //StatusTextBlock.Text = "Dang ket noi Server...";

        string username = UsernameTextBox.Text;
        string password = PasswordBox.Password;

        string ip = Configuration["ServerSettings:IpAddress"];
        int port = Configuration.GetValue<int>("ServerSettings:Port");

        bool isConnected = await _socketClient.ConnectAsync(ip, port);
        if (!isConnected)
        {
            //StatusTextBlock.Text = "Loi ket noi Server...";
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

            if (result.IsSuccess)
            {
                StatusTextBlock.Text = "✅ " + result.Message;
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.LightGreen;

                // Delay 1 chút cho người dùng đọc thông báo
                await Task.Delay(1000);

                // Kiểm tra quyền
                if (result.Role == "Admin")
                {
                    MessageBox.Show($"Chao mung!");
                    var MainUi = new MainWindow();
                    MainUi.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show($"Chao mung!");
                    // TODO: Mở màn hình Client
                }
            }
            else
            {
                // Đăng nhập thất bại (Sai pass, khóa nick...)
                StatusTextBlock.Text = "❌ " + result.Message;
                StatusBorder.Visibility = Visibility.Visible;
                LoginButton.IsEnabled = true;
            }
        }
        catch
        {
            StatusTextBlock.Text = "❌ Loi doc du lieu Server!";
            LoginButton.IsEnabled = true;
        }
    
    }

}