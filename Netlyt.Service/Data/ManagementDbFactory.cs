using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Netlyt.Service.Data
{
    public class ManagementDbFactory : IFactory<ManagementDbContext>
    { 
        private DbContextOptionsBuilder<ManagementDbContext> _optionsBuilder;
        public ManagementDbFactory(DbContextOptionsBuilder<ManagementDbContext> optionsBuilder)
        {
            _optionsBuilder = optionsBuilder;
        }
        public ManagementDbContext Create()
        { 
            return new ManagementDbContext(_optionsBuilder.Options);
        }
    }
}