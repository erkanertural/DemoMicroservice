using Core.Entities;
using Core.Entities.IEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactEntities
{
    public class Contact : BaseEntity, ISoftDelete
    {


        public string Name { get; set; }
        public string SurName { get; set; }
        public string Company { get; set; }
        public virtual List<ContactDetail> ContactDetails { get; set; }
     

        public bool IsDeleted { get; set; }

        public void AddContactDetail(ContactDetail c)
        {
            if (ContactDetails == null)
                ContactDetails = new List<ContactDetail>();
            ContactDetails.Add(c);
        }

        public void RemoveContactDetail(long contactDetailId)
        {
            if (ContactDetails != null && ContactDetails.Count > 0)
            {
                ContactDetail cd = ContactDetails.FirstOrDefault(o => o.Id == contactDetailId);
                if (cd != null)
                    ContactDetails.Remove(cd);
                
            }
            
        }
    }
}