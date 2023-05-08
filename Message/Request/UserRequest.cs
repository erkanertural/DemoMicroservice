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

    public class UserRequest : BaseRequest
    {
        public UserRequest()
        {
            this.SetAllPropertiesToDefaultValue();
        }

        public string WildDuckId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public int NewEmailSignatureId { get; set; }

        public int ForwardReplyEmailSignatureId { get; set; }

        public bool IsActive { get; set; }

        public string Title { get; set; }

        public bool IsAdmin { get; set; }
    }
}
