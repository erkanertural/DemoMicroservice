
using ContactApiClient;
using Core.DBContext;
using Core.Repositories;
using Core.Services;
using Core.UnitofWork;
using Library;
using Library.RabbitMQ;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.OpenApi.Models;
using Refit;
using ReportAPI.Helper;
using ReportData;
using ReportServices.Services;
using System.Text.Json.Serialization;

namespace ReportAPI
{

    public static class Program
    {
        public static IApplicationBuilder App { get; private set; }
        public static ServiceProvider Provider { get; private set; }
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);
            var myEnv = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            myEnv = string.IsNullOrEmpty(myEnv) ? "" : "." + myEnv;
            builder.Configuration.AddJsonFile($"appsettings{myEnv}.json");

            //access to IConfiguration
            var configuration = builder.Configuration;

            string kafkaIP = configuration.GetSection("KafkaIP").Value;



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




            // Add services to the container.
            Provider = builder.Services.BuildServiceProvider();
            builder.Services.AddRefitClient<IContactApiClient>().ConfigureHttpClient(c => c.BaseAddress = new Uri("https://localhost:5046/api"));
            builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string[] xmls = Directory.GetFiles(basePath, "*.xml");
                xmls.ToList().ForEach(o => options.IncludeXmlComments(o));
                options.EnableAnnotations();

                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1.0",

                    Title = " API",
                    Description = "<b>Demo</b>",
                    Contact = new OpenApiContact
                    {
                        Name = "Front-end",
                        Url = new Uri("https://localhost:5046")
                    },

                });
                options.AddSecurityDefinition("Authorization", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Authorization",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization with Access Token"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                        Id = "Authorization"
                }
            },
            new string[] {}
        }
    });

            });

            builder.Services.AddHttpContextAccessor();
            ObjectMapper.Register<MappingProfile>();
            RegisterDI(builder);
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddHostedService<ReportBackgroundService>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {

                app.UseSwagger(c =>
                {

                    c.RouteTemplate = "swagger/{documentname}/swagger.json";
                    c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Servers = new System.Collections.Generic.List<OpenApiServer>


        {
#if LOCAL
new OpenApiServer { Url = $"https://{httpReq.Host.Value}/api" }
            
#else

new OpenApiServer { Url = $"https://{httpReq.Host.Value}/" }

#endif

                    });
                });
                app.UseSwaggerUI(c =>
                {

                    c.SwaggerEndpoint("/swagger/v1/swagger.json", " Test Service v1");

                    c.RoutePrefix = "doc";
                });
            }
            app.MapControllerRoute("default", "{controller}/{action}/{id?}");
            app.UseExceptionHandler(a => a.Run(async context =>
            {
                var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature.Error;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = exception.Message
                });
            }));

            app.UseHttpsRedirection();
            app.UseCors("MyPolicy");
            app.MapControllers();
            app.UseAuthentication();
            app.UseAuthorization();

            app.Run();
        }

        private static void RegisterDI(WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<IDBContext, ReportContext>();

            builder.Services.AddScoped<Repository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddTransient(typeof(IRepository<>), typeof( Repository<>));
            builder.Services.AddSingleton<IQueuePublisher, QueuePublisher>();
           // builder.Services.AddTransient<IBaseService<Report>, BaseService<Report>>();
            builder.Services.AddTransient(typeof(IBaseService<>), typeof(BaseService<>));
            builder.Services.AddTransient<ReportService>();

            builder.Services.AddSingleton<IQueueConsumer, QueueConsumer>();

        }
    }
}
