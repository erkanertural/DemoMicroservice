using ContactEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Core.DBContext;

namespace ContactData
{
    public class ContactContext : DbContext, IDBContext
    {
        protected readonly IConfiguration Configuration;

        public ContactContext(DbContextOptions options, IConfiguration configuration) : base(options)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            Configuration = configuration;
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
