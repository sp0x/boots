using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Netlyt.Service.Data;
using Netlyt.Service.Ml;

namespace Netlyt.Service
{
    public class ModelService
    {
        private IHttpContextAccessor _contextAccessor;
        private ManagementDbContext _context;

        public ModelService(ManagementDbContext context,
            IHttpContextAccessor ctxAccessor)
        {
            _contextAccessor = ctxAccessor;
            _context = context;
        }

        public IEnumerable<Model> GetAllForUser(User user, int page)
        {
            int pageSize = 25;
            return _context.Models
                .Where(x => x.User == user)
                .Skip(page * pageSize)
                .Take(pageSize);
        }

        public Model GetById(long id)
        {
            return _context.Models.FirstOrDefault(t => t.Id == id);
        }

        public Task<Model> CreateModel(User user, string Name)
        {
            var newModel = new Model();
            newModel.User = user;
            newModel.ModelName = Name;
            _context.Models.Add(newModel);
            _context.SaveChanges();
            return Task.FromResult<Model>(newModel);
        }

        public void DeleteModel(User cruser, long id)
        {
            var targetModel = _context.Models.FirstOrDefault(x => x.User == cruser && x.Id == id);
            if (targetModel != null)
            {
                _context.Models.Remove(targetModel);
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}