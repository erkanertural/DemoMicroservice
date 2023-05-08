using Library;
using Microsoft.Extensions.DependencyInjection;

namespace ContactAPI.Extensions;

public static class CorsExtension
{
    public static void AddCorsConfiguration(this Microsoft.AspNetCore.Builder.WebApplicationBuilder builder, AppSettings appSettings)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("MyPolicy",
                builder =>
                {
                    builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });
        });
    }
}