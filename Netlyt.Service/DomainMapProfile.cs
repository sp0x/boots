using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Donut;
using Donut.Data;
using Donut.Models;
using Donut.Source;
using Netlyt.Data.ViewModels;
using Netlyt.Interfaces;
using Netlyt.Interfaces.Models;
using Netlyt.Service.Cloud;
using Newtonsoft.Json.Linq;
using DataIntegration = Donut.Data.DataIntegration;

namespace Netlyt.Service
{
    public class DomainMapProfile : Profile
    {
        public DomainMapProfile(IServiceProvider services)
        {
            CreateMap<ApiAuth, AuthKeyViewModel>()
                .ForMember(x=>x.Secret, opt=>opt.MapFrom(src=>src.AppSecret))
                .ForMember(x=>x.Key, opt=>opt.MapFrom(src=>src.AppId));
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
                .ForMember(x=>x.TargetType, opt=>opt.MapFrom(y=>y.TargetType))
                .ForMember(x=>x.DType, opt=>opt.MapFrom(src=> src.DataType))
                .ForMember(x=>x.Description, opt=> opt.MapFrom(src=> JObject.Parse(src.DescriptionJson)));
            CreateMap<DonutScriptInfo, DonutScriptViewModel>();
            CreateMap<Permission, PermissionViewModel>();
            CreateMap<ModelTarget, ModelTargetViewModel>()
                .ForMember(x=>x.Column, opt=> opt.MapFrom(src=>src.Column));
            CreateMap<Model, ModelViewModel>()
                .ForMember(x=>x.ApiKey, opt=> opt.Ignore())
                .ForMember(x=>x.Permissions, opt=> opt.Ignore())
                .ForMember(x => x.BuiltTargets, opt => opt.ResolveUsing(src =>
                {
                    var modelService = services.GetService(typeof(ModelService)) as ModelService;
                    var output = modelService.GetBuildViews(src);
                    return output;
                }))
                .ForMember(x => x.Status,
                    opt => opt.ResolveUsing(src =>
                    {
                        var modelService = services.GetService(typeof(ModelService)) as ModelService;
                        return modelService.GetModelStatus(src).ToString();
                    }))
                .ForMember(x => x.IsBuilding, opt => opt.ResolveUsing(src =>
                {
                    var modelService = services.GetService(typeof(ModelService)) as ModelService;
                    return modelService.IsBuilding(src);
                }));
            CreateMap<ApiAuth, ApiAuthViewModel>()
                .ForMember(x => x.AppId, opt => opt.MapFrom(src => src.AppId));
            CreateMap<ApiUser, ApiAuthViewModel>()
                .ForMember(x => x.AppId, opt => opt.MapFrom(src => src.Api.AppId))
                .ForMember(x=>x.Id, opt=>opt.MapFrom(src=>src.ApiId));
            CreateMap<User, UserViewModel>()
                .ForMember(x => x.Role, opt => opt.ResolveUsing(src => src.Role?.Name));
            CreateMap<IntegrationExtra, IntegrationExtraViewModel>();
            CreateMap<DataIntegration, DataIntegrationViewModel>();
            CreateMap<User, UsersViewModel>()
                .ForMember(x=>x.Roles, opt=>opt.ResolveUsing(src =>
                {
                    var userService = services.GetService(typeof(IUserManagementService)) as IUserManagementService;
                    return userService.GetRoles(src).Result;
                }))
                .ForMember(x=>x.HasRemoteInstance, opt=>opt.ResolveUsing(src =>
                {
                    var userService = services.GetService(typeof(ICloudNodeService)) as ICloudNodeService;
                    return userService.UserHasOnPremInstance(src);
                }));
            CreateMap<User, UserPreviewViewModel>()
                .ForMember(x=>x.Username, opt=> opt.ResolveUsing(src=> src.UserName))
                .ForMember(x => x.Roles, opt => opt.ResolveUsing(src =>
                {
                    var userService = services.GetService(typeof(IUserManagementService)) as IUserManagementService;
                    return userService.GetRoles(src).Result;
                }));
            CreateMap<ActionLog, AccessLogViewModel>()
                .ForMember(x=>x.User, opt=> opt.ResolveUsing(src=> src.User?.UserName))
                .ForMember(x=>x.Event, opt=> opt.ResolveUsing(src=> src.Type.ToString()))
                .ForMember(x=>x.CreatedOn, opt=> opt.ResolveUsing(src=> src.Created));
        }
    }
}