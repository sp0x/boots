using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Donut;
using Donut.Data;
using Donut.IntegrationSource;
using Donut.Models;
using Donut.Orion;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Donut;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.ServiceTests.Fixtures
{
    public class DonutConfigurationFixture : IDisposable
    {
        protected ManagementDbContext _context;
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
                .UseLazyLoadingProxies()
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
            services.AddTransient<IIntegrationService, IntegrationService>();//s => new IntegrationService(_context, new ApiService(_context, null), s.GetService<UserService>(), new TimestampService(_context)));
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

        public async Task<Model> GetModel(ApiAuth apiAuth, string modelName = "Romanian", string integrationName = "Namex")
        {
            var rootIntegration = new DataIntegration(integrationName, true)
            {
                APIKey = apiAuth,
                APIKeyId = apiAuth.Id,
                Name = integrationName,
                DataTimestampColumn = "timestamp",
            };
            rootIntegration.AddField<double>("humidity");
            rootIntegration.AddField<double>("latitude");
            rootIntegration.AddField<double>("longitude");
            rootIntegration.AddField<double>("pm10");
            rootIntegration.AddField<double>("pm25");
            rootIntegration.AddField<double>("pressure");
            rootIntegration.AddField<double>("rssi");
            rootIntegration.AddField<double>("temperature");
            rootIntegration.AddField<DateTime>("timestamp");
            var firstIgn = await _context.Integrations.FirstOrDefaultAsync(x => x.Name == rootIntegration.Name);
            if (firstIgn != null)
            {
                _context.Integrations.Remove(firstIgn);
            }

            var model = await GetModel(apiAuth, modelName, rootIntegration);
            _context.Integrations.Add(firstIgn);
            return model;
        }
        public async Task<Model> GetModel(ApiAuth apiAuth, string modelName, DataIntegration ign)
        {
            var model = new Model()
            {
                ModelName = modelName
            };//_db.Models.Include(x=>x.DataIntegrations).FirstOrDefault(x => x.Id == modelId);
            var firstModel = await _context.Models.FirstOrDefaultAsync(x => x.ModelName == modelName);
            if (firstModel != null)
            {
                _context.Models.Remove(firstModel);
            }
            var modelIntegration = new ModelIntegration() { Model = model, Integration = ign };
            model.DataIntegrations.Add(modelIntegration);
            model.User = new User() { UserName = "Testuser" };
            model.Targets = new ModelTargets().AddTarget(ign.GetField("pm10"));
            return model;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="name"></param>
        /// <param name="appAuth"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public DataIntegration GetIntegrationByFile(string sourceFile, string name, ApiAuth appAuth,User user, out IInputSource inputSource)
        {
            inputSource = new FileSource(sourceFile);
            var ignService = GetService<IIntegrationService>();
            inputSource.SetFormatter(ignService.ResolveFormatter<ExpandoObject>(MimeResolver.Resolve(sourceFile)));

            bool isNewIgn;
            var ign = ignService.ResolveIntegration(appAuth, user, name, out isNewIgn, inputSource);
            var keys = ign.AggregateKeys;
            return ign;
        }
    }
}