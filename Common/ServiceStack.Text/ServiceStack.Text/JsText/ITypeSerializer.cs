using System;
using System.IO;

namespace ServiceStack.Text.JsText
{
	public interface ITypeSerializer
	{
		Action<TextWriter, object> GetWriteFn(Type type);

		void WriteRawString(TextWriter writer, string value);
		void WritePropertyName(TextWriter writer, string value);

		void WriteBuiltIn(TextWriter writer, object value);
		void WriteObjectString(TextWriter writer, object value);
		void WriteException(TextWriter writer, object value);
		void WriteString(TextWriter writer, string value);
		void WriteDateTime(TextWriter writer, object oDateTime);
		void WriteNullableDateTime(TextWriter writer, object dateTime);
		void WriteGuid(TextWriter writer, object oValue);
		void WriteNullableGuid(TextWriter writer, object oValue);
		void WriteBytes(TextWriter writer, object oByteValue);
		void WriteFloat(TextWriter writer, object floatValue);
		void WriteDouble(TextWriter writer, object doubleValue);
		void WriteDecimal(TextWriter writer, object decimalValue);
	}
}