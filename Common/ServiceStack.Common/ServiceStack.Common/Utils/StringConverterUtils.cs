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
	/// Wrapper around StringSerializer, use it instead
	/// </summary>
	[Obsolete("Use StringSerializer instead")]
	public static class StringConverterUtils
	{
		public static bool CanCreateFromString(Type type)
		{
			return StringSerializer.CanCreateFromString(type);
		}

		public static T Parse<T>(string value)
		{
			return StringSerializer.DeserializeFromString<T>(value);
		}

		public static object Parse(string value, Type type)
		{
			return StringSerializer.DeserializeFromString(value, type);
		}

		public static string ToString(object value)
		{
			return StringSerializer.SerializeToString(value);
		}
	}

}