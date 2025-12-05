using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Core.Entities
{
    [Table("LoginHistories")]
    public class LoginHistory
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; } // Liên kết với bảng User

        [ForeignKey("UserId")]
        public User User { get; set; }  // Khóa ngoại

        public DateTime LoginTime { get; set; } = DateTime.Now;

        public string IpAddress { get; set; }

        public string DeviceInfo { get; set; } // "Client PC" hoặc "Admin PC"

        public bool IsSuccess { get; set; } 
    }
}
