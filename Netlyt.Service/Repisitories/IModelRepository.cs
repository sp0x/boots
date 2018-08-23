using System.Linq;
using Donut;
using Donut.Models;

namespace Netlyt.Service.Repisitories
{
    public interface IModelRepository
    {
        IQueryable<Model> GetById(long id);
        void Add(Model newModel);
    }

}