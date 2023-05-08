using AutoMapper;
using ContactMessages.Request;

namespace ContactEntities.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ContactDetail, AddContactDetail>().ReverseMap();

            CreateMap<Contact, ContactDetailsDto>().ReverseMap();
            CreateMap<ContactDetail, ContactDetailDto>().ReverseMap();
            CreateMap<ContactDetail, AddContactDetail>().ReverseMap();
       
        }
    }
}
