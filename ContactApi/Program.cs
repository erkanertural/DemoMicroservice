using ContactAPI.Extensions;
using ContactEntities.Mapping;
using FluentValidation.AspNetCore;
using Library;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;

namespace ContactAPI
{

    public static class Program
    {


        public static IConfiguration Configuration { get; set; }
        /// <summary>
        ///  This method returns list of domain of owner  
        /// </summary>
        /// <remarks>
        /// Sample of usage
        /// 
        /// <b>Must : Request.Id, Request.Pagination </b>
        /// satýr açýklama 1
        /// 
        /// satýr açýklama 2
        /// 
        /// satýr açýklama 3
        /// 
        ///     {
        ///        "id": "yourownerId",
        ///        "pagination": 
        ///        {
        ///          "pagesize":"10",
        ///          "pageNo":"0" 
        ///        }
        ///     }
        ///     
        /// </remarks>
        /// <returns> </returns>
        public static void Main(string[] args)
        {
            string? myEnv = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Console.WriteLine("asp enviroment-> " + myEnv);

            if (myEnv.IsNullOrEmpty() && args.Length > 0)
            {
                string envCmd = args.ToList().FirstOrDefault(o => o.ToLower().StartsWith("env="));
                if (envCmd.IsNotNullOrEmpty())
                {
                    myEnv = envCmd.Split("=", StringSplitOptions.RemoveEmptyEntries)[1];
                    Console.WriteLine("Detected Env = " + myEnv);
                }
            }
            System.Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", myEnv);

            //   System.Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "https://10.20.110.50:5046");
            // change to ev
            // todo : dynamic applicationURL 

            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
            myEnv = string.IsNullOrEmpty(myEnv) ? "" : "." + myEnv;

            Configuration = new ConfigurationBuilder().AddJsonFile($"appsettings{myEnv}.json", true, true).Build();

            #region AppSettings Configuration

            var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
            // builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            //builder.Configuration.AddJsonFile($"appsettings.{myEnv}.json");
            builder.AddAppSettingsConfiguration();

            #endregion



            // CORS Configuration
            builder.AddCorsConfiguration(appSettings);



            // Add services to the container.
            var p = builder.Services.BuildServiceProvider();

            builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddJsonOptions(x =>
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            builder.Services.AddControllers()
                .AddFluentValidation(options =>
                {
                    // Validate child properties and root collection elements
                    options.ImplicitlyValidateChildProperties = true;
                    options.ImplicitlyValidateRootCollectionElements = true;

                    // Automatic registration of validators in assembly
                    options.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
                });

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

                    Title = "Upsilon API",
                    Description = "<b>Upsilon</b>",
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

            ObjectMapper.Register<MappingProfile>();

            builder.Services.AddHttpContextAccessor();

            // Dependency Injection Configuration
            builder.Services.RegisterDIContainers();

            builder.Services.AddDistributedMemoryCache();
            var app = builder.Build();

            // Configure the HTTP request pipeline.

            #region Swagger
            if (!app.Environment.IsProduction())
            {
                app.UseSwagger(c =>
                {

                    c.RouteTemplate = "swagger/{documentname}/swagger.json";
                    c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                    {
                        swaggerDoc.Servers = new System.Collections.Generic.List<OpenApiServer>();


                        swaggerDoc.Servers.Add(new OpenApiServer { Url = $"https://{httpReq.Host.Value}/" });

                    });
                });

                app.UseSwaggerUI(c =>
                {
                    string apiTitle = "Upsilon API Test Service V 1.0.10";
                    //   if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Local")
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", apiTitle);


                    c.RoutePrefix = "doc";
                });
            }
            #endregion

            app.MapControllerRoute("default", "{controller}/{action}/{id?}", "api");



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


    }
}
