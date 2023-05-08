using Library;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ContactAPI.Extensions;

public static class AppSettingsExtension
{
    public static void AddAppSettingsConfiguration(this WebApplicationBuilder builder)
    {
        var appSettingsProps = builder.Configuration.GetSection("AppSettings");
        builder.Services.Configure<AppSettings>(appSettingsProps);
        builder.Services.AddSingleton<IAppSettings>(serviceProvider => serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value);
    }
}
