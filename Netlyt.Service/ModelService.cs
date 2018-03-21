﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Netlyt.Service.Data;
using Netlyt.Service.Integration;
using Netlyt.Service.Ml;
using Netlyt.Service.Models;
using Netlyt.Service.Orion;

namespace Netlyt.Service
{
    public class ModelService
    {
        private IHttpContextAccessor _contextAccessor;
        private ManagementDbContext _context;
        private OrionContext _orion;
        private TimestampService _timestampService;

        public ModelService(ManagementDbContext context,
            OrionContext orionContext,
            IHttpContextAccessor ctxAccessor,
            TimestampService timestampService)
        {
            _contextAccessor = ctxAccessor;
            _context = context;
            _orion = orionContext;
            _timestampService = timestampService;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="name"></param> 
        /// <param name="integrations"></param>
        /// <param name="callbackUrl"></param>
        /// <param name="generateFeatures">If feature generation should be ran. This is only done if there are any existing integrations for this model.</param>
        /// <param name="relations"></param>
        /// <returns></returns>
        public async Task<Model> CreateModel(
            User user, 
            string name,
            IEnumerable<DataIntegration> integrations, 
            string callbackUrl,
            bool generateFeatures,
            IEnumerable<FeatureGenerationRelation> relations, 
            string targetAttribute
            )
        {
            var newModel = new Model();
            newModel.UserId = user.Id;
            newModel.ModelName = name;
            newModel.Callback = callbackUrl; 
            if (integrations != null)
            {
                newModel.DataIntegrations = integrations.Select(x => new ModelIntegration(newModel, x)).ToList(); 
            }
            _context.Models.Add(newModel);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                throw new Exception("Could not create a new model!");
            }
            if (generateFeatures)
            {
                await GenerateFeatures(newModel, relations, targetAttribute);
                //newModel.FeatureGenerationTasks.Add(newTask);
                //_context.FeatureGenerationTasks.Add(newTask);
                //_context.SaveChanges();
            }
            return newModel;
        }

        public async Task GenerateFeatures(Model newModel, IEnumerable<FeatureGenerationRelation> relations, string targetAttribute)
        {
            var collections = new List<FeatureGenerationCollectionOptions>();
            foreach (var integration in newModel.DataIntegrations)
            {
                var ign = integration.Integration;
                var ignTimestampColumn = _timestampService.Discover(ign);
                var fields = ign.Fields;
                InternalEntity intEntity = null;
                if (fields.Any(x => x.Name == targetAttribute))
                {
                    intEntity = new InternalEntity()
                    {
                        Name = targetAttribute
                    };
                }
                var colOptions = new FeatureGenerationCollectionOptions()
                {
                    Collection = ign.Collection,
                    Name = ign.Name,
                    TimestampField = ignTimestampColumn,
                    InternalEntity = intEntity
                    //Other parameters are ignored for now
                };
                collections.Add(colOptions);
            }

            var query = OrionQuery.Factory.CreateFeatureGenerationQuery(newModel, collections, relations, targetAttribute);
            var result = await _orion.Query(query);
//            var newTask = new FeatureGenerationTask();
//            newTask.OrionTaskId = result["task_id"].ToString();
//            newTask.Model = newModel;
//            return newTask;
        }

        public void DeleteModel(User cruser, long id)
        {
            var targetModel = _context.Models.FirstOrDefault(x => x.User == cruser && x.Id == id);
            if (targetModel != null)
            {
                _context.Models.Remove(targetModel);
                _context.SaveChanges();
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}