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

    public class GroupRequest : BaseRequest
    {
        public GroupRequest()
        {
            this.SetAllPropertiesToDefaultValue();
        }
        public int DomainId { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
    }
}
