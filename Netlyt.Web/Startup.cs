﻿using System; 
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity; 
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; 
using nvoid.db.DB.Configuration; 
using Netlyt.Web.Middleware.Hmac;
using Netlyt.Web.Middleware;
using Netlyt.Service;
using Netlyt.Service.Auth;
using Netlyt.Web.Services;
using AuthMessageSender = Netlyt.Web.Services.AuthMessageSender;
using IdentityRole = Microsoft.AspNetCore.Identity.MongoDB.IdentityRole;
using IEmailSender = Netlyt.Web.Services.IEmailSender;
using ISmsSender = Netlyt.Web.Services.ISmsSender;

namespace Netlyt.Web
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        private BehaviourContext BehaviourContext { get; }
        

        public Startup(IApplicationBuilder app, IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
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
