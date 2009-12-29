using System.Collections;
using System.Text;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Common.Text
{
	internal static class ToStringMethods
	{
		const char FieldSeperator = ',';
		const char KeySeperator = ':';

		public static string ToString(object value)
		{
			return value.ToString();
		}

		public static string ToString(byte[] byteValue)
		{
			return byteValue == null ? null : Encoding.Default.GetString(byteValue);
		}

		public static string ToString(IEnumerable valueCollection)
		{
			var sb = new StringBuilder();
			foreach (var valueItem in valueCollection)
			{
				var stringValueItem = valueItem.ToString();
				if (sb.Length > 0)
				{
					sb.Append(FieldSeperator);
				}
				sb.Append(stringValueItem.ToSafeString());
			}
			return sb.ToString();
		}

		public static string ToString(IDictionary valueDictionary)
		{
			var sb = new StringBuilder();
			foreach (var key in valueDictionary.Keys)
			{
				var keyString = key.ToString();
				var dictionaryValue = valueDictionary[key];
				var valueString = dictionaryValue != null ? dictionaryValue.ToString() : string.Empty;

				if (sb.Length > 0)
				{
					sb.Append(FieldSeperator);
				}
				sb.Append(keyString.ToSafeString())
					.Append(KeySeperator)
					.Append(valueString.ToSafeString());
			}
			return sb.ToString();
		}

	}
}