using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Donut;
using Donut.Data;
using Donut.Data.Format;
using Donut.Encoding;
using Donut.Integration;
using Donut.IntegrationSource;
using Donut.Models;
using Donut.Orion;
using Donut.Source;
using EntityFramework.DbContextScope;
using EntityFramework.DbContextScope.Interfaces;
using FluentNHibernate.Conventions.Inspections;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Netlyt.Data.ViewModels;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Cloud;
using Netlyt.Interfaces.Data;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Data;
using Netlyt.Service.Exceptions;
using Netlyt.Service.Helpers;
using Netlyt.Service.Repisitories;
using Newtonsoft.Json.Linq;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.Service
{
    public class IntegrationService : IIntegrationService
    {
        private TimestampService _timestampService;
        private IDbContextScopeFactory _contextFactory;
        private IOrionContext _orionContext;
        private IMapper _mapper;
        private IIntegrationRepository _integrationsRepo;
        private IUsersRepository _users;
        private IApiKeyRepository _keysRepository;
        private PermissionService _permissionService;
        private ILoggingService _loggingService;

        public IntegrationService(
            TimestampService tsService,
            IDbContextScopeFactory dbContextFactory,
            IOrionContext orionContext,
            IMapper mapper,
            IIntegrationRepository integrationsRepo,
            IUsersRepository users,
            IApiKeyRepository keys,
            PermissionService permissionService,
            ILoggingService loggingService)
        {
            _loggingService = loggingService;
            _keysRepository = keys;
            _mapper = mapper;
            _timestampService = tsService;
            _contextFactory = dbContextFactory;
            _orionContext = orionContext;
            _integrationsRepo = integrationsRepo;
            _users = users;
            _permissionService = permissionService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataIntegration GetUserIntegration(User user, long id)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            using (var ctxSrc = _contextFactory.Create())
            {
                var context = ctxSrc.DbContexts.Get<ManagementDbContext>();
                var userApiKeys = context.ApiUsers.Where(x => x.UserId == user.Id);
                var userOrgId = context.Users.FirstOrDefault(x => x.Id == user.Id).Organization.Id;
                var integration = context.Integrations.FirstOrDefault(x => x.Permissions.Any(p=>p.ShareWith.Id == userOrgId) 
                                                                           && x.Id == id);
                return integration;
            }
        }
        public async Task<IEnumerable<DataIntegration>> GetIntegrations(User user, int page, int pageSize)
        {
            using (var contextSrc = _contextFactory.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var integrations = context.Integrations.Where(x => x.Owner == user).Skip(page * pageSize).Take(pageSize)
                    .ToList();
                return await Task.FromResult(integrations);
            }
        }

        public async Task<IEnumerable<DataIntegration>> GetIntegrations(User currentUser, string targetUserId, int page, int pageSize)
        {
            using (var contextSrc = _contextFactory.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var currentUserEntity = _users.GetById(currentUser.Id).FirstOrDefault();
                var crOrgId = currentUserEntity.Organization.Id;
                var integrations = context.Integrations.Where(x => x.Owner.Id == targetUserId
                                                                 && x.Permissions.Any(p => p.ShareWith.Id == crOrgId)
                                                                   )
                    .Skip(page * pageSize).Take(pageSize)
                    .ToList();
                //&& x.Permissions.Any(p=>p.Owner.Id==crOrgId)
                return await Task.FromResult(integrations);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataIntegration GetUserIntegration(User user, string name)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            using (var contextSrc = _contextFactory.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var integration = context.Integrations.FirstOrDefault(x => x.APIKey != null
                                                                            && (user.ApiKeys.Any(y => y.ApiId == x.APIKey.Id)
                                                                                || user.ApiKeys.Any(y => y.ApiId == x.PublicKeyId))
                                                                            && x.Name == name);
                return integration;
            }
        }
        public void SaveOrFetchExisting(ref DataIntegration type)
        {
            DataIntegration exitingIntegration;
            var typeApiKey = type.APIKey;
            var appId = typeApiKey.AppId;
            if (!IntegrationExists(type, appId, out exitingIntegration))
            {
                using (var context = _contextFactory.Create())
                {
                    var ambientDbContextLocator = new AmbientDbContextLocator();
                    var dbContext = ambientDbContextLocator.Get<ManagementDbContext>();
                    dbContext.Integrations.Add(type);
                    context.SaveChanges();
                }

            }
            else
            {
                type = exitingIntegration;
            }
        }

        /// <summary>
        /// Gets the aggregate keys for this integration.
        /// If a timestamp column is present, it's used as a key (day of year and hour).
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AggregateKey> GetAggregateKeys(IIntegration integration)
        {
            //if (_aggregateKeys != null) return _aggregateKeys;
            var lsOut = new List<AggregateKey>();
            var tsKey = integration.DataTimestampColumn;
            if (!string.IsNullOrEmpty(tsKey))
            {
                lsOut.Add(new AggregateKey("tsHour", "hour", tsKey));
                lsOut.Add(new AggregateKey("tsDayyr", "dayOfYear", tsKey));
            }
            else
            {
                lsOut.Add(new AggregateKey("_id", null, "$_id"));
            }
            //_aggregateKeys = lsOut;
            return lsOut;
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
            using (var context = _contextFactory.Create())
            {
                var ambientDbContextLocator = new AmbientDbContextLocator();
                var dbContext = ambientDbContextLocator.Get<ManagementDbContext>();
                existingDefinition = (from x in dbContext.Integrations
                    where x.APIKey.AppId == appId
                          && x.Name == type.Name
                          && x.Fields.All(f => localFields.Any(lf => lf.Name == f.Name))
                    select x).FirstOrDefault();
                return existingDefinition != null;
            }
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
            using (var context = _contextFactory.Create())
            {
                var ambientDbContextLocator = new AmbientDbContextLocator();
                var dbContext = ambientDbContextLocator.Get<ManagementDbContext>();
                existingDefinition = dbContext.Integrations.FirstOrDefault(x => x.APIKey.Id == apiId && (x.Fields == type.Fields || x.Name == type.Name));
                return existingDefinition != null;
            }

        }
        /// <summary>
        /// Removes an integration and associated collections.
        /// </summary>
        /// <param name="importTaskIntegration"></param>
        public void Remove(DataIntegration importTaskIntegration)
        {
            using (var context = _contextFactory.Create())
            {
                var ambientDbContextLocator = new AmbientDbContextLocator();
                var dbContext = ambientDbContextLocator.Get<ManagementDbContext>();
                importTaskIntegration.Permissions.Clear();
                importTaskIntegration.AggregateKeys.Clear();
                dbContext.Integrations.Remove(importTaskIntegration);

                dbContext.SaveChanges();
                if (!string.IsNullOrEmpty(importTaskIntegration.Collection))
                {
                    var collection = MongoHelper.GetCollection(importTaskIntegration.Collection);
                    collection.Drop();
                    collection = null;
                }
            }

            
        }

        public void SetTargetTypes(DataIntegration ign, JToken description)
        {
            var summary = description["file_summary"];
            var descs = summary["desc"];
            var fields = ign.Fields.ToList();
            foreach (JProperty descPair in descs)
            {
                var fname = descPair.Name;;
                var fld = fields.FirstOrDefault(x => x.Name == fname);
                if (fld == null) continue;
                var target_type = descPair.Value["target_type"];
                fld.TargetType = target_type.ToString();
            }
            ign.Fields = fields;
        }

        public async Task<BsonDocument> GetTaskDataSample(TrainingTask trainingTask)
        {
            var rootIntegration = trainingTask.Model.GetRootIntegration();
            var dataCollection = MongoHelper.GetCollection(trainingTask.Model.GetFeaturesCollection());
            var collectionSize = dataCollection.Count(new BsonDocument());
            var skipPerc = 0.8;
            int startingPoint = (int)Math.Floor(collectionSize * skipPerc);
            var ops = new FindOptions();
            var examplesList = await dataCollection.Find<BsonDocument>(new BsonDocument(), ops)
                .Skip(startingPoint)
                .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
                .Limit(1)
                .ToListAsync();
            var example = examplesList.FirstOrDefault();
            using (var context = _contextFactory.Create())
            {
                var ambientDbContextLocator = new AmbientDbContextLocator();
                var extrasRepo = new FieldExtraRepository(ambientDbContextLocator);
                var encoder = FieldEncoder.Factory.Create(rootIntegration);
                encoder.DecodeFields(example, extrasRepo);
            }
            //Decode
            return example;
        }

        public void OnRemoteIntegrationCreated(ICloudNodeNotification notification, JToken eBody)
        {
            var token = eBody["token"].ToString();
            var newIntegration = new DataIntegration();
            newIntegration.CreatedOnNodeToken = token;
            newIntegration.Name = eBody["name"].ToString();
            newIntegration.FeatureScript = eBody["FeatureScript"]?.ToString();
            newIntegration.DataFormatType = eBody["DataFormatType"]?.ToString();
            newIntegration.CreatedOn = eBody["on"].ToObject<DateTime>();
            newIntegration.IsRemote = true;
            newIntegration.RemoteId = long.Parse(eBody["id"].ToString());
            foreach (var field in eBody["fields"])
            {
                FieldDefinition newField = DeserializeField(field);
                newIntegration.Fields.Add(newField);
            }

            newIntegration.DataIndexColumn = eBody["ix_column"]?.ToString();
            newIntegration.DataTimestampColumn = eBody["ts_column"]?.ToString();
            using (var contextSrc = _contextFactory.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                newIntegration.Owner = _users.GetById(eBody["user_id"].ToString()).FirstOrDefault();
                if (newIntegration.Owner != null)
                {
                    var apiKey = newIntegration.Owner.ApiKeys.FirstOrDefault();
                    newIntegration.APIKeyId = apiKey.ApiId;
                    newIntegration.APIKey = apiKey.Api;
                }
                context.Integrations.Add(newIntegration);
                context.SaveChanges();
            }
        }

        private FieldDefinition DeserializeField(JToken jsField)
        {
            var field = jsField.ToObject<FieldDefinition>();
            return field;
        }

        public async Task<DataIntegration> ResolveDescription(User user, DataIntegration integration)
        {
            using (var contextSrc = _contextFactory.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                integration = context.Integrations.FirstOrDefault(x => x.Id == integration.Id);
                if (integration == null) throw new NotFound();
                user = context.Users.FirstOrDefault(x => x.Id == user.Id);
                if (!integration.Permissions.Any(x => x.ShareWith.Id == user.Organization.Id))
                {
                    throw new Forbidden(string.Format("You are not allowed to view this integration"));
                }
                var descQuery = OrionQuery.Factory.CreateDataDescriptionQuery(integration, new ModelTarget[]{} );
                var description = await _orionContext.Query(descQuery);
                SetTargetTypes(integration, description);
                integration.AddDataDescription(description);
                context.SaveChanges();
                return integration;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IntegrationSchemaViewModel> GetSchema(User user, long id)
        {
            using (var contextSrc = _contextFactory.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var ign = _integrationsRepo.GetById(id)
                    .Include(x => x.Fields)
                    .Include(x => x.Models)
                    .ThenInclude(x => x.Model)
                    .ThenInclude(x => x.Targets)
                    .ThenInclude(x => x.Column)
                    .FirstOrDefault();
                if (ign == null)
                {
                    throw new NotFound();
                }

                user = context.Users.FirstOrDefault(x => x.Id == user.Id);
                if (!ign.Permissions.Any(x => x.ShareWith.Id == user.Organization.Id))
                {
                    throw new Forbidden(string.Format("You are not allowed to view this integration"));
                }
                var fields = ign.Fields.Select(x => _mapper.Map<FieldDefinitionViewModel>(x));
                var schema = new IntegrationSchemaViewModel(ign.Id, fields);
                schema.Targets = ign.Models.SelectMany(x => x.Model.Targets)
                    .Select(x => _mapper.Map<ModelTargetViewModel>(x))
                    .ToList();
//                var targets = schema.Targets
//                    .Select(x => new ModelTarget(ign.GetField(x.Id)))
//                    .Where(x => x.Column != null);
//                var descQuery = OrionQuery.Factory.CreateDataDescriptionQuery(ign, targets);
//                var description = await _orionContext.Query(descQuery);
//                SetTargetTypes(ign, description);
//                schema.AddDataDescription(description);
                return schema;
            }
        }

        public async Task<DataIntegration> GetIntegrationForAutobuild(CreateAutomaticModelViewModel modelData)
        {

            using (var context = _contextFactory.Create())
            {
                var integration = _integrationsRepo.GetById(modelData.IntegrationId)
                    .Include(x => x.Fields)
                    .Include(x => x.Models)
                    .Include(x => x.APIKey)
                    .FirstOrDefault();
                if (integration == null)
                {
                    throw new NotFound("Integration not found.");
                }
                if (modelData.IdColumn != null && !string.IsNullOrEmpty(modelData.IdColumn.Name))
                {
                    integration.DataIndexColumn = modelData.IdColumn.Name;
                    context.SaveChanges();
                }
                return integration;
            }
        }
        
        public void SetIndexColumn(DataIntegration integration, string idColumnName)
        {
            using (var ctxSrc = _contextFactory.Create())
            {
                integration = _integrationsRepo.GetById(integration.Id).FirstOrDefault();
                if (integration != null)
                {
                    integration.DataIndexColumn = idColumnName;
                    ctxSrc.SaveChanges();
                }
            }
        }

        public async Task<IntegrationViewModel> GetIntegrationView(User user, long id)
        {
            using (var contextSrc = _contextFactory.Create())
            {
                var context = contextSrc.DbContexts.Get<ManagementDbContext>();
                var ign = _integrationsRepo.GetById(id).FirstOrDefault();
                var schema = await GetSchema(user, id);
                var output = new IntegrationViewModel();
                output.Schema = schema;
                output.UserIsOwner = ign.Owner.Id == user.Id;
                output.AccessLog = _loggingService.GetIntegrationLogs(ign)
                    .Select(x => _mapper.Map<AccessLogViewModel>(x)).ToList();
                output.Permissions = ign.Permissions.Select(x => _mapper.Map<PermissionViewModel>(x)).ToList();
                return output;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataIntegration GetById(long id, bool withPermissions = false)
        {
            using (var context = _contextFactory.Create())
            {
                var qr = _integrationsRepo.GetById(id);
                if (withPermissions)
                {
                    qr = qr.Include(x => x.Permissions);
                }
                return qr.FirstOrDefault();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextApiAuth"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public IIntegration GetByName(IApiAuth contextApiAuth, string name)
        {
            using (var context = _contextFactory.Create())
            {
                var ambientDbContextLocator = new AmbientDbContextLocator();
                var dbContext = ambientDbContextLocator.Get<ManagementDbContext>();
                var integration = dbContext.Integrations
                    .Include(x => x.APIKey)
                    .FirstOrDefault(x => x.APIKey.Id == contextApiAuth.Id && x.Name == name);
                return integration;
            }
        }

        public async Task<DataImportResult> CreateOrAppendToIntegration(User user, ApiAuth apiKey, HttpRequest request)
        {
            string targetFilePath = Path.GetTempFileName();
            string fileContentType = null;
            DataImportResult result = null;
            try
            {
                using (var targetStream = System.IO.File.Create(targetFilePath))
                {
                    var form = await request.StreamFile(targetStream);
                    fileContentType = form.GetValue("mime-type").ToString();
                    var filename = form.GetValue("filename")
                        .ToString().Trim('\"').Replace('.', '_').Replace('-', '_');
                    targetStream.Position = 0;
                    result = await CreateOrAppendToIntegration(targetStream, apiKey, user, fileContentType, filename);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                System.IO.File.Delete(targetFilePath);
            }
            return result;
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
            using (var context = _contextFactory.Create())
            {
                var ambientDbContextLocator = new AmbientDbContextLocator();
                var dbContext = ambientDbContextLocator.Get<ManagementDbContext>();
//                _dbContext.SaveChanges();
            }

            
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
            
            using (var context = _contextFactory.Create())
            {
                owner = _users.GetById(owner.Id).FirstOrDefault();
                apiKey = _keysRepository.GetById(apiKey.Id);

                var ambientDbContextLocator = new AmbientDbContextLocator();
                var dbContext = ambientDbContextLocator.Get<ManagementDbContext>();
                
                if (apiKey != null)
                {
                    var dbApikey = dbContext.ApiKeys.FirstOrDefault(x => x.Id == apiKey.Id);
                    if (dbApikey != null) apiKey = dbApikey;
                }

                var integrationInfo = CreateIntegrationImportTask(inputData, apiKey, owner, mime, name, out var isNewIntegration, out var importTask);
                var result = await importTask.Import();

                if (isNewIntegration)
                {
                    var collection = MongoHelper.GetCollection(integrationInfo.Collection);
                    await importTask.Encode(collection);
                    integrationInfo.Permissions.Add(new Permission
                    {
                        Owner = owner.Organization,
                        ShareWith = owner.Organization,
                        CanRead = true,
                        CanModify = true
                    });
                    dbContext.Integrations.Add(integrationInfo);
                    
                }

                try
                {
                    dbContext.SaveChanges(); //Save any changes done to the integration.
                }
                catch (DbUpdateException dbe)
                {
                    if (!string.IsNullOrEmpty(dbe?.InnerException?.Message)) Console.WriteLine(dbe.InnerException.Message);
                    throw dbe;
                }
                catch (Exception e)
                {
                    throw e;
                }
                return result;
            }
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

        public DataImportTask<ExpandoObject> CreateIntegrationImportTask(IInputSource input,
            ApiAuth apiKey,
            User owner,
            string name)
        {
            DataImportTask<ExpandoObject> importTask;
            bool isNewOne;
            CreateIntegrationImportTask(input, apiKey, owner, name, out isNewOne, out importTask);
            return importTask;
        }
        public DataIntegration CreateIntegrationImportTask(IInputSource input,
            ApiAuth apiKey,
            User owner,
            string name,
            out bool isNewIntegration,
            out DataImportTask<ExpandoObject> importTask)
        {
            var options = new DataImportTaskOptions();
            if (apiKey == null) throw new Exception("Could not resolve an api key for the current user.");
            var integrationInfo = ResolveIntegration(apiKey, owner, name, out isNewIntegration, input);
            options.Source = input;
            options.ApiKey = apiKey;
            options.IntegrationName = integrationInfo.Name;
            options.Integration = integrationInfo;
            importTask = new DataImportTask<ExpandoObject>(options);
            return integrationInfo;
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
            if (apiKey == null) throw new Exception("Could not resolve an api key for the current user.");
            var formatter = ResolveFormatter<ExpandoObject>(mime);
            if (formatter == null)
            {
                throw new Exception("Could not resolve formatter for the given content type!");
            }
            var source = InMemorySource.Create(inputData, formatter);
            return CreateIntegrationImportTask(source, apiKey, owner, name, out isNewIntegration, out importTask);
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
            source.Formatter.SetFieldOptions(source.FieldOptions);
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
                integrationInfo.AggregateKeys = this.GetAggregateKeys(integrationInfo).ToList();
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
        public async Task<DataIntegration> Create(User user, ApiAuth apiKey, string integrationName, string formatType)
        {
            //var apiKey = await _userService.GetCurrentApi();
            if (apiKey == null) throw new Exception("Could not resolve an api key for the current user.");
            //var crUser = await _userService.GetCurrentUser();
            var newIntegration = new DataIntegration();
            newIntegration.Name = integrationName;
            newIntegration.DataEncoding = System.Text.Encoding.UTF8.CodePage;
            newIntegration.DataFormatType = formatType;
            newIntegration.Owner = user;
            newIntegration.APIKey = apiKey;

            using (var context = _contextFactory.Create())
            {
                var ambientDbContextLocator = new AmbientDbContextLocator();
                var dbContext = ambientDbContextLocator.Get<ManagementDbContext>();
                var existingIntegration = dbContext.Integrations
                    .FirstOrDefault(x => x.Name.ToLower() == integrationName.ToLower()
                                         && x.Owner.Id == user.Id);
                if (existingIntegration != null)
                {
                    throw new ObjectAlreadyExists("Integration with the same name already exists!");
                }
                dbContext.Integrations.Add(newIntegration);
                dbContext.SaveChanges();
            }
            return await Task.FromResult(newIntegration);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public IInputFormatter<T> ResolveFormatterFromFile<T>(string filepath)
            where T : class
        {
            var mime = MimeResolver.Resolve(filepath);
            return ResolveFormatter<T>(mime);
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
            if (fileContentType == "text/csv") return true;
            return false;
        }

    }
}
