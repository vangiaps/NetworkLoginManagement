using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; 
using NetworkLoginSystem.Data.Context;
using NetworkLoginSystem.Server;

namespace NetworkLoginSystem
{
    /// <summary>
    /// class để tạo cổng và những thứ cần để khởi tạo server
    /// </summary>
    class Program
    {
        // Biến toàn cục lưu cấu hình
        public static IConfiguration Configuration { get; private set; }
        public static string ConnectionString { get; private set; }
        
        static void Main(string[] args)
        {
            Console.WriteLine("=== KHOI DONG SERVER ===");
            //Đọc file cấu hình appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())//Thiết lập đường dẫn gốc
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            // Lấy chuỗi kết nối
            ConnectionString = Configuration.GetConnectionString("DefaultConnection");
            int port = Configuration.GetValue<int>("ServerSettings:Port");

            // Tự động cập nhật Database (Auto Migration)
            InitializeDatabase();
            Console.WriteLine("\nDang khoi dong TCP Socket...");
            string ip = Configuration["ServerSettings:IpAddress"];
            int tcpPort = Configuration.GetValue<int>("ServerSettings:Port");

            // Tạo Server và Chạy
            TcpServer server = new TcpServer(ip,tcpPort);

            server.StartAsync().Wait();
            Console.ReadLine();
        }

        private static void InitializeDatabase()
        {
            try
            {
                // Truyền chuỗi kết nối lấy từ Config vào DbContext
                using (var context = new AppDbContext(ConnectionString))
                {
                    context.Database.Migrate(); // Lệnh này tự tạo DB nếu chưa có
                }
                Console.WriteLine("OK! (Database da san sang)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ LOI KET NOI DB: {ex.Message}");
                Console.WriteLine("Vui long kiem tra file appsettings.json");
            }
        }
    }
}