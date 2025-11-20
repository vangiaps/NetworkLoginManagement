using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Core.DOTs
{
    public class LoginResponse
    {
        public bool IsSuccess { get; set; }   
        public string Message { get; set; }   
        public string Role { get; set; }      // "Admin" hoặc "Client"
    }
}
