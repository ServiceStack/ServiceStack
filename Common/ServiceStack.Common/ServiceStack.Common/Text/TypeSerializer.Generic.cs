using System;
using System.IO;
using System.Text;

namespace ServiceStack.Common.Text
{
	public class TypeSerializer<T>
	{
		public T DeserializeFromString(string value)
		{
			return DeserializeFromString(value, typeof(T));
		}

		public T DeserializeFromString(string value, Type type)
		{
			if (type == typeof(string)) return (T)(object)value;
			var typeDefinition = TypeDefinition.GetTypeDefinition(type);
			return (T)typeDefinition.GetValue(value);
		}

		public string SerializeToString(T value)
		{
			if (Equals(value, default(T))) return null;
			if (value is string) return value as string;

			var sb = new StringBuilder(4096);
			using (var writer = new StringWriter(sb))
			{
				SerializeToWriter(value, writer);
			}
			return sb.ToString();
		}

		public void SerializeToWriter(T value, TextWriter writer)
		{
			if (Equals(value, default(T))) return;
			var strValue = value as string;
			if (strValue != null)
			{
				writer.Write(strValue);
				return;
			}

			var writeFn = ToStringMethods.GetToStringMethod(value.GetType());
			writeFn(writer, value);
		}
	}
}