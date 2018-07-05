using AutoMapper;
using Donut;
using Donut.Models;
using Donut.Source;
using MongoDB.Bson;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Web.ViewModels;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.Web
{
    public class DomainMapProfile : Profile
    {
        public DomainMapProfile()
        {
            CreateMap<ModelTrainingPerformance, ModelTrainingPerformanceViewModel>();
            CreateMap<Model, ModelViewModel>()
                .ForMember(x => x.Performance, opt => opt.MapFrom(src => src.Performance));
            CreateMap<ApiAuth, ApiAuthViewModel>()
                .ForMember(x => x.AppId, opt => opt.MapFrom(src => src.AppId));
            CreateMap<User, UserViewModel>()
                .ForMember(x => x.Role, opt => opt.ResolveUsing(src =>
                {
                    return src.Role?.Name;
                }));
            CreateMap<IntegrationExtra, IntegrationExtraViewModel>();
            CreateMap<FieldDefinition, FieldDefinitionViewModel>();
            CreateMap<DataIntegration, DataIntegrationViewModel>(); 
        }
    }
}