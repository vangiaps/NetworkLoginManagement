using NetworkLoginSystem.Core.DOTs;
using NetworkLoginSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Admin
{
    /// <summary>
    /// Interaction logic for History.xaml
    /// </summary>
    /// 
    public partial class History : UserControl
    {
        public ObservableCollection<HistoryItemDto> HistoryList { get; set; }
        public History()
        {
            InitializeComponent();
            HistoryList = new ObservableCollection<HistoryItemDto>();
            HistoryListView.ItemsSource = HistoryList;

        }
        public void UpdateHistory(List<HistoryItemDto> items)
        {
            //MessageBox.Show($"Giao diện đã nhận được: {items.Count} dòng dữ liệu.\nDòng 1: {items[0].Username} - {items[0].Time}");
            HistoryList.Clear(); // Xóa cũ
            foreach (var item in items)
            {
                HistoryList.Add(item); // Thêm mới
            }
        }
    }
}
