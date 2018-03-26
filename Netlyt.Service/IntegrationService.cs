using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using nvoid.db.DB.Configuration;
using nvoid.db.DB.MongoDB;
using nvoid.Integration;
using Netlyt.Data;
using Netlyt.Service.Data;
using Netlyt.Service.Exceptions;
using Netlyt.Service.Format;
using Netlyt.Service.Integration;
using Netlyt.Service.Integration.Import;
using Netlyt.Service.IntegrationSource;

namespace Netlyt.Service
{
    public class IntegrationService
    {
        //private IFactory<ManagementDbContext> _factory;
        private ManagementDbContext _context;

        private ApiService _apiService;
        private UserService _userService;
        private TimestampService _timestampService;

        public IntegrationService(ManagementDbContext context,
            ApiService apiService,
            UserService userService,
            TimestampService tsService)
        {
            _context = context;
            _apiService = apiService;
            _userService = userService;
            _timestampService = tsService;
        }

        public void SaveOrFetchExisting(ref DataIntegration type)
        {
            DataIntegration exitingIntegration;
            var typeApiKey = type.APIKey;
            var appId = typeApiKey.AppId;
            if (!IntegrationExists(type, appId, out exitingIntegration))
            {
                _context.Integrations.Add(type);
                _context.SaveChanges();
            }
            else
            {
                type = exitingIntegration;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="appId"></param>
        /// <param name="existingDefinition"></param>
        /// <returns></returns>
        public bool IntegrationExists(IIntegration type, string appId, out DataIntegration existingDefinition)
        {
            var localFields = type.Fields; 
            existingDefinition = (from x in _context.Integrations
                                  where x.APIKey.AppId == appId
                                        && x.Name == type.Name
                                        && x.Fields.All(f => localFields.Any(lf => lf.Name == f.Name))
                                  select x).FirstOrDefault();
            return existingDefinition != null;
        }

        //
        //        public override void PrepareForSaving()
        //        {
        //            base.PrepareForSaving();
        //            if (string.IsNullOrEmpty(APIKey))
        //                throw new InvalidOperationException("Only user owned type definitions can be saved!");
        //        }
         

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="apiId"></param>
        /// <param name="existingDefinition"></param>
        /// <returns></returns>
        public bool IntegrationExists(IIntegration type, long apiId, out DataIntegration existingDefinition)
        {
            existingDefinition = _context.Integrations.FirstOrDefault(x => x.APIKey.Id == apiId && (x.Fields == type.Fields || x.Name == type.Name));
            return existingDefinition != null;
        }
        /// <summary>
        /// Removes an integration and associated collections.
        /// </summary>
        /// <param name="importTaskIntegration"></param>
        public void Remove(DataIntegration importTaskIntegration)
        {
            _context.Integrations.Remove(importTaskIntegration);
            _context.SaveChanges();
            var databaseConfiguration = DBConfig.GetGeneralDatabase();
            if (!string.IsNullOrEmpty(importTaskIntegration.Collection))
            {
                var collection = new MongoList(databaseConfiguration, importTaskIntegration.Collection);
                collection.Trash();
                collection = null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataIntegration GetById(long id)
        {
            return _context.Integrations.Find(id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextApiAuth"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataIntegration GetByName(ApiAuth contextApiAuth, string name)
        {
            var integration = _context.Integrations
                .Include(x=>x.APIKey)
                .FirstOrDefault(x => x.APIKey.Id == contextApiAuth.Id && x.Name == name);
            return integration;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task<DataImportResult> CreateOrFillIntegration(string filePath, ApiAuth apiKey, User user,string name=null)
        {
            //Must be relative
            if (filePath[0] != '/' && filePath[1] != ':')
            {
                var relativePath = Environment.CurrentDirectory;
                filePath = System.IO.Path.Combine(relativePath, filePath);
            }
            var mime = MimeResolver.Resolve(filePath); 
            using (var fsStream = System.IO.File.Open(filePath, FileMode.Open))
            {
                return await CreateOrFillIntegration(fsStream, apiKey, user, mime, name);
            }
        }
        /// <summary>
        /// Creates a new integration from the stream, or adds the stream to an existing integration.
        /// </summary>
        /// <param name="inputData">The stream containing integration data</param>
        /// <returns></returns>
        public async Task<DataImportResult> CreateOrFillIntegration(Stream inputData, string mime = null, string name = null)
        {
            var options = new DataImportTaskOptions();
            var apiKey = await _userService.GetCurrentApi();
            var crUser = await _userService.GetCurrentUser();
            return await CreateOrFillIntegration(inputData, apiKey, crUser, mime, name);
        }

        /// <summary>
        /// Creates a new integration from the stream, or adds the stream to an existing integration.
        /// </summary>
        /// <param name="inputData">The stream containing integration data</param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<DataImportResult> CreateOrFillIntegration(Stream inputData, ApiAuth apiKey, User owner, string mime = null, string name = null)
        {
            var options = new DataImportTaskOptions();
            if (apiKey == null) throw new Exception("Could not resolve an api key for the current user.");
            var formatter = ResolveFormatter<ExpandoObject>(mime);
            if (formatter == null)
            {
                throw new Exception("Could not resolve formatter for the given content type!");
            }
            var source = InMemorySource.Create(inputData, formatter);
            DataIntegration integrationInfo = source.ResolveIntegrationDefinition() as DataIntegration;
            var timestampCol = _timestampService.Discover(integrationInfo);
            if (!string.IsNullOrEmpty(timestampCol) && integrationInfo != null)
            {
                integrationInfo.DataTimestampColumn = timestampCol;
            }

            if (integrationInfo == null)
            {
                throw new Exception("No integration found!");
            }
            integrationInfo.APIKey = apiKey;
            integrationInfo.Owner = owner;
            integrationInfo.Name = name;

            options.Source = source;
            options.ApiKey = apiKey;
            options.IntegrationName = integrationInfo.Name;

            var importTask = new DataImportTask<ExpandoObject>(_apiService, this, options);
            var result = await importTask.Import();
            return result;
        }

        /// <summary>
        /// Creates a new integration with the given name and data format.
        /// </summary>
        /// <param name="integrationName"></param>
        /// <param name="formatType"></param>
        /// <returns></returns>
        public async Task<DataIntegration> Create(string integrationName, string formatType)
        {
            var apiKey = await _userService.GetCurrentApi();
            if (apiKey == null) throw new Exception("Could not resolve an api key for the current user.");
            var crUser = await _userService.GetCurrentUser();
            var newIntegration = new Integration.DataIntegration();
            newIntegration.Name = integrationName;
            newIntegration.DataEncoding = System.Text.Encoding.UTF8.CodePage;
            newIntegration.DataFormatType = formatType;
            newIntegration.Owner = crUser;
            newIntegration.APIKey = apiKey;

            var existingIntegration = _context.Integrations
                .FirstOrDefault(x=>x.Name.ToLower() == integrationName.ToLower()
                && x.Owner.Id == crUser.Id);
            if (existingIntegration != null)
            {
                throw new ObjectAlreadyExists("Integration with the same name already exists!");
            }
            _context.Integrations.Add(newIntegration);
            _context.SaveChanges();
            return await Task.FromResult(newIntegration);
        }

        /// <summary>
        /// Resolves the needed formatter to read the data.
        /// </summary>
        /// <param name="mimeType">The mime type of the data.</param>
        /// <returns></returns>
        private IInputFormatter<T> ResolveFormatter<T>(string mimeType)
            where T : class
        {
            if (!string.IsNullOrEmpty(mimeType))
            {
                if (mimeType.Contains("ms-excel")) return new CsvFormatter<T>() { Delimiter = ',' };
                if (mimeType=="text/csv") return new CsvFormatter<T>() { Delimiter = ',' };
                if (mimeType== "application/json") return new JsonFormatter<T>() {  };
            }
            else
            {
                var output = new JsonFormatter<T>();
                return output;
            }
            return null;
        }

        /// <summary>
        /// Checks the mime if it's allowed for integration.
        /// </summary>
        /// <param name="fileContentType"></param>
        /// <returns></returns>
        public static bool MimeIsAllowed(string fileContentType)
        {
            if (string.IsNullOrEmpty(fileContentType)) return false;
            if (fileContentType.EndsWith("ms-excel")) return true;
            if (fileContentType == "application/json") return true;
            return false;
        }
    }
}
