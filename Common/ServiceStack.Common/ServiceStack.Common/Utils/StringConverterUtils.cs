using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Text;

namespace ServiceStack.Common.Utils
{
	/// <summary>
	/// Wrapper around TypeSerializer, use it instead
	/// </summary>
	[Obsolete("Use TypeSerializer instead")]
	public static class StringConverterUtils
	{
		public static bool CanCreateFromString(Type type)
		{
			return TypeSerializer.CanCreateFromString(type);
		}

		public static T Parse<T>(string value)
		{
			return TypeSerializer.DeserializeFromString<T>(value);
		}

		public static object Parse(string value, Type type)
		{
			return TypeSerializer.DeserializeFromString(value, type);
		}

		public static string ToString(object value)
		{
			return TypeSerializer.SerializeToString(value);
		}
	}

}