﻿using Core.DBContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ReportEntities;

namespace ReportData
{
    public class ReportContext : DbContext, IDBContext
    {
        protected readonly IConfiguration Configuration;
        string myEnv = "";
        public ReportContext(DbContextOptions options, IConfiguration configuration) : base(options)
        {
            myEnv = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{myEnv}.json", false)
                .Build();
        }

      
        public virtual DbSet<Report> Reports { get; set; }

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
