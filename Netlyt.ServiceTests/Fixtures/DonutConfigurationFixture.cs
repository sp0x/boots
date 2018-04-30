using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Donut;
using Donut.Caching;
using Donut.Orion;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;

namespace Netlyt.ServiceTests.Fixtures
{
    public class DonutConfigurationFixture : IDisposable
    {
        private ManagementDbContext _context;
        public DbContextOptionsBuilder<ManagementDbContext> DbOptionsBuilder { get; private set; }
        public ServiceProvider ServiceProvider { get; set; }
        private IOrionContext OrionContext { get; set; }
        public IConfigurationRoot Config { get; set; }
        private static Assembly _assembly = Assembly.GetExecutingAssembly();

        public DonutConfigurationFixture()
        {
            var p = Process.GetCurrentProcess();
            Debug.WriteLine($"Started test process: {p.Id}");
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
            Config = builder.Build();
            
            var services = new ServiceCollection();
            var postgresConnectionString = Config.GetConnectionString("PostgreSQLConnection");
            DbOptionsBuilder = new DbContextOptionsBuilder<ManagementDbContext>()
                .UseNpgsql(postgresConnectionString);
            _context = CreateContext();
            var dbConfig = DBConfig.GetInstance(Config);

            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider(); 
            ServiceProvider.GetService<DonutOrionHandler>();
        }

        public static String GetTemplate(string name)
        {
            var resourceName = $"Netlyt.ServiceTests.Res.{name}";
            Stream stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new Exception("Template not found!");
            }
            //StreamReader reader = new StreamReader(stream);
            return new StreamReader(stream, System.Text.Encoding.UTF8).ReadToEnd();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var postgresConnectionString = Config.GetConnectionString("PostgreSQLConnection");
            services.AddTransient<IDatabaseConfiguration>((sp) =>
            {
                return DonutDbConfig.GetOrAdd("general", DBConfig.GetInstance().GetGeneralDatabase().ToDonutDbConfig());
            });
            services.AddSingleton<ILoggerFactory>(new LoggerFactory());
            services.AddTransient<OrganizationService>();
            services.AddTransient<ILogger>(x => x.GetService<ILoggerFactory>().CreateLogger("Netlyt.ServiceTests"));
            services.AddTransient(x => x.GetService<ILoggerFactory>().CreateLogger<UserManager<User>>());
            services.AddIdentity<User, UserRole>()
                .AddEntityFrameworkStores<ManagementDbContext>()
                .AddDefaultTokenProviders();
            services.AddSingleton<ManagementDbContext>(_context);//<ManagementDbContext>(s => s.UseNpgsql(postgresConnectionString)); 
            services.AddTransient<IFactory<ManagementDbContext>, DynamicContextFactory>(s =>
                new DynamicContextFactory(() =>
                {
                    var opsBuilder = new DbContextOptionsBuilder<ManagementDbContext>().UseNpgsql(postgresConnectionString);
                    return new ManagementDbContext(opsBuilder.Options);
                })
            );
            services.AddTransient<TimestampService>();
            services.AddTransient<ApiService>();//s => new ApiService(_context, null));
            services.AddTransient<UserService>();
            var fakeHttpAccessor = new Mock<IHttpContextAccessor>();
            services.AddTransient<IHttpContextAccessor>((s) => fakeHttpAccessor.Object);
            //s => new UserService(s.GetService<UserManager<User>>(), s.GetService<ApiService>(), null, null,
            //    s.GetService<OrganizationService>(), s.GetService<ModelService>(), _context));
            services.AddTransient<ModelService>();//s => new ModelService(_context, s.GetService<OrionContext>(), null, new TimestampService(_context)));
            services.AddTransient<IntegrationService>();//s => new IntegrationService(_context, new ApiService(_context, null), s.GetService<UserService>(), new TimestampService(_context)));
            services.AddSingleton<IRedisCacher>(DBConfig.GetInstance().GetCacheContext());
            services.AddTransient<IEmailSender, AuthMessageSender>((sp) =>
            {
                return new AuthMessageSender(Config);
            });
            OrionContext = services.RegisterOrionContext(Config.GetSection("behaviour"), (x) => { });
            services.AddSingleton<DonutOrionHandler>();
            services.AddTransient<CompilerService>();
        }

        public ManagementDbContext CreateContext()
        {
            return new ManagementDbContext(DbOptionsBuilder.Options);
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