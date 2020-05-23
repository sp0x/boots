using Donut.Data;

namespace Netlyt.Service.Repisitories
{
    public interface IPermissionRepository
    {
        Permission GetById(long newPermId);
    }
}