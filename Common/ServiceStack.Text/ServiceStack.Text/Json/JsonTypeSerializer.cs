//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Globalization;
using System.IO;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Json
{
	public class JsonTypeSerializer 
		: ITypeSerializer
	{
		public static ITypeSerializer Instance = new JsonTypeSerializer();

		public Action<TextWriter, object> GetWriteFn<T>()
		{
			return JsonWriter<T>.WriteFn();
		}

		public Action<TextWriter, object> GetWriteFn(Type type)
		{
			return JsonWriter.GetWriteFn(type);
		}

		/// <summary>
		/// Shortcut escape when we're sure value doesn't contain any escaped chars
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteRawString(TextWriter writer, string value)
		{
			writer.Write('"');
			writer.Write(value);
			writer.Write('"');
		}

		public void WritePropertyName(TextWriter writer, string value)
		{
			WriteRawString(writer, value);
		}

		public void WriteString(TextWriter writer, string value)
		{
			JsonUtils.WriteString(writer, value);
		}

		public void WriteBuiltIn(TextWriter writer, object value)
		{
			WriteRawString(writer, value.ToString());
		}

		public void WriteObjectString(TextWriter writer, object value)
		{
			if (value != null)
			{
				WriteString(writer, value.ToString());
			}
		}

		public void WriteException(TextWriter writer, object value)
		{
			WriteString(writer, ((Exception)value).Message);
		}

		public void WriteDateTime(TextWriter writer, object oDateTime)
		{
			WriteRawString(writer, "/Date(" + ((DateTime)oDateTime).ToUnixTime() + ")/");
		}

		public void WriteNullableDateTime(TextWriter writer, object dateTime)
		{
			if (dateTime == null) return;
			WriteRawString(writer, "/Date(" + ((DateTime)dateTime).ToUnixTime() + ")/");
		}

		public void WriteGuid(TextWriter writer, object oValue)
		{
			WriteRawString(writer, ((Guid)oValue).ToString("N"));
		}

		public void WriteNullableGuid(TextWriter writer, object oValue)
		{
			if (oValue == null) return;
			WriteRawString(writer, ((Guid)oValue).ToString("N"));
		}

		public void WriteBytes(TextWriter writer, object oByteValue)
		{
			if (oByteValue == null) return;
			WriteRawString(writer, Convert.ToBase64String((byte[])oByteValue));
		}

		public void WriteFloat(TextWriter writer, object floatValue)
		{
			if (floatValue == null) return;
			writer.Write(((float)floatValue).ToString(CultureInfo.InvariantCulture));
		}

		public void WriteDouble(TextWriter writer, object doubleValue)
		{
			if (doubleValue == null) return;
			writer.Write(((double)doubleValue).ToString(CultureInfo.InvariantCulture));
		}

		public void WriteDecimal(TextWriter writer, object decimalValue)
		{
			if (decimalValue == null) return;
			writer.Write(((decimal)decimalValue).ToString(CultureInfo.InvariantCulture));
		}
	}
}