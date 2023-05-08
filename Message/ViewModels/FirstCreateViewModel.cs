using BulKurumsal.Core.Request;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message.ViewModels
{
    public class FirstCreateViewModel
    {

        public UserViewModel User { get; set; }
        public OwnerRequest Corporation { get; set; }
        public DomainViewModel Domain { get; set; }
    }
}
