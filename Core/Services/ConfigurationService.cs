using Core.Utilities;
using Microsoft.Extensions.Configuration;

namespace Core.Services;

public interface IConfigurationService
{
    IConfiguration GetConfiguration();
}

public class ConfigurationService : IConfigurationService
{
    IConfiguration Configuration { get; set; }
    private IConfigurationService _configurationService;
    public ConfigurationService() { }


    public IConfiguration GetConfiguration()
    {
        CreateInstance();
        string env = CommonUtil.Enviromnent;
        Configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{env}.json", false)
                .Build();
        return Configuration;
    }
    private IConfigurationService CreateInstance()
    {
        if (_configurationService == null)
        {
            _configurationService = new ConfigurationService();
        }
        return _configurationService;
    }  
}
