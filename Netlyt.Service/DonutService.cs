using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Donut;
using Donut.Data;
using Donut.FeatureGeneration;
using Donut.Lex.Data;
using Donut.Models;
using Donut.Orion;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.EntityFrameworkCore;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Service.Repisitories;

namespace Netlyt.Service
{

    public class DonutService : IDonutService
    {
        private IOrionContext _orion;
        private IDbContextScopeFactory _dbContextFactory;
        private IModelRepository _modelRepository;

        public DonutService(IOrionContext orion,
            IDbContextScopeFactory dbContextFactory,
            IModelRepository modelRepository)
        {
            _orion = orion;
            _dbContextFactory = dbContextFactory;
            _modelRepository = modelRepository;
        }

        public string GetInferenceUrl()
        {
            return "http://inference.netlyt.com";
        }

        public string GetSnippet(User user, TrainingTask trainingTask, string language)
        {
            var inferenceUrl = GetInferenceUrl();
            inferenceUrl += "/" + HttpUtility.UrlEncode(trainingTask.Id.ToString());
            ApiAuth apikey = user.ApiKeys.FirstOrDefault()?.Api;
            var rootIgn = trainingTask.Model.GetRootIntegration();
            var idKey = !string.IsNullOrEmpty(rootIgn.DataIndexColumn) ? rootIgn.DataIndexColumn : null;
            string output = "";
            if (language == "python")
            {
                var dataPart = "";
                if (idKey == null) dataPart = "\""+ idKey  + "\": <idValue> OR ..data row..";
                if (idKey == null) dataPart = "..data row..";
                output = $@"
import requests
data = " + "{ " + dataPart + " } #or array of data" + $@"\n
r = requests.post('{inferenceUrl}', data=data, headers=" + "{key: \"" + apikey.AppId + "\", secret: \"" + apikey.AppSecret + "\" }" + @")\n
predictions = r.json()\n
                ";
            }
            else if (language == "js")
            {
                var headers = "{key: \"" + apikey.AppId + "\", secret: \"" + apikey.AppSecret + "\" }\n";
                var dataPart = "";
                if (idKey == null) dataPart = "\"" + idKey + "\": <idValue> OR ..data row..";
                if (idKey == null) dataPart = "..data row or array of data rows..";
                output = "fetch(\"" + inferenceUrl +"\", {\n" +
                    "method: \"POST\",\n" +
                    "headers: " + headers +
                    "body: " + dataPart + "\n" +
                    "\n})\n" +
                    ".json()\n" +
                    ".then(predictions=>{\n" +
                    "console.log(predictions)" +
                    "\n})";
            }
            else if (language == "cs")
            {
                var dataPart = "";
                if (idKey == null) dataPart = "values[\"" + idKey + "\"] = <idValue> //OR ..data row..";
                if (idKey == null) dataPart = "//values = ..data row..";
                output = @"
using System.Net.Http;
float Predict(){
  HttpClient client = new HttpClient();
  client.DefaultRequestHeaders.Add(" + "\"key\", \"" + apikey.AppId + "\"" + @")\n
  client.DefaultRequestHeaders.Add(" + "\"secret\", \"" + apikey.AppSecret + "\"" + @")\n
  var values = new Dictionary<string, string>();
  " + dataPart + @"\n
  var content = new FormUrlEncodedContent(values);
  " + "var response = await client.PostAsync(\"" + inferenceUrl + "\", content);\n" + @"
  var predictions = JArray.parse(response.Content.ReadAsStringAsync().Result);
  return predictions[0];
}
";
            }
            return output;
        }

        public Dictionary<string, string> GetSnippets(User user, TrainingTask trainingTask)
        {
            var output = new Dictionary<string, string>();
            output["js"] = GetSnippet(user, trainingTask, "js");
            output["cs"] = GetSnippet(user, trainingTask, "cs");
            output["python"] = GetSnippet(user, trainingTask, "python");
            return output;
        }

        public async Task<Tuple<string, DonutScriptInfo>> GeneratePythonModule(long id, User user)
        {
            using (var contextSrc = _dbContextFactory.Create())
            {
                var item = _modelRepository.GetById(id)
                    .Include(x=>x.DonutScript)
                    .Include(x=>x.User)
                    .FirstOrDefault();
                if (item == null) throw new NotFound();
                if (item.User.Id != user.Id)
                {
                    throw new InvalidOperationException("Only owners of models can create python modules for them.");
                }
                var donut = (DonutScriptInfo)item.DonutScript;
                if (donut == null) throw new NotFound();
                var pythonCode = await ToPythonModule(donut);
                return new Tuple<string, DonutScriptInfo>(pythonCode, donut);
            }
        }

        public async Task<IHarvesterResult> RunExtraction(DonutScript script, DataIntegration integration, IServiceProvider serviceProvider)
        {
            var dbConfig = serviceProvider.GetService(typeof(IDatabaseConfiguration)) as IDatabaseConfiguration;
            var harvester = new Harvester<IntegratedDocument>(10);
            IInputSource source = integration.GetCollectionAsSource();

            //Create a donut and a donutRunner
            var donutMachine = DonutGeneratorFactory.Create<IntegratedDocument>(script, integration, serviceProvider);
            harvester.AddIntegration(integration, source);//source, appAuth, "SomeIntegrationName3");
            IDonutfile donut = donutMachine.Generate();
            donut.SetupCacheInterval(source.Size);
            donut.ReplayInputOnFeatures = false;
            //donut.SkipFeatureExtraction = true;
            IDonutRunner<IntegratedDocument> donutRunner = DonutRunnerFactory.CreateByType<IntegratedDocument, IntegratedDocument>(
                donutMachine.DonutType,
                donutMachine.DonutContextType,
                harvester, dbConfig, integration.FeaturesCollection);
            var featureGenerator = FeatureGeneratorFactory<IntegratedDocument>.Create(donut, donutMachine.GetEmitterType());
            var result = await donutRunner.Run(donut, featureGenerator);
            return result;
        }

        public async Task<string> ToPythonModule(DonutScriptInfo donut)
        {
            var output = "";
            IDonutScript ds = donut.GetScript();
            var query = OrionQuery.Factory.CreateScriptGenerationQuery(donut.Model, ds);
            query["params"]["name"] = $"{ds.Type.Name}";
            query["params"]["client"] = donut.Model.User.UserName;
            query["params"]["grouping"] = donut.Model.Grouping;
            var res = await _orion.Query(query);
            output = res["code"].ToString();
            return output;
        }
    }
}
