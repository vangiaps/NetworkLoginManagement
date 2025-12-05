using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLoginSystem.Core.DOTs
{
    public class UpdateStatusDto
    {
        public int UserId { get; set; }
        public bool NewStatus { get; set; } // Trạng thái muốn set
    }
}
