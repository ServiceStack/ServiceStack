using System;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common.Extensions
{
	public static class ReflectionExtensions
	{
		public static object PopulateWith(this object to, object from)
		{
			return ReflectionUtils.PopulateObject(to, from);
		}

		public static object PopulateWithNonDefaultValues(this object to, object from)
		{
			return ReflectionUtils.PopulateWithNonDefaultValues(to, from);
		}

		public static object PopulateFromPropertiesWithAttribute<T>(this object to, object from)
		{
			return ReflectionUtils.PopulateFromPropertiesWithAttribute(to, from, typeof(T));
		}
	}
}