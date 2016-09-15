using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.DataAnnotations;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public delegate void RouteNamingConventionDelegate(IServiceRoutes routes, Type requestType, string allowedVerbs);

    public static class RouteNamingConvention
    {
        private const int AutoGenPriority = -1;

        public static readonly List<string> AttributeNamesToMatch = new[] {
            nameof(PrimaryKeyAttribute),
		}.ToList();

        public static readonly List<string> PropertyNamesToMatch = new[] {
            IdUtils.IdField,
            "IDs",
        }.ToList();

        public static void WithRequestDtoName(IServiceRoutes routes, Type requestType, string allowedVerbs)
        {
            routes.Add(requestType, restPath: $"/{requestType.GetOperationName()}", verbs: allowedVerbs, priority: AutoGenPriority);
        }

        public static void WithMatchingAttributes(IServiceRoutes routes, Type requestType, string allowedVerbs)
        {
            var membersWithAttribute = (from p in requestType.GetPublicProperties()
                                        let attributes = p.AllAttributes<Attribute>()
                                        where attributes.Any(a => AttributeNamesToMatch.Contains(a.GetType().GetOperationName()))
                                        select $"{{{p.Name}}}").ToList();

            if (membersWithAttribute.Count == 0) return;

            membersWithAttribute.Insert(0, $"/{requestType.GetOperationName()}");

            var restPath = membersWithAttribute.Join("/");
            routes.Add(requestType, restPath: restPath, verbs: allowedVerbs, priority: AutoGenPriority);
        }

        public static void WithMatchingPropertyNames(IServiceRoutes routes, Type requestType, string allowedVerbs)
        {
            var membersWithName = (from property in requestType.GetPublicProperties().Select(p => p.Name)
                                   from name in PropertyNamesToMatch
                                   where property.Equals(name, StringComparison.OrdinalIgnoreCase)
                                   select $"{{{property}}}").ToList();

            if (membersWithName.Count == 0) return;

            membersWithName.Insert(0, $"/{requestType.GetOperationName()}");

            var restPath = membersWithName.Join("/");
            routes.Add(requestType, restPath: restPath, verbs: allowedVerbs, priority: AutoGenPriority);
        }
    }
}
