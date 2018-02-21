using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Import;
using Netlyt.Service.IntegrationSource;

namespace Netlyt.Service
{
    public class DataIntegrationService
    {
        private ApiService _apiService;
        private IntegrationService _integrationService;
        private UserService _userService;

        public DataIntegrationService(
            ApiService apiService,
            IntegrationService integrationService,
            UserService userService)
        {
            _apiService = apiService;
            _integrationService = integrationService;
            _userService = userService;
        }

        public async Task<DataImportResult> PostEntityData(Stream inputData)
        {
            var options = new DataImportTaskOptions();
            var apiKey = _apiService.GetCurrentApi();
            var crUser = await _userService.GetCurrentUser();
            //TODO: Resolve the formatter..
            var formatter = ResolveFormatter();
            var source = InMemorySource.Create(inputData, formatter);
            DataIntegration integrationInfo = source.ResolveIntegrationDefinition() as DataIntegration;
            if (integrationInfo == null)
            {
                throw new Exception("No integration found!");
            }
            integrationInfo.APIKey = apiKey;
            integrationInfo.Owner = crUser; 

            options.Source = source;
            options.ApiKey = apiKey;
            options.IntegrationName = integrationInfo.Name;
            
            var importTask = new DataImportTask<ExpandoObject>(_apiService, _integrationService, options);
            var result = await importTask.Import();
            return result;
        }

        private IInputFormatter ResolveFormatter()
        {
            var output = new JsonFormatter();
            return output;
        }
    }
}
