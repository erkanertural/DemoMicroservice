using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactMessages.Request
{
    public class ReportDto
    {
        public long ReportId { get; set; }
        public string  Location { get; set; }
        public int ContactCount { get; set; }    
        public int CountOfContactDetailTelephone { get; set; } 
    }
}
