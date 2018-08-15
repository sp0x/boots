using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Donut;
using Donut.Orion;
using EntityFramework.DbContextScope;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Service;
using Netlyt.Service.Cloud;
using Netlyt.Service.Data;

namespace Netlyt.Client
{
    public partial class Startup
    {
        public IOrionContext OrionContext { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            DBConfig.GetInstance(Configuration);
            var dbOptions = Configuration.GetDbOptionsBuilder();
            var postgresConnectionString = PersistanceSettings.GetPostgresConnectionString(Configuration);
            Console.WriteLine("Management DB at: " + postgresConnectionString);
            services.AddDbContext<ManagementDbContext>(options =>
                {
                    options.UseNpgsql(postgresConnectionString);
                    options.UseLazyLoadingProxies();
                }
            );
            services.AddTransient<IDbContextFactory>((sp) => new DbContextFactory(dbOptions));
            services.AddTransient<IDbContextScopeFactory>((sp) => new DbContextScopeFactory(sp.GetService<IDbContextFactory>()));
            services.AddTransient<ILogger>(x => x.GetService<ILoggerFactory>().CreateLogger("Netlyt.Client.Logs"));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IRateService, RateService>();
            services.AddTransient<UserService>();
            services.AddTransient<ApiService>();
            services.AddTransient<TimestampService>();
            services.AddTransient<ModelService>();
            services.AddTransient<OrganizationService>();
            services.AddTransient<IDonutService, DonutService>();
            services.AddTransient<IIntegrationService, IntegrationService>();
            SlaveConnector = new SlaveConnector(Configuration);
            SlaveConnector.Run();
            services.AddSingleton<SlaveConnector>(SlaveConnector);
            SlaveConnector.Send("Pesho is here.");

            OrionContext = services.RegisterOrionContext(Configuration.GetSection("behaviour"), x => { });
            ConfigureBackgroundServices(services.BuildServiceProvider());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
