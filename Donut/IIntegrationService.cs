using Donut.Integration;
using Netlyt.Interfaces;

namespace Donut
{
    public interface IIntegrationService
    {
        IIntegration GetByName(IApiAuth contextApiAuth, string integrationSourceIntegrationName);
    }
}