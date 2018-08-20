using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Donut.Data;
using Donut.Source;
using Microsoft.Extensions.DependencyInjection;
using Netlyt.Data.ViewModels;
using Netlyt.Service;

namespace Netlyt.Web.Extensions
{
    public static class Extensions
    {
        public static void AddDomainAutomapper(this IServiceCollection sp)
        {
            sp.AddTransient(p => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new DomainMapProfile(p.GetService<ModelService>(), p.GetService<UserService>()));
            }).CreateMapper());
        }
        public static IEnumerable<ModelTarget> ToModelTargets(this IEnumerable<TargetSelectionViewModel> viewmodels, DataIntegration integration)
        {
            foreach (var vm in viewmodels)
            {
                var modelTarget = new ModelTarget(integration.GetField(vm.FieldId));
                if (vm.TimeShift != null)
                {
                    var constraint = vm.TimeShift.TimeToTargetConstraint(integration.DataTimestampColumn);
                    if (constraint != null)
                    {
                        modelTarget.Constraints.Add(constraint);
                    }
                }
                yield return modelTarget;
            }
        }

        public static TargetConstraint TimeToTargetConstraint(this TimeConstraintViewModel timeshift, string timestampColumn)
        {
            if (string.IsNullOrEmpty(timestampColumn)) return null;
            var constraint = new TargetConstraint();
            constraint.Type = TargetConstraintType.Time;
            constraint.Key = timestampColumn;
            constraint.After = new TimeConstraint();
            constraint.After.Years = timeshift.Year;
            constraint.After.Months = timeshift.Month;
            constraint.After.Days = timeshift.Day;
            constraint.After.Hours = timeshift.Hour;
            return constraint;
        }
    }
}
