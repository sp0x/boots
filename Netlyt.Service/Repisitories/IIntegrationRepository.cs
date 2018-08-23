using System.Linq;
using Donut.Data;

namespace Netlyt.Service.Repisitories
{
    public interface IDonutRepository
    {

    }
    public interface IIntegrationRepository
    {
        IQueryable<DataIntegration> GetById(long id);
    }
}