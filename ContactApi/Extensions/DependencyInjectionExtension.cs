using ContactData;
using ContactEntities;
using ContactServices.Services;
using Core.DBContext;
using Core.Repositories;
using Core.Services;
using Core.UnitofWork;
using Library.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

namespace ContactAPI.Extensions;

public static class DependencyInjectionExtension
{
    public static void RegisterDIContainers(this IServiceCollection services)
    {
        // services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddDbContext<IDBContext, ContactContext>();
        services.AddScoped<Repository, Repository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddTransient(typeof (IRepository<>),typeof( Repository<>));
        services.AddTransient(typeof(IBaseService<>), typeof(BaseService<>));
        services.AddTransient<ContactService>();

  
    }
}
