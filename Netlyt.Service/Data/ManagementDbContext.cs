using System;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using nvoid.Integration;
using Netlyt.Service.Integration;
using Netlyt.Service.Ml;
using Netlyt.Service.Source;

namespace Netlyt.Service.Data
{
    public class ManagementDbContext : DbContext
    {
        public DbSet<DataIntegration> Integrations { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> Roles { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<Rule> Rules { get; set; }
        public DbSet<FieldDefinition> Fields { get; set; }
        public DbSet<FieldExtras> FieldExtras { get; set; }
        public DbSet<ApiAuth> ApiKeys { get; set; }
         

        public ManagementDbContext(DbContextOptions<ManagementDbContext> options)
            : base(options)
        { 
            //Console.WriteLine("Initialized context with options: " + options.ToString());
            options = options;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
//            builder.Entity<DataEventRecord>().HasKey(m => m.DataEventRecordId);
//            builder.Entity<SourceInfo>().HasKey(m => m.SourceInfoId);
//
//            // shadow properties
//            builder.Entity<DataEventRecord>().Property<DateTime>("UpdatedTimestamp");
//            builder.Entity<SourceInfo>().Property<DateTime>("UpdatedTimestamp");

            base.OnModelCreating(builder);
        }

//        public override int SaveChanges()
//        {
//            ChangeTracker.DetectChanges();
////
////            updateUpdatedProperty<SourceInfo>();
////            updateUpdatedProperty<DataEventRecord>();
//
//            return base.SaveChanges();
//        } 
    }
}
