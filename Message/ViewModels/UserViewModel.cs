using BulKurumsal.Entities;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Message.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public int DomainId { get; set; }

        public string? WildDuckId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsActive { get; set; }
        public string Title { get; set; }
        public bool IsAdmin { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }


        public Domain? Domain { get; set; }
    }

    public class UserCreate
    {
        public string username { get; set; }
        public string name { get; set; }
        public string password { get; set; }
        public string address { get; set; }
        public bool uploadSentMessages { get; set; }
        public int quota { get; set; }
    }
}
