using System;
using System.Collections.Generic;
using System.Linq;
using Donut;
using Donut.Source;
using Microsoft.EntityFrameworkCore;
using Netlyt.Interfaces;
using Netlyt.Service.Data;
using Netlyt.Service.Integration;

namespace Netlyt.Service
{
    public class TimestampService
    {
        private List<string> _possibleColumns;
        private ManagementDbContext _db;
        private Type _tmType = typeof(DateTime);
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
                    bool isTs = IsTimestamp(field);
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

        private bool IsTimestamp(IFieldDefinition field)
        {
            return field.Type == _tmType.FullName;//_possibleColumns.Any(x => x == field.Name);
        }
    }
}