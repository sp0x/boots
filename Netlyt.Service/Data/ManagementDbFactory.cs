namespace Netlyt.Service.Data
{
    public class ManagementDbFactory : IFactory<ManagementDbContext>
    {
        public ManagementDbContext Create()
        {

            return new ManagementDbContext();
        }
    }
}