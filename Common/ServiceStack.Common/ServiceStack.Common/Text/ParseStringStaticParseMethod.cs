using System;
using System.Collections.Generic;
using System.Reflection;

namespace ServiceStack.Common.Text
{
	public class ParseStringStaticParseMethod
	{
		const string ParseMethod = "Parse";

		private delegate object ParseDelegate(string value);

		private static readonly Dictionary<Type, ParseDelegate> ParseDelegateCache 
			= new Dictionary<Type, ParseDelegate>();

		public static Func<string, object> GetParseMethod(Type type)
		{
			ParseDelegate parseDelegate;

			lock (ParseDelegateCache)
			{
				if (!ParseDelegateCache.TryGetValue(type, out parseDelegate))
				{
					// Get the static Parse(string) method on the type supplied
					var parseMethodInfo = type.GetMethod(
						ParseMethod, BindingFlags.Public | BindingFlags.Static, null,
						new[] { typeof(string) }, null);

					parseDelegate = (ParseDelegate)Delegate.CreateDelegate(typeof(ParseDelegate), parseMethodInfo);
					ParseDelegateCache[type] = parseDelegate;
				}
			}

			if (parseDelegate != null)
				return value => parseDelegate(value);

			return null;
		}

	}
}