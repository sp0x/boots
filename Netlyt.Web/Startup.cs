using System;
using System.Collections.Generic;
using AutoMapper;
using Donut;
using Donut.Caching;
using Donut.Orion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;
using Netlyt.Web.Middleware;
using Netlyt.Web.Middleware.Hmac;
using Netlyt.Web.Services;
using Newtonsoft.Json;

namespace Netlyt.Web
{
    public partial class Startup
    {
        public IConfiguration Configuration { get; private set; }
        private IOrionContext OrionContext { get; set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            DBConfig.GetInstance(Configuration);
            //            var dbConfig = DBConfig.GetGeneralDatabase();
            //            var mongoList = new MongoList(dbConfig, "1145146c-bff3-495f-ac1c-40afce4536fd");
            //            var featuresCollection = new MongoList(dbConfig, "1145146c-bff3-495f-ac1c-40afce4536fd_ftr");
            //            featuresCollection.Truncate();
            //            var recRom = mongoList.Records;
            //            var groupKeys = new BsonDocument();
            //            var groupFields = new BsonDocument();
            //            var projections = new BsonDocument();
            //            var pipeline = new List<BsonDocument>();
            //
            //            groupFields.Merge(BsonDocument.Parse(@"{""f_0"":{""$min"":""$rssi""}}"));
            //            groupFields.Merge(BsonDocument.Parse(@"{""g1817192762"":{""$first"":""$timestamp""}}"));
            //
            //            projections.Merge(new BsonDocument { { "f_0", "$f_0" } });
            //            projections.Merge(BsonDocument.Parse(@"{""f_1"":{""$dayOfMonth"":""$g1817192762""}}"));
            //            projections.Merge(BsonDocument.Parse(@"{""f_2"":{""$year"":""$g1817192762""}}"));
            //            projections.Merge(BsonDocument.Parse(@"{""f_3"":{""$month"":""$g1817192762""}}"));
            //            projections.Merge(BsonDocument.Parse(@"{""f_4"":{""$dayOfWeek"":""$g1817192762""}}"));
            //
            //            var idSubKey1 = new BsonDocument { { "idKey", "$_id" } };
            //            var idSubKey2 = new BsonDocument { { "tsKey", new BsonDocument { { "$dayOfYear", "$timestamp" } } } };
            //            groupKeys.Merge(idSubKey1);
            //            groupKeys.Merge(idSubKey2);
            //            var grouping = new BsonDocument();
            //            grouping["_id"] = groupKeys;
            //            grouping = grouping.Merge(groupFields);
            //            pipeline.Add(new BsonDocument{
            //                {"$group", grouping}});
            //            pipeline.Add(new BsonDocument{
            //                {"$project", projections} });
            //            pipeline.Add(new BsonDocument{
            //                {"$out", featuresCollection.CollectionName}}); 
            //var aggOptions = new AggregateOptions(){ AllowDiskUse = true, BatchSize=1 }; 
            //            var aggregateResult = recRom.Aggregate<BsonDocument>(pipeline, aggOptions);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Register identity framework services and also Mongo storage. 
            var dbConfigInstance = DBConfig.GetInstance(Configuration);
            var databaseConfiguration = DBConfig.GetInstance().GetGeneralDatabase();
            if (databaseConfiguration == null) throw new Exception("No database configuration for `general` db!");
            var postgresConnectionString = Configuration.GetConnectionString("PostgreSQLConnection");
            services.AddDbContext<ManagementDbContext>(options =>
                {
                    options.UseNpgsql(postgresConnectionString);
                }
            );
            //services.AddScoped<IDataAccessProvider, DataAccessPostgreSqlProvider.DataAccessPostgreSqlProvider>();
            services.AddTransient<IDatabaseConfiguration>((sp) =>
                {
                    return DonutDbConfig.GetOrAdd("general",
                        DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig());
                });
            services.AddIdentity<User, UserRole>()
                .AddEntityFrameworkStores<ManagementDbContext>()
                .AddDefaultTokenProviders();

            //            services.AddIdentityWithMongoStoresUsingCustomTypes<ApplicationUser, IdentityRole>(mongoConnectionString)
            //                .AddDefaultTokenProviders();

            services.AddMemoryCache();
            services.AddSession();
            services.AddSingleton(Configuration);
            services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache 
            services.AddSingleton<IRedisCacher>(DBConfig.GetInstance().GetCacheContext());
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
            services.AddTransient<IFactory<ManagementDbContext>, DynamicContextFactory>(s =>
                new DynamicContextFactory(() =>
                {
                    var opsBuilder = new DbContextOptionsBuilder<ManagementDbContext>().UseNpgsql(postgresConnectionString);
                    return new ManagementDbContext(opsBuilder.Options);
                })
            );
            services.AddTransient<ILogger>(x => x.GetService<ILoggerFactory>().CreateLogger("Netlyt.Web.Logs"));
            services.AddTransient<UserService>();
            services.AddTransient<ApiService>();
            services.AddTransient<TimestampService>();
            services.AddTransient<ModelService>();
            services.AddTransient<OrganizationService>();
            services.AddTransient<IntegrationService>();
            //services.AddSingleton<DonutOrionHandler>();

            SetupAuthentication(services);
            services.AddAutoMapper();
            services.AddMvc();
            ConfigureBackgroundServices();
            //Enable for 100% auth coverage by default
            //            services.AddMvc(options =>
            //            {
            //                // All endpoints need authentication
            //                options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
            //            }); 
        }

        public void SetupAuthentication(IServiceCollection services)
        {
            services.AddHmacAuthentication();
            //            //Add cookie auth
            //            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            //                .AddCookie(options => {
            //                    options.LoginPath = "/user/Login/";
            //                });
            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.Cookie.Expiration = TimeSpan.FromDays(500);
                options.Cookie.Domain = ".netlyt.com";
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
            app.MapWhen(ctx => routeHelper.MatchesForRole("api", ctx), builder => SetupApi(app, builder));
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            InitializeDatabase(app);
            //            app.UseMvc(routes =>
            //            {
            //                routes.MapRoute(
            //                    name: "default",
            //                    template: "{controller=Home}/{action=Index}/{id?}");
            //            });
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            app.ApplicationServices.GetService<DonutOrionHandler>();
            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var managementDbContext = scope.ServiceProvider.GetRequiredService<ManagementDbContext>();
                managementDbContext.Database.Migrate();
            }
        }

        /// <summary>
        /// Setup api.* requests
        /// </summary>
        /// <param name="app"></param>
        /// <param name="builder"></param>
        /// <returns></returns>
        private static void SetupApi(IApplicationBuilder app, IApplicationBuilder builder)
        {
            app.UseEnableRequestRewind();
            app.UseSession();
            builder.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            builder.UseAuthentication();
            builder.Run(async (context) =>
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
                //context.User = await context.Authentication.AuthenticateAsync(HmacAuthenticationDefaults.AuthenticationScheme);
                //it should be True
            });
            //                builder.Run(async (context) =>
            //                {
            //                    //context.User = await context.Authentication.AuthenticateAsync(HmacAuthenticationDefaults.AuthenticationScheme);
            //                    //it should be True
            //                    await context.Response.WriteAsync(context.User.Identity.IsAuthenticated.ToString());
            //                });
        }
    }
}
