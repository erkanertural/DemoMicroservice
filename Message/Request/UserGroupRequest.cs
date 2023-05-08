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

    public class UserGroupRequest : BaseRequest
    {
        public UserGroupRequest()
        {
            this.SetAllPropertiesToDefaultValue();
        }

        public int GroupId { get; set; }
    }
}
