using ContactEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Core.DBContext;

namespace ContactData
{
    public class ContactContext : DbContext, IDBContext
    {
        protected readonly IConfiguration Configuration;
        string myEnv = "";
        public ContactContext(DbContextOptions options, IConfiguration configuration) : base(options)
        {
            myEnv = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (myEnv is null)
            { 
             Configuration = new ConfigurationBuilder()                .AddJsonFile($"appsettings.development.json", false)                .Build();
            }
            else
            Configuration = new ConfigurationBuilder()                .AddJsonFile($"appsettings.{myEnv}.json", false)                .Build();
        }

      
        public virtual DbSet<Contact> Contacts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = Configuration.GetConnectionString("Default");
                optionsBuilder.EnableSensitiveDataLogging(true);
                optionsBuilder.UseNpgsql(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<User>()         .Property(b => b.Email).HasMaxLength(100).IsRequired();
           

            base.OnModelCreating(modelBuilder);
        }
    }
}
