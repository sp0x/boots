using System;
using AutoMapper;
using Donut;
using Donut.Orion;
using EntityFramework.DbContextScope;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;
using Netlyt.Web.Extensions;
using Netlyt.Web.Services;

namespace Netlyt.Web
{
    public partial class Startup
    {
        public ServiceProvider BackgroundServiceProvider { get; set; }
        public DonutOrionHandler OrionHandler { get; set; }
        public void ConfigureBackgroundServices(IServiceProvider mainServices)
        {
            NetlytService.SetupBackgroundServices(GetDbOptionsBuilder(), Configuration, OrionContext);
        }

    }
}
