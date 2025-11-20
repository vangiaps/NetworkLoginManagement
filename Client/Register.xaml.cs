using NetworkLoginSystem.Core.DOTs;
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
using System.Xml.Linq;
using NetworkLoginSystem.Protocol;
using System.Text.Json;
namespace Client
{
    /// <summary>
    /// Interaction logic for Register.xaml
    /// </summary>
    public partial class Register : Window
    {
        public Register()
        {
            InitializeComponent();
        }
        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var regReq = new RegisterRequest
            {
                Username = UsernameTextBox.Text,
                Password = PasswordBox.Password
            };
            var packet = new DataPacket
            {
                Type = PacketType.RegisterRequest,
                Data = JsonSerializer.Serialize(regReq)
            };

            // gui du lieu
            SocketClient client = new SocketClient();

            if (await client.ConnectAsync("192.168.155.110", 9000))
            {
                string jsonToSend = JsonSerializer.Serialize(packet);
                string response = await client.SendAndReceiveAsync(jsonToSend);

                // 4. Đọc kết quả
                var result = JsonSerializer.Deserialize<LoginResponse>(response);
                MessageBox.Show(result.Message);

                if (result.IsSuccess)
                {
                    this.Close(); // Đóng form đăng ký để quay lại đăng nhập
                }
                client.Disconnect();
            }
            else
            {
                MessageBox.Show("Khong ket noi duoc Server");
            }
        }
    }
}
