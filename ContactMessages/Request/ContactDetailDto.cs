using Core.Entities;
using Core.Entities.IEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ContactLibrary.Enums;

namespace ContactMessages.Request
{
    public class ContactDetailDto
    {
   
        public long ContactId { get; set; }

        public string Context { get; set; }

        public ContactType Type { get; set; }

        public bool IsDeleted { get; set; }
    }
}