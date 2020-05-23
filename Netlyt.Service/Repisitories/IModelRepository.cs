using System.Linq;
using Donut;
using Donut.Models;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Repisitories
{
    public interface IModelRepository
    {
        IQueryable<Model> GetById(long id);
        IQueryable<Model> GetById(long id, User user);
        void Add(Model newModel);
    }

}