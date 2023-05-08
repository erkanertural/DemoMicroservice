using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message.ViewModels
{
    public class DomainViewModel
    {
        public int Id { get; set; }
        public int CorporationId { get; set; }

        public string? WildDuckId { get; set; }
        public string Name { get; set; }
        public bool? DnsVerification { get; set; }
        public bool? MxVerification { get; set; }
        public bool? DkimVerification { get; set; }
        public bool? SpfVerification { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
