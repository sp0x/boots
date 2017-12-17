using Microsoft.EntityFrameworkCore;

namespace Netlyt.Data
{
    public class ManagementDbContext : DbContext
    {
        public ManagementDbContext()
        {
            
        }

        public ManagementDbContext(DbContextOptions<ManagementDbContext> options)
            : base(options)
        {
            
        }
    }
}
