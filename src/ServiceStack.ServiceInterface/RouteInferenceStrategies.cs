using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Common.Utils;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.ServiceInterface
{

	public static class RouteInferenceStrategies
	{

		public static readonly List<Type> AttributesToMatch = (new[] {
			typeof(PrimaryKeyAttribute)
		}).ToList();

		public static readonly List<string> PropertyNamesToMatch = (new[] {
			IdUtils.IdField,
			"IDs"
		}).ToList();


		public static string FromRequestTypeName(Type requestType)
		{
			return "/{0}".FormatWith(requestType.Name);
		}

		public static string FromAttributes(Type requestType)
		{
			var membersWithAttribute = (from p in requestType.GetPublicProperties()
										let attributes = p.CustomAttributes(inherit: false).Cast<Attribute>()
										where attributes.Any(a => AttributesToMatch.Contains(a.GetType()))
										select "{{{0}}}".FormatWith(p.Name)).ToList();

			if (membersWithAttribute.Count == 0) return null;

			membersWithAttribute.Insert(0, FromRequestTypeName(requestType));

			return membersWithAttribute.Join("/");
		}

		public static string FromPropertyNames(Type requestType)
		{
			var membersWithName = (from property in requestType.GetPublicProperties().Select(p => p.Name)
								   from name in PropertyNamesToMatch
								   where property.Equals(name, StringComparison.InvariantCultureIgnoreCase)
								   select "{{{0}}}".FormatWith(property)).ToList();

			if (membersWithName.Count == 0) return null;

			membersWithName.Insert(0, FromRequestTypeName(requestType));

			return membersWithName.Join("/");
		}
	}
}
