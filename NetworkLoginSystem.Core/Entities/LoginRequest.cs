using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Core.Entities
{
    // client gửi yêu cầu lưu trong DB khi admin login sau vẫn xuất hện
    public class LoginRequest
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; }
        public string IpAddress { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;

        // 0: Pending (Chờ), 1: Approved (Duyệt), 2: Rejected (Từ chối)
        public int Status { get; set; } = 0;
    }
}
