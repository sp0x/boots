using System.Linq;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Repisitories
{
    public interface IUsersRepository
    {
        IQueryable<User> GetById(string id);
        User GetByUsername(string toString);
        void CreateIfMissing(User user);
    }
}