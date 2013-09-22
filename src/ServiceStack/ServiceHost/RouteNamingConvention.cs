using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Server;
using ServiceStack.Text;
using ServiceStack.Utils;

namespace ServiceStack.ServiceHost
{
    public delegate void RouteNamingConventionDelegate(IServiceRoutes routes, Type requestType, string allowedVerbs);

    public static class RouteNamingConvention
    {
        public static readonly List<string> AttributeNamesToMatch = new[] {
			"PrimaryKeyAttribute",//typeof(PrimaryKeyAttribute),
		}.ToList();

        public static readonly List<string> PropertyNamesToMatch = new[] {
			IdUtils.IdField,
			"IDs",
		}.ToList();

        public static void WithRequestDtoName(IServiceRoutes routes, Type requestType, string allowedVerbs)
        {
            routes.Add(requestType, restPath: "/{0}".Fmt(requestType.Name), verbs: allowedVerbs);
        }

        public static void WithMatchingAttributes(IServiceRoutes routes, Type requestType, string allowedVerbs)
        {
            var membersWithAttribute = (from p in requestType.GetPublicProperties()
                                        let attributes = p.CustomAttributes(inherit: false).Cast<Attribute>()
                                        where attributes.Any(a => AttributeNamesToMatch.Contains(a.GetType().Name))
                                        select "{{{0}}}".Fmt(p.Name)).ToList();

            if (membersWithAttribute.Count == 0) return;

            membersWithAttribute.Insert(0, "/{0}".Fmt(requestType.Name));

            var restPath = membersWithAttribute.Join("/");
            routes.Add(requestType, restPath: restPath, verbs: allowedVerbs);
        }

        public static void WithMatchingPropertyNames(IServiceRoutes routes, Type requestType, string allowedVerbs)
        {
            var membersWithName = (from property in requestType.GetPublicProperties().Select(p => p.Name)
                                   from name in PropertyNamesToMatch
                                   where property.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                                   select "{{{0}}}".Fmt(property)).ToList();

            if (membersWithName.Count == 0) return;

            membersWithName.Insert(0, "/{0}".Fmt(requestType.Name));

            var restPath = membersWithName.Join("/");
            routes.Add(requestType, restPath: restPath, verbs: allowedVerbs);
        }
    }
}
