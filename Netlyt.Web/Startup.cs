using System;
using Donut;
using Donut.Orion;
using EntityFramework.DbContextScope;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Cloud;
using Netlyt.Service.Cloud.Slave;
using Netlyt.Service.Data;
using Netlyt.Web.Middleware;
using Netlyt.Web.Middleware.Hmac;
using Newtonsoft.Json;

namespace Netlyt.Web
{
    public partial class Startup
    {
        public IConfiguration Configuration { get; private set; }
        public IHostingEnvironment HostingEnvironment { get; }
        public IOrionContext OrionContext { get; private set; }

        public Startup(IHostingEnvironment env)
        {
            HostingEnvironment = env;
            var cfgBuilder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = cfgBuilder.Build();
            DBConfig.GetInstance(Configuration);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Register identity framework services and also Mongo storage. 
            var dbConfigInstance = DBConfig.GetInstance(Configuration);
            var databaseConfiguration = DBConfig.GetInstance().GetGeneralDatabase();
            if (databaseConfiguration == null) throw new Exception("No database configuration for `general` db!");
            var dbOptions = Configuration.GetDbOptionsBuilder();
            services.AddManagementDbContext(Configuration);
            //services.AddScoped<IDataAccessProvider, DataAccessPostgreSqlProvider.DataAccessPostgreSqlProvider>();
            services.AddTransient<IDatabaseConfiguration>((sp) =>
                {
                    return DonutDbConfig.GetOrAdd("general",
                        DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig());
                });
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            });
            services.AddCors();
            services.AddIdentity<User, UserRole>()
                .AddEntityFrameworkStores<ManagementDbContext>()
                .AddDefaultTokenProviders();
            services.AddApiIdentity();
//            services.AddTransient<IUserStore<User>, UserStore<User>>();

            //            services.AddIdentityWithMongoStoresUsingCustomTypes<ApplicationUser, IdentityRole>(mongoConnectionString)
            //                .AddDefaultTokenProviders();

            services.AddMemoryCache();
            services.AddSession();
            services.AddSingleton(Configuration);
            services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache 
            services.AddCache();
            // Add application services.
            services.AddSingleton<RoutingConfiguration>(new RoutingConfiguration(Configuration));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            OrionContext = services.RegisterOrionContext(Configuration.GetSection("behaviour"), x => { });

            services.AddSingleton<SocialNetworkApiManager>(new SocialNetworkApiManager());
            services.AddTransient<UserManager<User>>();
            services.AddTransient<SignInManager<User>>();
            services.AddTransient<CompilerService>();

            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
            services.AddTransient<ICloudTaskService, CloudTaskService>();
            services.AddTransient<IDbContextFactory>((sp) => new DbContextFactory(dbOptions));
            services.AddTransient<IDbContextScopeFactory>((sp) => new DbContextScopeFactory(sp.GetService<IDbContextFactory>()));

            services.AddTransient<IFactory<ManagementDbContext>, DynamicContextFactory>(s =>
                new DynamicContextFactory(() =>
                {
                    return new ManagementDbContext(dbOptions.Options);
                })
            );
            services.AddTransient<ILogger>(x => x.GetService<ILoggerFactory>().CreateLogger("Netlyt.Web.Logs"));
            services.AddTransient<IRateService, RateService>();
            services.AddTransient<ApiService>();
            services.AddTransient<TimestampService>();
            services.AddTransient<ModelService>();
            services.AddTransient<OrganizationService>();
            services.AddTransient<PermissionService>();
            services.AddTransient<IDonutService, DonutService>();
            services.AddTransient<IIntegrationService, IntegrationService>();
            services.AddTransient<INotificationService, NotificationService>();
            services.AddSingleton<ICloudNodeService, CloudNodeService>();
            services.AddSingleton<ISlaveConnector, SlaveConnector>();
            services.AddRepositories();
            

            SetupAuthentication(services);
            //services.AddAutoMapper(mc => { mc.AddProfiles(GetType().Assembly); });
            services.AddDomainAutomapper();
            services.AddMvc();
            var builtServices = services.BuildServiceProvider();
            OrionContext = builtServices.GetService<IOrionContext>();
            ConfigureBackgroundServices(builtServices);
        }

        public void SetupAuthentication(IServiceCollection services)
        {
            services.AddHmacAuthentication();
            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.Cookie.Expiration = TimeSpan.FromDays(500);
                //options.Cookie.Domain = ".netlyt.com";
                options.LoginPath = "/user/Login"; // If the LoginPath is not set here, ASP.NET Core will default to /Account/Login
                options.LogoutPath = "/user/Logout"; // If the LogoutPath is not set here, ASP.NET Core will default to /Account/Logout
                options.AccessDeniedPath = "/user/AccessDenied"; // If the AccessDeniedPath is not set here, ASP.NET Core will default to /Account/AccessDenied
                options.SlidingExpiration = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();
            app.UseAuthentication();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseSession();
            app.UseStaticFiles();
            var routeHelper = app.ApplicationServices.GetService<RoutingConfiguration>();
            app.MapWhen(ctx => routeHelper.MatchesForRole("api", ctx), appx => SetupApi(app));
            SetupApi(app); // We run standartly as api..
            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Home}/{action=Index}/{id?}");
            //});
            InitializeDatabase(app);
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var managementDbContext = scope.ServiceProvider.GetRequiredService<ManagementDbContext>();
                try
                {
                    managementDbContext.Database.Migrate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Migration failed: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Setup api.* requests
        /// </summary>
        /// <param name="app"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        private static void SetupApi(IApplicationBuilder app)
        {
            app.UseCors(builder =>
            {
                builder
                .AllowAnyMethod()
                .WithHeaders("Set-Cookie")
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowCredentials();
            });
            var cookiePolicyOptions = new CookiePolicyOptions
            {
                Secure = CookieSecurePolicy.SameAsRequest,
                MinimumSameSitePolicy = SameSiteMode.None
            };
            app.UseCookiePolicy(cookiePolicyOptions);
            app.UseEnableRequestRewind();
            app.UseSession();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            app.UseAuthentication();
            app.Run(async (context) =>
            {
                if (true)//!context.User.Identity.IsAuthenticated)
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                    {
                        error = "Route not found.",
                        success = false
                    }));
                    //await context.Response.Flush();
                }
                else
                {
                }
            });
        }
    }
}
