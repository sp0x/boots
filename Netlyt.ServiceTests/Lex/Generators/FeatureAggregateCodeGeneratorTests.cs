using System;
using System.Collections.Generic;
using System.Text;
using Donut;
using Donut.Caching;
using Donut.Data;
using Donut.Features;
using Donut.Lex.Data;
using Donut.Lex.Generation;
using Donut.Lex.Generators;
using Donut.Models;
using Donut.Orion;
using nvoid.Crypto;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.ServiceTests.Fixtures;
using Xunit;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.ServiceTests.Lex.Generators
{
    [Collection("DonutTests")]
    public class FeatureAggregateCodeGeneratorTests
    {
        DonutScript _dscript;
        private CompilerService _compiler;
        private ApiService _apiService;
        private IntegrationService _integrationService;
        private UserService _userService;
        private ApiAuth _appAuth;
        private ManagementDbContext _db;
        private OrionContext _orion;
        private User _user;
        private RedisCacher _cacher;
        private DatabaseConfiguration _dbConfig;
        private IServiceProvider _serviceProvider;
        private ModelService _modelService;
        private DonutScriptCodeGenerator _codeGen;
        private DonutConfigurationFixture _fixture;
        private AggregateFeatureGeneratingExpressionVisitor _expVisitor;

        public FeatureAggregateCodeGeneratorTests(DonutConfigurationFixture fixture)
        {
            _compiler = fixture.GetService<CompilerService>();
            _apiService = fixture.GetService<ApiService>();
            _integrationService = fixture.GetService<IntegrationService>();
            _userService = fixture.GetService<UserService>();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            if (_appAuth == null) _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1");
            _db = fixture.GetService<ManagementDbContext>();
            _orion = fixture.GetService<OrionContext>();
            _user = _userService.GetByApiKey(_appAuth);
            if (_user == null)
            {
                _user = new User() { FirstName = "tester1231", Email = "mail@lol.co" };
                _userService.CreateUser(_user, "Password-IsStrong!", _appAuth);
            }
            _cacher = fixture.GetService<RedisCacher>();
            _dbConfig = DBConfig.GetInstance().GetGeneralDatabase();
            _serviceProvider = fixture.GetService<IServiceProvider>();
            _modelService = fixture.GetService<ModelService>();
            _fixture = fixture;
            var features = @"MIN(Romanian.rssi)
DAY(first_Romanian_time)
YEAR(first_Romanian_time)
MONTH(first_Romanian_time)
WEEKDAY(first_Romanian_time)";
            var model = GetModel();
            string[] featureBodies = features.Split('\n');
            string donutName = $"{model.ModelName}Donut";
            var ign = model.GetRootIntegration();
            var targets = new ModelTargets().AddTarget(ign.GetField("pm10"));
            _dscript = DonutScript.Factory.CreateWithFeatures(donutName, targets, ign, featureBodies);
            _codeGen = _dscript.GetCodeGenerator() as DonutScriptCodeGenerator;
            _expVisitor = new AggregateFeatureGeneratingExpressionVisitor(_dscript);
        }
        private Model GetModel()
        {
            var ignName = "Romanian";//Must match the features
            var model = new Model()
            {
                ModelName = "Namex"
            };//_db.Models.Include(x=>x.DataIntegrations).FirstOrDefault(x => x.Id == modelId);
            var rootIntegration = new DataIntegration()
            {
                APIKey = _appAuth,
                APIKeyId = _appAuth.Id,
                Name = ignName,
                DataTimestampColumn = "timestamp",
                FeaturesCollection = $"{ignName}_features",
            };
            rootIntegration.AddField<string>("humidity");
            rootIntegration.AddField<string>("latitude");
            rootIntegration.AddField<string>("longitude");
            rootIntegration.AddField<string>("pm25");
            rootIntegration.AddField<string>("pressure");
            rootIntegration.AddField<string>("rssi");
            rootIntegration.AddField<string>("temperature");
            rootIntegration.AddField<string>("humidity");
            rootIntegration.AddField<string>("latitude");
            rootIntegration.AddField<string>("longitude");
            rootIntegration.AddField<DateTime>("timestamp");
            var modelIntegration = new ModelIntegration() { Model = model, Integration = rootIntegration };
            model.DataIntegrations.Add(modelIntegration);
            return model;
        }

        [Fact]
        public void GetScriptContentTest()
        {
            var aggregates = new AggregateFeatureCodeGenerator(_dscript, _expVisitor);
            aggregates.AddAll(_dscript.Features);
            var aggregatePipeline = aggregates.GetScriptContent();
            var codeHash = HashAlgos.Adler32(aggregatePipeline);
            Assert.Equal((uint)2334630262, codeHash);
        }


    }
}
