using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace NetworkLoginSystem.Security
{
    // thuật toán SHA256
    public static class PasswordHasher
    {
        // Hàm mã hóa: "123456" -> "8d969eef6ecad3c29a3a629280e686cf..."
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Chuyển chuỗi thành byte
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Chuyển mảng byte thành chuỗi Hex
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Hàm kiểm tra: Nhập pass mới -> Mã hóa -> So sánh với pass trong DB
        public static bool VerifyPassword(string inputPassword, string dbHash)
        {
            string inputHash = HashPassword(inputPassword);
            return StringComparer.OrdinalIgnoreCase.Compare(inputHash, dbHash) == 0;
        }
    }
}
