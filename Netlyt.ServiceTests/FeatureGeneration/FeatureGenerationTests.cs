using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.Integration;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Integration;
using Netlyt.Service.Orion;
using Netlyt.ServiceTests.Fixtures;
using Xunit;

namespace Netlyt.ServiceTests.FeatureGeneration
{
    [Collection("DonutTests")]
    public class FeatureGenerationTests
    {
        private CompilerService _compiler;
        private ApiService _apiService;
        private IntegrationService _integrationService;
        private ApiAuth _appAuth;
        private IServiceProvider _serviceProvider;
        private DatabaseConfiguration _dbConfig;
        private RedisCacher _cacher;
        private ManagementDbContext _db;
        private UserService _userService;
        private ModelService _modelService;
        private User _user;

        public FeatureGenerationTests(DonutConfigurationFixture fixture)
        {
            _compiler = fixture.GetService<CompilerService>();
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            if (_appAuth == null) _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1");
            _db = fixture.GetService<ManagementDbContext>();
            _user = _db.Users.FirstOrDefault(u => u.ApiKeys.Any(x => x.Id == _appAuth.Id));
            if (_user == null)
            {
                _user = new User() { FirstName = "tester1231", Email = "mail@lol.co" };
                _user.ApiKeys.Add(_appAuth);
                _db.Users.Add(_user);
                _db.SaveChanges();
            }
            _cacher = fixture.GetService<RedisCacher>();
            _dbConfig = DBConfig.GetGeneralDatabase();
            _userService = fixture.GetService<UserService>();
            _serviceProvider = fixture.GetService<IServiceProvider>();
            _modelService = fixture.GetService<ModelService>();
        }
        [Fact]
        public async Task IntegrationTest()
        {
            var user = _db.Users.FirstOrDefault(x => x.ApiKeys.Any(y => y.Id == _appAuth.Id));
            string dataSource = "Events";
            var integration = _userService.GetUserIntegration(user, dataSource);
            var relations = new List<FeatureGenerationRelation>();
//            for (var i = 0; i < 1; i++)
//            {
//                var nr = new FeatureGenerationRelation("Events.uuid", "Demography.uuid");
//                relations.Add(nr);
//            }
            string modelName = "EventsDphy";
            string modelCallback = "http://localhost:9999";
            string targetAttribute = "finished"; //"Events.is_paying";
            //item.Relations?.Select(x => new FeatureGenerationRelation(x[0], x[1]));
            //This really needs a builder..
            var newModel = await _modelService.CreateModel(user,
                modelName,
                new List<DataIntegration>(new[] { integration }),
                modelCallback,
                true,
                relations,
                targetAttribute);
            Assert.NotNull(newModel);

            Console.ReadLine();//We hang on here, waiting for the features to be generated ..
        }
    }
}

