using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Donut.Data;
using Donut.Data.Format;
using Donut.Integration;
using Donut.IntegrationSource;
using Donut.Models;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using Netlyt.Data.ViewModels;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Cloud;
using Netlyt.Interfaces.Models;
using Newtonsoft.Json.Linq;

namespace Donut
{
    public interface IIntegrationService
    {
        DataIntegration GetUserIntegration(User user, long id);
        //DataIntegration GetUserIntegration(User user, DataIntegration integration);
        Task<IEnumerable<DataIntegration>> GetIntegrations(User user, int page, int pageSize);
        Task<IEnumerable<DataIntegration>> GetIntegrations(User currentUser, string targetUserId, int page, int pageSize);
        DataIntegration GetUserIntegration(User user, string name);
        IIntegration GetByName(IApiAuth contextApiAuth, string integrationSourceIntegrationName);
        Task<DataImportResult> AppendToIntegration(DataIntegration ign, string filePath, ApiAuth apiKey);

        Task<DataImportResult> AppendToIntegration(DataIntegration ign, InputSource source, ApiAuth apiKey,
            string mime = null);
        Task<DataImportResult> AppendToIntegration(DataIntegration ign, Stream inputData, ApiAuth apiKey,
            string mime = null);

        Task<DataImportResult> CreateOrAppendToIntegration(Stream inputData, ApiAuth apiKey, User owner,
            string mime = null, string name = null);
        Task<DataImportResult> CreateOrAppendToIntegration(User user, ApiAuth apikey, HttpRequest request);

        Task<DataImportResult> CreateOrAppendToIntegration(string filePath, ApiAuth apiKey, User user,
            string name = null);

        DataImportTask<ExpandoObject> CreateIntegrationImportTask(string filePath, ApiAuth apiKey, User user,
            string name = null);

        Data.DataIntegration ResolveIntegration(ApiAuth apiKey, User owner, string name, out bool isNewIntegration,
            IInputSource source);

        IEnumerable<AggregateKey> GetAggregateKeys(IIntegration integration);

        Data.DataIntegration CreateIntegrationImportTask(Stream inputData,
            ApiAuth apiKey,
            User owner,
            string mime,
            string name,
            out bool isNewIntegration,
            out DataImportTask<ExpandoObject> importTask);

        DataIntegration CreateIntegrationImportTask(IInputSource input,
            ApiAuth apiKey,
            User owner,
            string name,
            out bool isNewIntegration,
            out DataImportTask<ExpandoObject> importTask);

        DataImportTask<ExpandoObject> CreateIntegrationImportTask(IInputSource input,
            ApiAuth apiKey,
            User owner,
            string name);

        Task<DataIntegration> Create(User user, ApiAuth apiKey, string integrationName, string formatType);
        IInputFormatter<T> ResolveFormatter<T>(string mimeType) where T : class;

        DataIntegration GetById(long id, bool withPermissions=false);
        void Remove(DataIntegration importTaskIntegration);
        void SetTargetTypes(DataIntegration ign, JToken description);
        Task<BsonDocument> GetTaskDataSample(TrainingTask trainingTask);
        void OnRemoteIntegrationCreated(ICloudNodeNotification notification, JToken eBody);
        Task<IntegrationSchemaViewModel> GetSchema(User user,long id);
        Task<DataIntegration> ResolveDescription(User user, DataIntegration integration);
        Task<DataIntegration> GetIntegrationForAutobuild(CreateAutomaticModelViewModel modelData);
        void SetIndexColumn(DataIntegration integration, string idColumnName);
        Task<IntegrationViewModel> GetIntegrationView(User user, long id);
    }
}