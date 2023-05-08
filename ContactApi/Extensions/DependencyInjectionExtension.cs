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
  
        services.AddTransient<IRepository<NoEntity>, Repository<NoEntity>>();
        services.AddTransient<IRepository<Contact>, Repository<Contact>>();


        services.AddTransient<BaseService<Contact>, ContactService>();
        services.AddTransient<ContactService>();

        services.AddTransient<NoEntityService>();
        services.AddTransient<BaseService<NoEntity>, NoEntityService>();
  
    }
}
