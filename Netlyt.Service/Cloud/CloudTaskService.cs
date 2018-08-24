using Donut.Data;
using EntityFramework.DbContextScope.Interfaces;
using Netlyt.Service.Cloud.Slave;
using Netlyt.Service.Repisitories;

namespace Netlyt.Service.Cloud
{
    public class CloudTaskService : ICloudTaskService
    {
        private ISlaveConnector _connector;
        private IIntegrationRepository _integrations;
        private IDbContextScopeFactory _dbContextFactory;

        public CloudTaskService(
            ISlaveConnector connector,
            IIntegrationRepository integrations,
            IDbContextScopeFactory dbContextFactory)
        {
            _connector = connector;
            _integrations = integrations;
            _dbContextFactory = dbContextFactory;
        }

        public void TrainModel(DataIntegration integration)
        {

        }
    }
}