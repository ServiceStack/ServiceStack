using System;
using System.Reflection;

namespace ServiceStack.Common.Text
{
	public class ParseStringStaticParseMethod
	{
		const string ParseMethod = "Parse";

		private delegate object ParseDelegate(string value);

		public static Func<string, object> GetParseMethod(Type type)
		{
			// Get the static Parse(string) method on the type supplied
			var parseMethodInfo = type.GetMethod(
				ParseMethod, BindingFlags.Public | BindingFlags.Static, null,
				new[] { typeof(string) }, null);

			if (parseMethodInfo == null) return null;

			var parseDelegate = (ParseDelegate)Delegate.CreateDelegate(typeof(ParseDelegate), parseMethodInfo);

			if (parseDelegate != null)
				return value => parseDelegate(value);

			return null;
		}

	}
}