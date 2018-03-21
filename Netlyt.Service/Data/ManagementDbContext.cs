using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using nvoid.Integration;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex.Data;
using Netlyt.Service.Ml;
using Netlyt.Service.Models;
using Netlyt.Service.Source;

namespace Netlyt.Service.Data
{
    public class ManagementDbContext : IdentityDbContext<User>
    {
        public DbSet<DataIntegration> Integrations { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<Rule> Rules { get; set; }
        public DbSet<FieldDefinition> Fields { get; set; }
        public DbSet<FieldExtras> FieldExtras { get; set; }
        public DbSet<ApiAuth> ApiKeys { get; set; }
        public DbSet<ApiUser> ApiUsers { get; set; }
        public DbSet<FeatureGenerationTask> FeatureGenerationTasks { get; set; }
        public DbSet<DonutScriptInfo> DonutScripts { get; set; }
        public DbSet<ModelTrainingPerformance> ModelPerformance { get; set; }

        public ManagementDbContext(DbContextOptions<ManagementDbContext> options)
            : base(options)
        {
            //Console.WriteLine("Initialized context with options: " + options.ToString());
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            //            builder.Entity<DataEventRecord>().HasKey(m => m.DataEventRecordId);
            //            builder.Entity<SourceInfo>().HasKey(m => m.SourceInfoId);
            //
            //            // shadow properties
            //            builder.Entity<DataEventRecord>().Property<DateTime>("UpdatedTimestamp");
            //            builder.Entity<SourceInfo>().Property<DateTime>("UpdatedTimestamp");
            builder.Entity<ModelRule>().HasKey(t => new {t.ModelId, t.RuleId});
            builder.Entity<ModelIntegration>().HasKey(t => new {t.ModelId, t.IntegrationId});
            builder.Entity<ApiUser>().HasKey(t => new {t.ApiId, t.UserId});
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
