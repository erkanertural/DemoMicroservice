using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactMessages.Request
{
    public  class ContactDetailsDto
    {
        public ContactDetailsDto()
        {
           
        }

        public long Id { get; set; }    
        public string Name { get; set; }
        public string SurName { get; set; }
        public string Company { get; set; }

        public  List<ContactDetailDto> ContactDetails { get; set; }
  

        public bool IsDeleted { get; set; }
    }
}
