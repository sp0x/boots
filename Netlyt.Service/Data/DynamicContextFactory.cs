using System;

namespace Netlyt.Service.Data
{
    public class DynamicContextFactory : IFactory<ManagementDbContext>
    {
        private Func<ManagementDbContext> _generator;

        public DynamicContextFactory(Func<ManagementDbContext> generator)
        {
            _generator = generator;
        }
        public ManagementDbContext Create()
        {
            return _generator();
        }
    }
}