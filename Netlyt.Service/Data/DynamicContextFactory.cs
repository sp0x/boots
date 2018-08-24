using System;
using System.Collections.Generic;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Netlyt.Interfaces.Models;

namespace Netlyt.Service.Data
{ 
    public class DynamicContextFactory : IFactory<ManagementDbContext>
    {
        private Func<ManagementDbContext> _generator;

        public DynamicContextFactory(Func<ManagementDbContext> generator)
        {
            _generator = generator;
        }
        public ManagementDbContext Create()
        {
            return _generator();
        }
    }

    public class DbContextFactory : IDbContextFactory
    {
        private DbContextOptionsBuilder<ManagementDbContext> dbOptions;

        public DbContextFactory(DbContextOptionsBuilder<ManagementDbContext> dbOptions)
        {
            this.dbOptions = dbOptions;
        }

        public TDbContext CreateDbContext<TDbContext>() where TDbContext : class, IDbContext
        {
            if (typeof(TDbContext) == typeof(ManagementDbContext))
            {
                return new ManagementDbContext(dbOptions.Options) as TDbContext;
            }
            else
            {
                return Activator.CreateInstance(typeof(TDbContext)) as TDbContext;
            }
        }
    }
}