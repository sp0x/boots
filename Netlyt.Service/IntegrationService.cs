using System;
using System.Collections.Generic;
using System.Text;
using Netlyt.Data;
using Netlyt.Service.Data;
using Netlyt.Service.Integration;

namespace Netlyt.Service
{
    public class IntegrationService
    {
        private IFactory<ManagementDbContext> _factory;

        public IntegrationService(IFactory<ManagementDbContext> factory)
        {
            _factory = factory;
        }

        public void SaveType(DataIntegration type)
        {
            using (var context = _factory.Create())
            {
                context.Integrations.Add(type);
                context.SaveChanges();
            }
        }
    }
}
