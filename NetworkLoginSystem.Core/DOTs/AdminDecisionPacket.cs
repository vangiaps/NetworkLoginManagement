using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Core.DOTs
{
    public class AdminDecisionPacket
    {
        public int RequestId { get; set; } // ID của yêu cầu trong DB
        public bool IsApproved { get; set; } 
    }
}
