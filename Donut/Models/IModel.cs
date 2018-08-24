using System;
using System.Collections.Generic;
using Donut.Data;
using Donut.Lex.Data;
using Netlyt.Interfaces.Models;

namespace Donut.Models
{
    public interface IModel
    {
        ApiAuth APIKey { get; set; }
        long APIKeyId { get; set; }
        string Callback { get; set; }
        DateTime CreatedOn { get; set; }
        string CurrentModel { get; set; }
        ICollection<ModelIntegration> DataIntegrations { get; set; }
        DonutScriptInfo DonutScript { get; set; }
        long? DonutScriptId { get; set; }
        ICollection<FeatureGenerationTask> FeatureGenerationTasks { get; set; }
        string Grouping { get; set; }
        string HyperParams { get; set; }
        long Id { get; set; }
        string ModelName { get; set; }
        ModelTrainingPerformance Performance { get; set; }
        ApiAuth PublicKey { get; set; }
        long? PublicKeyId { get; set; }
        ICollection<ModelRule> Rules { get; set; }
        ICollection<ModelTarget> Targets { get; set; }
        string TrainingParams { get; set; }
        ICollection<TrainingTask> TrainingTasks { get; set; }
        bool UseFeatures { get; set; }
        User User { get; set; }
        string UserId { get; set; }

        string GetFeaturesCollection();
        DataIntegration GetRootIntegration();
        void SetScript(DonutScript script);
    }
}