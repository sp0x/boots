using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Netlyt.Web.Extensions
{
    public static class RouteExtensions
    {

        public static IEnumerable<dynamic> GetRoutes(this IActionDescriptorCollectionProvider provider)
        {
            var routes = provider.ActionDescriptors.Items
                .Select(x => new {
                    Action = x.RouteValues["Action"],
                    Controller = x.RouteValues["Controller"],
                    Name = x.AttributeRouteInfo?.Name,
                    Template = x.AttributeRouteInfo?.Template,
                    Contraint = x.ActionConstraints
                }).ToList();
            return routes;
        }
    }
}