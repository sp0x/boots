using System;
using System.Collections.Generic;
using System.Linq;
using Donut;
using Donut.Source;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.EntityFrameworkCore;
using Netlyt.Interfaces;
using Netlyt.Service.Data;
using Netlyt.Service.Integration;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.Service
{
    public class TimestampService
    {
        private List<string> _possibleColumns;
        private IDbContextScopeFactory _dbContextFactory;
        private Type _tmType = typeof(DateTime);
        public TimestampService(
            IDbContextScopeFactory dbContextFactory)
        {
            _possibleColumns = new List<string>(new string[]{ "timestamp", "on_date", "added_on", "time" });
            _dbContextFactory = dbContextFactory;
        }

        public string DiscoverByIntegrationId(long ignId)
        {
            using (var ctxSrc = _dbContextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var fields = context.Integrations
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