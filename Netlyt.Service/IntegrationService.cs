using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netlyt.Data;
using Netlyt.Service.Data;
using Netlyt.Service.Integration;

namespace Netlyt.Service
{
    public class IntegrationService
    {
        private IFactory<ManagementDbContext> _factory;
        private ManagementDbContext _context;

        public IntegrationService(ManagementDbContext context)
        {
            _context = context;
        }

        public void SaveOrFetchExisting(ref DataIntegration type)
        {
            DataIntegration exitingIntegration;
            if (!Exists(type, type.APIKey.AppId, out exitingIntegration))
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
        public bool Exists(IIntegration type, string appId, out DataIntegration existingDefinition)
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
        public bool Exists(IIntegration type, long apiId, out DataIntegration existingDefinition)
        {
            existingDefinition = _context.Integrations.FirstOrDefault(x => x.APIKey.Id == apiId && (x.Fields == type.Fields || x.Name == type.Name));
            return existingDefinition != null;
        }
    }
}
