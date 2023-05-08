using BulKurumsal.Entities;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message.ViewModels
{
    public class UserGroupViewModel
    {
        public int? Id { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public int DomainId { get; set; }

        public User? User { get; set; }

        public Group? Group { get; set; }
    }
}
