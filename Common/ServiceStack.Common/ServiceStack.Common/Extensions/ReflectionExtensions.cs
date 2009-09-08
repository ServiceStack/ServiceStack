using System;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common.Extensions
{
	public static class ReflectionExtensions
	{
		public static To PopulateWith<To, From>(this To to, From from)
		{
			return ReflectionUtils.PopulateObject(to, from);
		}

		public static To PopulateWithNonDefaultValues<To, From>(this To to, From from)
		{
			return ReflectionUtils.PopulateWithNonDefaultValues(to, from);
		}

		public static To PopulateFromPropertiesWithAttribute<To, From, TAttr>(this To to, From from)
		{
			return ReflectionUtils.PopulateFromPropertiesWithAttribute(to, from, typeof(TAttr));
		}

		public static T TranslateTo<T>(this object from)
			where T : new()
		{
			var to = new T();
			return to.PopulateWith(from);
		}

	}
}