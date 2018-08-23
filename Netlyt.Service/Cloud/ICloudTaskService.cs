using Donut.Data;

namespace Netlyt.Service.Cloud
{
    public interface ICloudTaskService
    {
        void TrainModel(DataIntegration integration);
    }
}