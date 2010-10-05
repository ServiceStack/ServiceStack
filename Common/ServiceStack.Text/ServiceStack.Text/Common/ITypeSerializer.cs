using System;
using System.IO;

namespace ServiceStack.Text.Common
{
	internal interface ITypeSerializer
	{
		WriteObjectDelegate GetWriteFn<T>();
		WriteObjectDelegate GetWriteFn(Type type);

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
		void WriteInteger(TextWriter writer, object integerValue);
		void WriteBool(TextWriter writer, object boolValue);
		void WriteFloat(TextWriter writer, object floatValue);
		void WriteDouble(TextWriter writer, object doubleValue);
		void WriteDecimal(TextWriter writer, object decimalValue);

		//object EncodeMapKey(object value);

		ParseStringDelegate GetParseFn<T>();
		ParseStringDelegate GetParseFn(Type type);

		string ParseRawString(string value);
		string ParseString(string value);
		string EatTypeValue(string value, ref int i);
		bool EatMapStartChar(string value, ref int i);
		string EatMapKey(string value, ref int i);
		bool EatMapKeySeperator(string value, ref int i);
		string EatValue(string value, ref int i);
		bool EatItemSeperatorOrMapEndChar(string value, ref int i);
	}
}