using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Data;
using Netlyt.Service.Data;
using Netlyt.Service.Integration;

namespace Netlyt.Service
{
    public class IntegrationService
    {
        //private IFactory<ManagementDbContext> _factory;
        private ManagementDbContext _context;

        public IntegrationService(ManagementDbContext context)
        {
            _context = context;
        }

        public void SaveOrFetchExisting(ref DataIntegration type)
        {
            DataIntegration exitingIntegration;
            if (!IntegrationExists(type, type.APIKey.AppId, out exitingIntegration))
            {
                _context.Integrations.Add(type);
                _context.SaveChanges();
            }
            else
            {
                type = exitingIntegration;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="appId"></param>
        /// <param name="existingDefinition"></param>
        /// <returns></returns>
        public bool IntegrationExists(IIntegration type, string appId, out DataIntegration existingDefinition)
        {
            var localFields = type.Fields; 
            existingDefinition = (from x in _context.Integrations
                                  where x.APIKey.AppId == appId
                                        && x.Fields.All(f => localFields.Any(lf => lf.Name == f.Name))
                                  select x).FirstOrDefault();
            return existingDefinition != null;
        }

        //
        //        public override void PrepareForSaving()
        //        {
        //            base.PrepareForSaving();
        //            if (string.IsNullOrEmpty(APIKey))
        //                throw new InvalidOperationException("Only user owned type definitions can be saved!");
        //        }
         

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="apiId"></param>
        /// <param name="existingDefinition"></param>
        /// <returns></returns>
        public bool IntegrationExists(IIntegration type, long apiId, out DataIntegration existingDefinition)
        {
            existingDefinition = _context.Integrations.FirstOrDefault(x => x.APIKey.Id == apiId && (x.Fields == type.Fields || x.Name == type.Name));
            return existingDefinition != null;
        }
        /// <summary>
        /// Removes an integration and associated collections.
        /// </summary>
        /// <param name="importTaskIntegration"></param>
        public void Remove(DataIntegration importTaskIntegration)
        {
            _context.Integrations.Remove(importTaskIntegration);
            _context.SaveChanges();
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            var collection = new MongoList(databaseConfiguration, importTaskIntegration.Collection);
            collection.Trash();
            collection = null;
        }

        public DataIntegration GetById(long id)
        {
            return _context.Integrations.Find(id);
        }
    }
}
