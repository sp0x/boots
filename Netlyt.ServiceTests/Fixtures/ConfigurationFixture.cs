using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Donut;
using Donut.Caching;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;

namespace Netlyt.ServiceTests.Fixtures
{
    public class ConfigurationFixture : IDisposable
    {
        private ManagementDbContext _context;
        private static Assembly _assembly = Assembly.GetExecutingAssembly();
        public DbContextOptionsBuilder<ManagementDbContext> DbOptionsBuilder { get; private set; } 
        public ServiceProvider ServiceProvider { get; set; }
        public IConfigurationRoot Configuration { get; set; }

        public ConfigurationFixture()
        {
            var p = Process.GetCurrentProcess();
            Debug.WriteLine($"Started test process: {p.Id}");
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();
            DbOptionsBuilder = new DbContextOptionsBuilder<ManagementDbContext>()
                .UseInMemoryDatabase("Testing")
                .EnableSensitiveDataLogging(true);
            var services = new ServiceCollection();
            _context = CreateContext();
            DBConfig.GetInstance(Configuration);
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }



        private void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<TimestampService>();
            services.AddDonutDb(DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig());
            services.AddDbContext<ManagementDbContext>(s => s.UseInMemoryDatabase("Testing"));
            services.AddTransient<ApiService>(s => new ApiService(_context, null));
            services.AddTransient<IntegrationService>(s => new IntegrationService(_context, 
                s.GetService<ApiService>(),
                s.GetService<UserService>(),
                s.GetService<TimestampService>(),
                s.GetService<IDatabaseConfiguration>()));
            var redisCacher = DBConfig.GetInstance().GetCacheContext();
            services.AddSingleton<IRedisCacher>(redisCacher);
            services.AddTransient<CompilerService>();
            services.AddTransient<UserService>(s => new UserService(s.GetService<UserManager<User>>(), s.GetService<ApiService>(), null, null,
                s.GetService<OrganizationService>(), s.GetService<ModelService>(), _context));
        }

        public ManagementDbContext CreateContext()
        {
            return new ManagementDbContext(DbOptionsBuilder.Options);
        }

        protected static Stream GetTemplate(string name)
        {
            var resourceName = $"Netlyt.ServiceTests.Res.{name}";
            Stream stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new Exception("Template not found!");
            }
            //StreamReader reader = new StreamReader(stream);
            return stream;
        }

        public T GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        public void Dispose()
        {

        }

    }
}