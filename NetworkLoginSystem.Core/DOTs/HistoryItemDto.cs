using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Core.DOTs
{
    public class HistoryItemDto
    {
        public string Username { get; set; }
        public string Action { get; set; } // "Login" hoặc "Approved"
        public string Time { get; set; }
        public string Status { get; set; } // "Success", "Failed"
        public string IpAddress { get; set; }
    }
}
