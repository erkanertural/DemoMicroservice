using Core.Entities;
using Core.Entities.IEntities;
using static ContactLibrary.Enums;

namespace ContactEntities
{
    public class ContactDetail : BaseEntity, ISoftDelete
    {
    

        public long ContactId { get; set; }

        public string Context { get; set; }

        public ContactType Type { get; set; }    

        // public long UserId { get; set; }
        // public User User { get; set; }


        public bool IsDeleted { get; set; }
    }
}