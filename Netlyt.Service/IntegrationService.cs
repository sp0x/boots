using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Donut;
using Donut.Data;
using Donut.Integration;
using Donut.IntegrationSource;
using Microsoft.EntityFrameworkCore;
using nvoid.db.DB.MongoDB;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Data.Format;
using Netlyt.Service.Data;
using Netlyt.Service.Exceptions;

namespace Netlyt.Service
{
    public class IntegrationService : IIntegrationService
    {
        //private IFactory<ManagementDbContext> _factory;
        private ManagementDbContext _context;

        private ApiService _apiService;
        private UserService _userService;
        private TimestampService _timestampService;
        private IDatabaseConfiguration _dbConfig;

        public IntegrationService(ManagementDbContext context,
            ApiService apiService,
            UserService userService,
            TimestampService tsService,
            IDatabaseConfiguration dbConfig)
        {
            _context = context;
            _apiService = apiService;
            _userService = userService;
            _timestampService = tsService;
            _dbConfig = dbConfig;
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
            if (!string.IsNullOrEmpty(importTaskIntegration.Collection))
            {
                var collection = new MongoList(_dbConfig.Name, importTaskIntegration.Collection, _dbConfig.GetUrl());
                collection.Trash();
                collection = null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IQueryable<DataIntegration> GetById(long id)
        {
            return _context.Integrations.Where(x => x.Id == id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextApiAuth"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public IIntegration GetByName(IApiAuth contextApiAuth, string name)
        {
            var integration = _context.Integrations
                .Include(x => x.APIKey)
                .FirstOrDefault(x => x.APIKey.Id == contextApiAuth.Id && x.Name == name);
            return integration;
        }

        /// <summary>
        /// Creates a new integration or appends the data to an existing one.
        /// Find an existing integration by checking the name and all of the field definitions.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task<DataImportResult> CreateOrAppendToIntegration(string filePath, ApiAuth apiKey, User user, string name = null)
        {
            //Must be relative
            if (filePath[0] != '/' && filePath[1] != ':')
            {
                var relativePath = Environment.CurrentDirectory;
                filePath = Path.Combine(relativePath, filePath);
            }
            var mime = MimeResolver.Resolve(filePath);
            using (var fsStream = File.Open(filePath, FileMode.Open))
            {
                return await CreateOrAppendToIntegration(fsStream, apiKey, user, mime, name);
            }
        }

        /// <summary>
        /// Creates a new integration from the stream, or adds the stream to an existing integration.
        /// </summary>
        /// <param name="inputData">The stream containing integration data</param>
        /// <returns></returns>
        public async Task<DataImportResult> CreateOrAppendToIntegration(Stream inputData, string mime = null, string name = null)
        {
            var options = new DataImportTaskOptions();
            var apiKey = await _userService.GetCurrentApi();
            var crUser = await _userService.GetCurrentUser();
            return await CreateOrAppendToIntegration(inputData, apiKey, crUser, mime, name);
        }

        /// <summary>
        /// Appends the data to an existing integration
        /// </summary>
        /// <param name="ign"></param>
        /// <param name="inputData"></param>
        /// <param name="apiKey"></param>
        /// <param name="mime"></param>
        /// <returns></returns>
        public async Task<DataImportResult> AppendToIntegration(DataIntegration ign, string filePath, ApiAuth apiKey)
        {
            if (apiKey == null) throw new Exception("Could not resolve an api key for the current user.");
            //Must be relative
            if (filePath[0] != '/' && filePath[1] != ':')
            {
                var relativePath = Environment.CurrentDirectory;
                filePath = System.IO.Path.Combine(relativePath, filePath);
            }
            var mime = MimeResolver.Resolve(filePath);
            var formatter = ResolveFormatter<ExpandoObject>(mime);
            if (formatter == null)
            {
                throw new Exception("Could not resolve formatter for the given content type!");
            }
            using (var fsStream = File.Open(filePath, FileMode.Open))
            {
                return await AppendToIntegration(ign, fsStream, apiKey, mime);
            }
        }

        /// <summary>
        /// Appends the data to an existing integration
        /// </summary>
        /// <param name="ign"></param>
        /// <param name="inputData"></param>
        /// <param name="apiKey"></param>
        /// <param name="mime"></param>
        /// <returns></returns>
        public async Task<DataImportResult> AppendToIntegration(DataIntegration ign, Stream inputData, ApiAuth apiKey, string mime = null)
        {
            if (apiKey == null) throw new Exception("Could not resolve an api key for the current user.");
            var formatter = ResolveFormatter<ExpandoObject>(mime);
            if (formatter == null)
            {
                throw new Exception("Could not resolve formatter for the given content type!");
            }
            var source = InMemorySource.Create(inputData, formatter);
            var options = new DataImportTaskOptions();
            options.Source = source;
            options.ApiKey = apiKey;
            options.Integration = ign;
            var importTask = new DataImportTask<ExpandoObject>(options);
            var result = await importTask.Import();
            return result;
        }

        /// <summary>
        /// Appends the data to an existing integration
        /// </summary>
        /// <param name="ign"></param>
        /// <param name="source"></param>
        /// <param name="apiKey"></param>
        /// <param name="mime"></param>
        /// <returns></returns>
        public async Task<DataImportResult> AppendToIntegration(DataIntegration ign, InputSource source, ApiAuth apiKey, string mime = null)
        {
            if (apiKey == null) throw new Exception("Could not resolve an api key for the current user.");
            var formatter = ResolveFormatter<ExpandoObject>(mime);
            if (formatter == null)
            {
                throw new Exception("Could not resolve formatter for the given content type!");
            }
            var options = new DataImportTaskOptions();
            options.Source = source;
            options.ApiKey = apiKey;
            options.Integration = ign;
            var importTask = new DataImportTask<ExpandoObject>(options);
            var result = await importTask.Import();
            _context.SaveChanges();
            return result;
        }

        /// <summary>
        /// Creates a new integration from the stream, or adds the stream to an existing integration.
        /// </summary>
        /// <param name="inputData">The stream containing integration data</param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<DataImportResult> CreateOrAppendToIntegration(Stream inputData, ApiAuth apiKey, User owner, string mime = null, string name = null)
        {
            var integrationInfo = CreateIntegrationImportTask(inputData, apiKey, owner, mime, name, out var isNewIntegration, out var importTask);
            var result = await importTask.Import();
            if (isNewIntegration)
            {
                var collection = MongoHelper.GetCollection(integrationInfo.Collection);
                await importTask.Encode(collection);
                _context.Integrations.Add(integrationInfo);
            }
            _context.SaveChanges(); //Save any changes done to the integration.
            return result;
        }

        /// <summary>
        /// Creates a new integration or appends the data to an existing one.
        /// Find an existing integration by checking the name and all of the field definitions.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public DataImportTask<ExpandoObject> CreateIntegrationImportTask(string filePath, ApiAuth apiKey, User user, string name = null)
        {
            //Must be relative
            if (filePath[0] != '/' && filePath[1] != ':')
            {
                var relativePath = Environment.CurrentDirectory;
                filePath = Path.Combine(relativePath, filePath);
            }
            var mime = MimeResolver.Resolve(filePath);
            var fsStream = System.IO.File.Open(filePath, FileMode.Open);
            DataImportTask<ExpandoObject> newTask;
            bool isNewIntegration;
            var newIntegration = CreateIntegrationImportTask(fsStream, apiKey, user, mime, name, out isNewIntegration, out newTask);
            return newTask;
        }

        /// <summary>
        /// Creates a new task to import data.
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="apiKey"></param>
        /// <param name="owner"></param>
        /// <param name="mime"></param>
        /// <param name="name"></param>
        /// <param name="isNewIntegration"></param>
        /// <param name="importTask"></param>
        /// <returns></returns>
        public DataIntegration CreateIntegrationImportTask(Stream inputData,
            ApiAuth apiKey, 
            User owner,
            string mime,
            string name,
            out bool isNewIntegration, 
            out DataImportTask<ExpandoObject> importTask)
        {
            var options = new DataImportTaskOptions();
            if (apiKey == null) throw new Exception("Could not resolve an api key for the current user.");
            var formatter = ResolveFormatter<ExpandoObject>(mime);
            if (formatter == null)
            {
                throw new Exception("Could not resolve formatter for the given content type!");
            }

            var source = InMemorySource.Create(inputData, formatter);
            var integrationInfo = ResolveIntegration(apiKey, owner, name, out isNewIntegration, source);

            options.Source = source;
            options.ApiKey = apiKey;
            options.IntegrationName = integrationInfo.Name;
            options.Integration = integrationInfo;
            importTask = new DataImportTask<ExpandoObject>(options);
            return integrationInfo;
        }

        /// <summary>
        /// Resolves an integration from a source
        /// </summary>
        /// <param name="apiKey"></param>
        /// <param name="owner"></param>
        /// <param name="name"></param>
        /// <param name="isNewIntegration"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public DataIntegration ResolveIntegration(ApiAuth apiKey, User owner, string name, out bool isNewIntegration,
            IInputSource source)
        {
            DataIntegration integrationInfo = source.ResolveIntegrationDefinition() as DataIntegration;
            DataIntegration exitingIntegration = null;
            isNewIntegration = false;
            if (IntegrationExists(integrationInfo, apiKey.AppId, out exitingIntegration))
            {
                integrationInfo = exitingIntegration;
            }
            else
            {
                isNewIntegration = true;
                integrationInfo.APIKey = apiKey;
                integrationInfo.Owner = owner;
                integrationInfo.Name = name;
                integrationInfo.Collection = Guid.NewGuid().ToString();
                integrationInfo.FeaturesCollection = $"{integrationInfo.Collection}_features";
                var timestampCol = _timestampService.Discover(integrationInfo);
                if (!string.IsNullOrEmpty(timestampCol) && integrationInfo != null)
                {
                    integrationInfo.DataTimestampColumn = timestampCol;
                }
            }
            if (integrationInfo == null)
            {
                throw new Exception("No integration found!");
            }
            return integrationInfo;
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
            var newIntegration = new DataIntegration();
            newIntegration.Name = integrationName;
            newIntegration.DataEncoding = System.Text.Encoding.UTF8.CodePage;
            newIntegration.DataFormatType = formatType;
            newIntegration.Owner = crUser;
            newIntegration.APIKey = apiKey;

            var existingIntegration = _context.Integrations
                .FirstOrDefault(x => x.Name.ToLower() == integrationName.ToLower()
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
        public IInputFormatter<T> ResolveFormatter<T>(string mimeType)
            where T : class
        {
            if (!string.IsNullOrEmpty(mimeType))
            {
                if (mimeType.Contains("ms-excel")) return new CsvFormatter<T>() { Delimiter = ',' };
                if (mimeType == "text/csv") return new CsvFormatter<T>() { Delimiter = ',' };
                if (mimeType == "application/json") return new JsonFormatter<T>() { };
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
