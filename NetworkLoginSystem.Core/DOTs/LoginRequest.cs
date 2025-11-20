using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Core.DOTs
{
    //Gói tin gửi từ Client lên Server.
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
