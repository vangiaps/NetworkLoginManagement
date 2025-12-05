using NetworkLoginSystem.Core.DOTs;
using NetworkLoginSystem.Protocol;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Admin
{
    /// <summary>
    /// Interaction logic for ClientManagementControl.xaml
    /// </summary>
    public partial class ClientManagementControl : UserControl
    {
        public ObservableCollection<ClientItemDto> ClientList { get; set; }
        private SocketClient _socketClient;

        public ClientManagementControl()
        {
            InitializeComponent();
            ClientList = new ObservableCollection<ClientItemDto>();
            ClientListView.ItemsSource = ClientList;
        }

        public void SetSocket(SocketClient socket)
        {
            _socketClient = socket;
            // Tự động tải dữ liệu khi mở
            RefreshData();
        }

        // Gửi lệnh lấy danh sách
        public async void RefreshData()
        {
            if (_socketClient != null)
            {
                var packet = new DataPacket { Type = PacketType.GetClientList, Data = "" };
                await _socketClient.SendAsync(JsonSerializer.Serialize(packet));
            }
        }

        // Hàm MainWindow gọi khi có dữ liệu về
        public void UpdateClientList(List<ClientItemDto> items)
        {
            ClientList.Clear();
            foreach (var item in items) ClientList.Add(item);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        // --- XỬ LÝ NÚT KHÓA/MỞ KHÓA ---
        private async void ToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var client = btn.Tag as ClientItemDto;

            if (client != null)
            {
                // 1. Đổi trạng thái ngược lại (True -> False, False -> True)
                bool newStatus = !client.IsActive;

                // 2. Gửi lên Server
                var updateDto = new UpdateStatusDto { UserId = client.Id, NewStatus = newStatus };
                var packet = new DataPacket
                {
                    Type = PacketType.UpdateUserStatus,
                    Data = JsonSerializer.Serialize(updateDto)
                };

                await _socketClient.SendAsync(JsonSerializer.Serialize(packet));

                // 3. Cập nhật ngay trên giao diện để User thấy nút đổi màu
                // (Vì ObservableCollection không tự bắt sự kiện thay đổi thuộc tính con, 
                // ta cần thủ thuật nhỏ: Xóa đi add lại hoặc dùng INotifyPropertyChanged)
                // Cách đơn giản nhất:
                client.IsActive = newStatus;

                // Refresh lại list view để Trigger màu sắc hoạt động
                ClientListView.Items.Refresh();

                // Ghi Log (Gọi ngược lên MainWindow nếu muốn)
                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                if (mainWindow != null)
                {
                    string action = newStatus ? "Mở khóa" : "Khóa";
                    mainWindow.AddToLog($"Đã {action} tài khoản: {client.Username}", 2);
                }
            }
        }
        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var client = btn.Tag as ClientItemDto;

            if (client != null)
            {
                // 1. Hỏi xác nhận (Quan trọng)
                var confirmResult = MessageBox.Show(
                    $"Bạn có chắc chắn muốn XÓA VĨNH VIỄN tài khoản '{client.Username}'?\n\nDữ liệu không thể phục hồi!",
                    "Cảnh báo xóa dữ liệu",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 2. Gửi lệnh lên Server
                        var deleteDto = new DeleteUserDto { UserId = client.Id };
                        var packet = new DataPacket
                        {
                            Type = PacketType.DeleteUser,
                            Data = JsonSerializer.Serialize(deleteDto)
                        };

                        await _socketClient.SendAsync(JsonSerializer.Serialize(packet));

                        // 3. Xóa khỏi giao diện ngay lập tức
                        ClientList.Remove(client);

                        // Ghi log
                        var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                        if (mainWindow != null)
                        {
                            mainWindow.AddToLog($"Đã XÓA tài khoản: {client.Username}", 2);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi: " + ex.Message);
                    }
                }
            }
        }
    }
}
