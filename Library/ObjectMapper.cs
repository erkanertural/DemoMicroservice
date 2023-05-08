using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace Library
{
    public class ObjectMapper
    {
        static private Lazy<IMapper> lazy = null;
        public static void Register<T>() where T:AutoMapper.Profile, new()
        {
            lazy   = new Lazy<IMapper>(

        () =>
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<T>();
            });

            return config.CreateMapper();
        }
      );
        }


        public static IMapper Mapper => lazy.Value;
    }
}
