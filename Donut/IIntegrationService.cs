using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Donut.Data;
using Donut.Data.Format;
using Donut.Integration;
using Donut.IntegrationSource;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;

namespace Donut
{
    public interface IIntegrationService
    {
        IIntegration GetByName(IApiAuth contextApiAuth, string integrationSourceIntegrationName);
        Task<DataImportResult> AppendToIntegration(Data.DataIntegration ign, string filePath, ApiAuth apiKey);

        Task<DataImportResult> AppendToIntegration(Data.DataIntegration ign, InputSource source, ApiAuth apiKey,
            string mime = null);
        Task<DataImportResult> AppendToIntegration(Data.DataIntegration ign, Stream inputData, ApiAuth apiKey,
            string mime = null);
        Task<DataImportResult> CreateOrAppendToIntegration(Stream inputData, string mime = null, string name = null);

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

        Task<Data.DataIntegration> Create(string integrationName, string formatType);
        IInputFormatter<T> ResolveFormatter<T>(string mimeType) where T : class;

        IQueryable<Data.DataIntegration> GetById(long id);
        void Remove(Data.DataIntegration importTaskIntegration);
    }
}