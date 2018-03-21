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
            _possibleColumns = new List<string>(new string[]{ "timestamp", "on_date", "added_on" });
            _db = db;
        }
        public string Discover(DataIntegration ign)
        {
            var fields = _db.Integrations
                    .Include(x=>x.Fields)
                    .FirstOrDefault(x => x.Id == ign.Id)?.Fields;
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
    }
}