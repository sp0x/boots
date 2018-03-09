using AutoMapper;
using nvoid.Integration;
using Netlyt.Service;
using Netlyt.Service.Integration;
using Netlyt.Service.Ml;
using Netlyt.Service.Source;
using Netlyt.Web.ViewModels;

namespace Netlyt.Web
{
    public class DomainMapProfile : Profile
    {
        public DomainMapProfile()
        {
            CreateMap<Model, ModelViewModel>();
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