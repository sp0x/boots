using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Netlyt.Service.Data;
using Netlyt.Service.Integration;

namespace Netlyt.Service
{
    public class TimestampService
    {
        private List<string> _possibleColumns;
        private ManagementDbContext _db;

        public TimestampService(ManagementDbContext db)
        {
            _possibleColumns = new List<string>(new string[]{ "timestamp", "on_date", "added_on", "time" });
            _db = db;
        }

        public string DiscoverByIntegrationId(long ignId)
        {
            var fields = _db.Integrations
                .Include(x => x.Fields)
                .FirstOrDefault(x => x.Id == ignId)?.Fields;
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    bool isTs = _possibleColumns.Any(x => x == field.Name);
                    if (isTs)
                    {
                        return field.Name;
                    }
                }
            }
            return null;
        }
        public string Discover(DataIntegration ign)
        {
            if (ign!=null && ign.Fields != null)
            {
                foreach (var field in ign.Fields)
                {
                    bool isTs = _possibleColumns.Any(x => x == field.Name);
                    if (isTs)
                    {
                        return field.Name;
                    }
                }
            }else if (ign != null && ign.Fields == null)
            {
                return DiscoverByIntegrationId(ign.Id);
            }
            return null;
        }
    }
}