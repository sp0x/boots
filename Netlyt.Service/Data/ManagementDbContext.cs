using Donut;
using Donut.Data;
using Donut.Models;
using Donut.Source;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.Service.Data
{
    public class ManagementDbContext : IdentityDbContext<User>, IDbContext
    {
        public DbSet<DataIntegration> Integrations { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<Rule> Rules { get; set; }
        public DbSet<FieldDefinition> Fields { get; set; }
        public DbSet<FieldExtras> FieldExtras { get; set; }
        public DbSet<FieldExtra> FieldExtra { get; set; }
        public DbSet<ApiAuth> ApiKeys { get; set; }
        public DbSet<ApiRateLimit> Rates { get; set; }
        public DbSet<ApiUser> ApiUsers { get; set; }
        public DbSet<FeatureGenerationTask> FeatureGenerationTasks { get; set; }
        public DbSet<DonutScriptInfo> DonutScripts { get; set; }
        public DbSet<ModelTrainingPerformance> ModelTrainingPerformance { get; set; }
        public DbSet<TrainingTask> TrainingTasks { get; set; }
        public DbSet<AggregateKey> AggregateKeys { get; set; }
        public DbSet<ModelTarget> ModelTargets { get; set; }
        public static readonly LoggerFactory Logger
            = new LoggerFactory(new[]
            {
                new ConsoleLoggerProvider((category, level)
                    => category == DbLoggerCategory.Database.Command.Name
                       && (level == LogLevel.Critical
                           || level == LogLevel.Error
                           || level == LogLevel.Warning), true)
            });

        public ManagementDbContext(DbContextOptions<ManagementDbContext> options)
            : base(options)
        {
            //Console.WriteLine("Initialized context with options: " + options.ToString());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(Logger);
            base.OnConfiguring(optionsBuilder);
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
            builder.Entity<AggregateKey>()
                .HasOne(x => x.Operation);
            builder.Entity<DonutFunction>()
                .Property(x => x._Parameters).HasColumnName("Parameters");
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
