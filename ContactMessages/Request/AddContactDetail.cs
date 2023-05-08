using Core.Message.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ContactLibrary.Enums;

namespace ContactMessages.Request
{
    public class AddContactDetail
    {
     

       

        public long ContactId { get; set; }

        public string Context { get; set; }

        public ContactType Type { get; set; }
    }
}
