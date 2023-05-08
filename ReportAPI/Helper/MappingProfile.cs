using AutoMapper;

namespace ReportAPI.Helper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap <string, string> ().ReverseMap();
        
        }
    }
}
