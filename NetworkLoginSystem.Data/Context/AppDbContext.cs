using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetworkLoginSystem.Core.DOTs;
using NetworkLoginSystem.Core.Entities;

namespace NetworkLoginSystem.Data.Context
{
    public class AppDbContext : DbContext
    {
        private readonly string _connectionString;

        public DbSet<User> Users { get; set; }
        public DbSet<Core.Entities.LoginRequest> loginRequest { get; set; }

        public DbSet<LoginHistory> LoginHistories { get; set; }

        //nhận chuỗi kết nối từ bên ngoài
        public AppDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        public AppDbContext()
        {
            // Chuỗi mặc định để chạy lệnh Migration trên máy Dev
            _connectionString = "Server=.;Database=NetworkLoginDB;Trusted_Connection=True;TrustServerCertificate=True;";
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Đảm bảo Username là duy nhất (không trùng)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // admin123
            string adminPassHash = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9";

            modelBuilder.Entity<User>().HasData(
                  new User
                    {
                       Id = 1, // Bắt buộc phải có ID cố định
                           Username = "admin",
                           PasswordHash = adminPassHash,
                           Role = "Admin",
                              IsActive = true,
                          CreatedAt = DateTime.Now
                   }
            );
            modelBuilder.Entity<Core.Entities.LoginRequest>().ToTable("loginRequests");
        }
    }
}
