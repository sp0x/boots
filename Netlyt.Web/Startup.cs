﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nvoid.db.DB.Configuration;
using Netlyt.Data;
using Netlyt.Service;
using Netlyt.Service.Auth;
using Netlyt.Web.Middleware;
using Netlyt.Web.Middleware.Hmac;
using Netlyt.Web.Models.DataModels;
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
            var mongoConnectionString = DBConfig.GetGeneralDatabase().Value;
            services.AddDbContext<ManagementDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("PostgreSQLConnection"));
            });
            services.AddIdentity<User, UserRole>()
                .AddEntityFrameworkStores<ManagementDbContext>()
                .AddDefaultTokenProviders();

//            services.AddIdentityWithMongoStoresUsingCustomTypes<ApplicationUser, IdentityRole>(mongoConnectionString)
//                .AddDefaultTokenProviders();

            services.AddHmacAuthentication(); 
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
            //            app.UseMvc(routes =>
            //            {
            //                routes.MapRoute(
            //                    name: "default",
            //                    template: "{controller=Home}/{action=Index}/{id?}");
            //            });
        }
    }
}
