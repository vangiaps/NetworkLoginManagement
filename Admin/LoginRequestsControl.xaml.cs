    using NetworkLoginSystem.Core.DOTs;
using NetworkLoginSystem.Protocol;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for LoginRequestsControl.xaml
    /// </summary>
    public partial class LoginRequestsControl : UserControl
    {
        public ObservableCollection<LoginRequestItem> PendingRequests { get; set; }
        private SocketClient _socketClient;

        public LoginRequestsControl()
        {
            InitializeComponent();
            PendingRequests = new ObservableCollection<LoginRequestItem>();
            LoginRequestsItemsControl.ItemsSource = PendingRequests;

        }
        // Hàm này để MainWindow truyền Socket vào cho dùng
        public void SetSocket(SocketClient socket)
        {
            _socketClient = socket;
        }
        public void AddNewRequest(LoginRequestItem newItem)
        {
            // Kiểm tra trùng lặp
            foreach (var item in PendingRequests)
            {
                if (item.Id == newItem.Id) return;
            }
            PendingRequests.Add(newItem);
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mainWindow.AddToLog($"{newItem.Username} yêu cầu đăng nhập", 2);
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

                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                mainWindow.AddToLog($"Đã CHẤP NHẬN user: {requestItem.Username}", 2);
                mainWindow.AddToLog($"{requestItem.Username}", 1);
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


                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                mainWindow.AddToLog($"Đã TỪ CHỐI user: {requestItem.Username}", 2);
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
    }
}
