using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Donut;
using Donut.Data;
using Donut.Models;
using Donut.Source;
using MongoDB.Bson;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service;
using Netlyt.Web.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.Web
{
    public class DomainMapProfile : Profile
    {
        public DomainMapProfile(ModelService modelService)
        {
            CreateMap<ModelTrainingPerformance, ModelTrainingPerformanceViewModel>()
                .ForMember(x => x.FeatureImportance, opt => opt.ResolveUsing(src =>
                {
                    if (src.FeatureImportance == "\"null\"") return null;
                    return src.FeatureImportance;
                }))
                .ForMember(x => x.AdvancedReport, opt => opt.ResolveUsing(src =>
                {
                    if (src.AdvancedReport == "\"null\"") return null;
                    return src.AdvancedReport;
                }));
            CreateMap<FieldDefinition, FieldDefinitionViewModel>()
                .ForMember(x=>x.TargetType, opt=>opt.MapFrom(y=>y.TargetType));
            CreateMap<DonutScriptInfo, DonutScriptViewModel>();
            CreateMap<ModelTarget, ModelTargetViewModel>()
                .ForMember(x=>x.Column, opt=> opt.MapFrom(src=>src.Column));
            CreateMap<Model, ModelViewModel>()
                .ForMember(x => x.BuiltTargets, opt => opt.ResolveUsing(src =>
                {
                    var output = new List<ModelBuildViewModel>();
                    var sourceTargets = src.TrainingTasks
                        .Where(tt => tt.Status == TrainingTaskStatus.Done)
                        .GroupBy(tt => tt.Target.Column.Name)
                        .Select(x=>x.FirstOrDefault());
                    foreach (TrainingTask srcTargetTask in sourceTargets)
                    {
                        var vm = new ModelBuildViewModel();
                        vm.TaskType = srcTargetTask.Target.IsRegression ? "Regression" : "Classification";
                        vm.Id = srcTargetTask.Id;
                        vm.Endpoint = modelService.GetTrainedEndpoint(srcTargetTask);
                        vm.Target = srcTargetTask.Target.Column.Name;
                        vm.CurrentModel = srcTargetTask.TypeInfo;
                        vm.Performance = new ModelTrainingPerformanceViewModel();
                        vm.Performance.Accuracy = srcTargetTask.Performance.Accuracy;
                        vm.Performance.AdvancedReport = srcTargetTask.Performance.AdvancedReport;
                        vm.Performance.FeatureImportance = srcTargetTask.Performance.FeatureImportance;
                        vm.Performance.Id = srcTargetTask.Performance.Id;
                        vm.Performance.IsRegression = srcTargetTask.Target.IsRegression;
                        vm.Performance.LastRequestIP = srcTargetTask.Performance.LastRequestIP;
                        vm.Performance.LastRequestTs = srcTargetTask.Performance.LastRequestTs;
                        vm.Performance.MontlyUsage = srcTargetTask.Performance.MonthlyUsage;
                        vm.Performance.WeeklyUsage = srcTargetTask.Performance.WeeklyUsage;
                        vm.Performance.TargetName = srcTargetTask.Target.Column.Name;
                        vm.Performance.TaskType = vm.TaskType;
                        vm.Scoring = srcTargetTask.Scoring;
                        output.Add(vm);
                    }
                    return output;
                }))
                .ForMember(x => x.Status,
                    opt => opt.ResolveUsing(src => { return modelService.GetModelStatus(src).ToString(); }))
                .ForMember(x => x.IsBuilding, opt => opt.ResolveUsing(src => modelService.IsBuilding(src)));
            CreateMap<ApiAuth, ApiAuthViewModel>()
                .ForMember(x => x.AppId, opt => opt.MapFrom(src => src.AppId));
            CreateMap<User, UserViewModel>()
                .ForMember(x => x.Role, opt => opt.ResolveUsing(src =>
                {
                    return src.Role?.Name;
                }));
            CreateMap<IntegrationExtra, IntegrationExtraViewModel>();
            CreateMap<DataIntegration, DataIntegrationViewModel>(); 
        }
    }
}