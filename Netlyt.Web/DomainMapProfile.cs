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
                .ForMember(x=>x.FeatureImportance, opt=> opt.ResolveUsing(src =>
                {
                    if (src.FeatureImportance == "\"null\"") return null;
                    return src.FeatureImportance;
                }))
                .ForMember(x => x.AdvancedReport, opt => opt.ResolveUsing(src =>
                {
                    if (src.AdvancedReport == "\"null\"") return null;
                    return src.AdvancedReport;
                }))
                .ForMember(x=>x.ModelName, opt=> opt.ResolveUsing(src =>
                {
                    return src.Model!=null ? src.Model.ModelName : "";
                }));
            CreateMap<FieldDefinition, FieldDefinitionViewModel>()
                .ForMember(x=>x.TargetType, opt=>opt.MapFrom(y=>y.TargetType));
            CreateMap<DonutScriptInfo, DonutScriptViewModel>();
            CreateMap<ModelTarget, ModelTargetViewModel>()
                .ForMember(x=>x.Column, opt=> opt.MapFrom(src=>src.Column));
            CreateMap<Model, ModelViewModel>()
                .ForMember(x => x.Performance, opt => opt.MapFrom(src => src.Performance))
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