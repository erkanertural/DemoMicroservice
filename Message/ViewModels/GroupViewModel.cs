using BulKurumsal.Entities;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message.ViewModels
{
    public class GroupViewModel
    {
        public int Id { get; set; }
        public int DomainId { get; set; }
        public string Email { get; set; }
        public bool? IsActive { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public Domain? Domain { get; set; }
    }
}
