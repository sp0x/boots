﻿using System;
using System.Diagnostics;
using System.Linq;
using Donut.Models;
using EntityFramework.DbContextScope.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;

namespace Netlyt.Service.Repisitories
{
    public class ModelRepository : IModelRepository
    {
        private readonly IAmbientDbContextLocator _ambientDbContextLocator;
        public ManagementDbContext DbContext
        {
            get
            {
                var dbContext = _ambientDbContextLocator.Get<ManagementDbContext>();

                if (dbContext == null)
                    throw new InvalidOperationException("No ambient DbContext of type ManagementDbContext found. This means that this repository method has been called outside of the scope of a DbContextScope. A repository must only be accessed within the scope of a DbContextScope, which takes care of creating the DbContext instances that the repositories need and making them available as ambient contexts. This is what ensures that, for any given DbContext-derived type, the same instance is used throughout the duration of a business transaction. To fix this issue, use IDbContextScopeFactory in your top-level business logic service method to create a DbContextScope that wraps the entire business transaction that your service method implements. Then access this repository within that scope. Refer to the comments in the IDbContextScope.cs file for more details.");

                return dbContext;
            }
        }

        public ModelRepository(IAmbientDbContextLocator ambientDbContextLocator)
        {
            if (ambientDbContextLocator == null) throw new ArgumentNullException(nameof(ambientDbContextLocator));
            _ambientDbContextLocator = ambientDbContextLocator;
        }

        public IQueryable<Model> GetById(long id)
        {
            return DbContext.Models.Where(x => x.Id == id);
        }

        public IQueryable<Model> GetById(long id, User user)
        {
            return DbContext.Models.Where(x => x.Id == id && x.UserId == user.Id);
        }
        public void Add(Model newModel)
        {
            try
            {
                DbContext.Models.Add(newModel);
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                throw new Exception("Could not create a new model!");
            }
        }
    }
}