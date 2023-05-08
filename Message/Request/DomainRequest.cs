using BulKurumsal.Core.Message.Request;
using BulKurumsal.Entities;
using BulLibrary;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message.Request
{

    /// <summary>
    /// bla bla test
    /// </summary>

    public class DomainRequest : BaseRequest
    {
        public DomainRequest()
        {
            this.SetAllPropertiesToDefaultValue();
        }

        public string WildDuckId { get; set; }

        public string Name { get; set; }

        public bool DnsVerification { get; set; }

        public bool MxVerification { get; set; }

        public bool DkimVerification { get; set; }

        public bool SpfVerification { get; set; }
    }
}
