﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using nvoid.db.Caching;
using nvoid.db.DB.Configuration;
using nvoid.Integration;
using Netlyt.Service;
using Netlyt.Service.Data;
using Netlyt.Service.Integration;
using Netlyt.Service.Lex.Data;
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
            _userService = fixture.GetService<UserService>();
            _appAuth = _apiService.GetApi("d4af4a7e3b1346e5a406123782799da1");
            if (_appAuth == null) _appAuth = _apiService.Create("d4af4a7e3b1346e5a406123782799da1");
            _db = fixture.GetService<ManagementDbContext>();
            _user = _userService.GetByApiKey(_appAuth);
            if (_user == null)
            {
                _user = new User() { FirstName = "tester1231", Email = "mail@lol.co" };
                _userService.CreateUser(_user, "Password-IsStrong!", _appAuth); 
            }
            _cacher = fixture.GetService<RedisCacher>();
            _dbConfig = DBConfig.GetGeneralDatabase();
            _serviceProvider = fixture.GetService<IServiceProvider>();
            _modelService = fixture.GetService<ModelService>();
        }
        [Fact]
        public async Task IntegrationTest()
        {
            string dataSource = "Romanian";
            var integration = _userService.GetUserIntegration(_user, dataSource);
            var relations = new List<FeatureGenerationRelation>();
//            for (var i = 0; i < 1; i++)
//            {
//                var nr = new FeatureGenerationRelation("Events.uuid", "Demography.uuid");
//                relations.Add(nr);
//            }
            string modelName = "Rommol1";
            string modelCallback = "http://localhost:9999";
            string targetAttribute = "pm10"; //"Events.is_paying";
            //item.Relations?.Select(x => new FeatureGenerationRelation(x[0], x[1]));
            //This really needs a builder..
            var newModel = await _modelService.CreateModel(_user,
                modelName,
                new List<DataIntegration>(new[] { integration }),
                modelCallback,
                true,
                relations,
                targetAttribute);
            Assert.NotNull(newModel);

            Console.ReadLine();//We hang on here, waiting for the features to be generated ..
        }

        [Fact]
        public async Task TestGeneratedFeatures()
        {
            long modelId = 117;
            var model = _db.Models.FirstOrDefault(x => x.Id == modelId);
            string features = @"pm10
SUM(Romanian.humidity)
SUM(Romanian.latitude)
SUM(Romanian.longitude)
SUM(Romanian.pm25)
SUM(Romanian.pressure)
SUM(Romanian.rssi)
SUM(Romanian.temperature)
STD(Romanian.humidity)
STD(Romanian.latitude)
STD(Romanian.longitude)
STD(Romanian.pm25)
STD(Romanian.pressure)
STD(Romanian.rssi)
STD(Romanian.temperature)
MAX(Romanian.humidity)
MAX(Romanian.latitude)
MAX(Romanian.longitude)
MAX(Romanian.pm25)
MAX(Romanian.pressure)
MAX(Romanian.rssi)
MAX(Romanian.temperature)
SKEW(Romanian.humidity)
SKEW(Romanian.latitude)
SKEW(Romanian.longitude)
SKEW(Romanian.pm25)
SKEW(Romanian.pressure)
SKEW(Romanian.rssi)
SKEW(Romanian.temperature)
MIN(Romanian.humidity)
MIN(Romanian.latitude)
MIN(Romanian.longitude)
MIN(Romanian.pm25)
MIN(Romanian.pressure)
MIN(Romanian.rssi)
MIN(Romanian.temperature)
MEAN(Romanian.humidity)
MEAN(Romanian.latitude)
MEAN(Romanian.longitude)
MEAN(Romanian.pm25)
MEAN(Romanian.pressure)
MEAN(Romanian.rssi)
MEAN(Romanian.temperature)
DAY(first_Romanian_time)
YEAR(first_Romanian_time)
MONTH(first_Romanian_time)
WEEKDAY(first_Romanian_time)
NUM_UNIQUE(Romanian.DAY(timestamp))
NUM_UNIQUE(Romanian.YEAR(timestamp))
NUM_UNIQUE(Romanian.MONTH(timestamp))
NUM_UNIQUE(Romanian.WEEKDAY(timestamp))
MODE(Romanian.DAY(timestamp))
MODE(Romanian.YEAR(timestamp))
MODE(Romanian.MONTH(timestamp))
MODE(Romanian.WEEKDAY(timestamp))
";
            string[] featureBodies = features.Split('\n');
            string donutName = $"{model.ModelName}Donut";
            DonutScript dscript = DonutScript.Factory.CreateWithFeatures(donutName, featureBodies);
            foreach (var integration in model.DataIntegrations)
            {
                dscript.AddIntegrations(integration.Integration.Collection);
            }
            Type donutType, donutContextType, donutFEmitterType;
            var assembly = _compiler.Compile(dscript, model.ModelName, out donutType, out donutContextType, out donutFEmitterType);
            model.DonutScript = new DonutScriptInfo(dscript);
            model.DonutScript.AssemblyPath = assembly.Location;
            model.DonutScript.Model = model;
            _db.SaveChanges();
        }
    }
}

