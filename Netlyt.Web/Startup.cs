using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nvoid.db.DB.Configuration;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Web.Middleware;
using Netlyt.Web.Middleware.Hmac;
using Netlyt.Web.Services;
using IdentityRole = Microsoft.AspNetCore.Identity.MongoDB.IdentityRole;

namespace Netlyt.Web
{
    public class Startup
    {

        public IConfiguration Configuration { get; private set; }
        private BehaviourContext BehaviourContext { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            DBConfig.Initialize(Configuration);
            BehaviourContext = new BehaviourContext();
            BehaviourContext.Configure(Configuration.GetSection("behaviour"));
            BehaviourContext.Run();
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Register identity framework services and also Mongo storage. 
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            if (databaseConfiguration == null) throw new Exception("No database configuration for `general` db!");
            var mongoConnectionString = databaseConfiguration.Value;
            var postgresConnectionString = Configuration.GetConnectionString("PostgreSQLConnection"); 
            services.AddDbContext<ManagementDbContext>(options =>
                options.UseNpgsql(postgresConnectionString)
            );
            //services.AddScoped<IDataAccessProvider, DataAccessPostgreSqlProvider.DataAccessPostgreSqlProvider>();
             
            services.AddIdentity<User, UserRole>()
                .AddEntityFrameworkStores<ManagementDbContext>()
                .AddDefaultTokenProviders();

            //            services.AddIdentityWithMongoStoresUsingCustomTypes<ApplicationUser, IdentityRole>(mongoConnectionString)
            //                .AddDefaultTokenProviders();

            services.AddMemoryCache();
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.CookieHttpOnly = true;
            });
            //330
            services.AddMvc();
            services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache 
            // Add application services.
            services.AddSingleton<RoutingConfiguration>(new RoutingConfiguration(Configuration));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<BehaviourContext>(this.BehaviourContext);
            services.AddSingleton<SocialNetworkApiManager>(new SocialNetworkApiManager());
            services.AddTransient<UserManager<User>>();
            services.AddTransient<IntegrationService>();
            services.AddTransient<SignInManager<User>>();
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
            SetupAuthentication(services);
        }

        public void SetupAuthentication(IServiceCollection services)
        {
            services.AddHmacAuthentication();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => {
                    options.LoginPath = "/user/Login/";
                });
            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.Cookie.Expiration = TimeSpan.FromDays(150);
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
            app.MapWhen(ctx => routeHelper.MatchesForRole("api", ctx), builder =>
            {
                app.UseEnableRequestRewind();
                builder.UseAuthentication();
                //                builder.UseHmacAuthentication(new HmacOptions()
                //                {
                //                    MaxRequestAgeInSeconds = 300,
                //                    AutomaticAuthenticate = true
                //                });
                builder.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "default",
                        template: "{controller=Home}/{action=Index}/{id?}");
                });
                builder.Run(async (context) =>
                {
                    if (!context.User.Identity.IsAuthenticated)
                    {
                        context.Response.StatusCode = 401;
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
            });
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
            //            app.UseMvc(routes =>
            //            {
            //                routes.MapRoute(
            //                    name: "default",
            //                    template: "{controller=Home}/{action=Index}/{id?}");
            //            });
        }
    }
}
