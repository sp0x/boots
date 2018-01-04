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

        public ManagementDbContext()
        {
            
        }

        public ManagementDbContext(DbContextOptions<ManagementDbContext> options)
            : base(options)
        {
            
        }
    }
}
