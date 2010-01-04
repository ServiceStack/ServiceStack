using System;
using System.Reflection;
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

		public static TAttribute FirstAttribute<TAttribute>(this Type type)
		{
			return type.FirstAttribute<TAttribute>(true);
		}

		public static TAttribute FirstAttribute<TAttribute>(this Type type, bool inherit)
		{
			var attrs = type.GetCustomAttributes(typeof(TAttribute), inherit);
			return (TAttribute)(attrs.Length > 0 ? attrs[0] : null);
		}

		public static TAttribute FirstAttribute<TAttribute>(this PropertyInfo propertyInfo)
		{
			return propertyInfo.FirstAttribute<TAttribute>(true);
		}

		public static TAttribute FirstAttribute<TAttribute>(this PropertyInfo propertyInfo, bool inherit)
		{
			var attrs = propertyInfo.GetCustomAttributes(typeof(TAttribute), inherit);
			return (TAttribute)(attrs.Length > 0 ? attrs[0] : null);
		}

		public static bool IsGenericType(this Type type)
		{
			while (type != null)
			{
				if (type.IsGenericType)
					return true;

				type = type.BaseType;
			}
			return false;
		}

		public static Type FirstGenericTypeDefinition(this Type type)
		{
			while (type != null)
			{
				if (type.IsGenericType)
					return type.GetGenericTypeDefinition();

				type = type.BaseType;
			}

			return null;
		}

	}
}