using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using nvoid.db.DB.Configuration;
using nvoid.db.Extensions;
using nvoid.Integration;
using Peeralize.Controllers;
using Peeralize.Middleware.Hmac;
using Peeralize.Middleware;
using Peeralize.Service;
using Peeralize.Service.Auth;
using Peeralize.Services;
using AuthMessageSender = Peeralize.Services.AuthMessageSender;
using IEmailSender = Peeralize.Services.IEmailSender;
using ISmsSender = Peeralize.Services.ISmsSender;

namespace Peeralize
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        private BehaviourContext BehaviourContext { get; }
        

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build(); 

            DBConfig.Initialize(Configuration);
            BehaviourContext = new BehaviourContext();
            BehaviourContext.Configure(Configuration.GetSection("behaviour"));
            BehaviourContext.Run(); 
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
	        // Register identity framework services and also Mongo storage. 
            var mongoConnectionString = DBConfig.GetGeneralDatabase().Value;

            services.AddIdentityWithMongoStoresUsingCustomTypes<ApplicationUser, IdentityRole>(mongoConnectionString)
                .AddDefaultTokenProviders();
            services.AddAuthentication();
            services.AddMemoryCache();
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.CookieHttpOnly = true;
            });
            services.AddMvc();
            services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache 

            // Add application services.
            services.AddSingleton<RoutingConfiguration>(new RoutingConfiguration(Configuration));
            services.AddSingleton<BehaviourContext>(this.BehaviourContext);
            services.AddSingleton<SocialNetworkApiManager>(new SocialNetworkApiManager());
            services.AddTransient<UserManager<ApplicationUser>>();
            services.AddTransient<SignInManager<ApplicationUser>>();
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseSession();
            app.UseStaticFiles();
            app.UseIdentity();
            var routeHelper = app.ApplicationServices.GetService<RoutingConfiguration>();
            // Add external authentication middleware below. To configure them please see http://go.microsoft.com/fwlink/?LinkID=532715
            //Map api.domain.... to api
            app.MapWhen(ctx => routeHelper.MatchesForRole("api", ctx), builder =>
            {
                app.UseEnableRequestRewind();
                builder.UseHmacAuthentication(new HmacOptions()
                {
                    MaxRequestAgeInSeconds = 300,
                    AutomaticAuthenticate = true
                });
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
                        await context.Response.WriteAsync(context.User.Identity.IsAuthenticated.ToString());
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
        }
    }
}
