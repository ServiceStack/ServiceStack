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

namespace ServiceStack.Text.Jsv
{
	public class WriterUtils
	{
		public static void WriteBuiltIn(TextWriter writer, object value)
		{
			writer.Write(value);
		}

		public static void WriteObjectString(TextWriter writer, object value)
		{
			writer.Write(value.ToString().ToCsvField());
		}

		public static void WriteException(TextWriter writer, object value)
		{
			writer.Write(((Exception)value).Message.ToCsvField());
		}

		public static void WriteItemSeperatorIfRanOnce(TextWriter writer, ref bool ranOnce)
		{
			if (ranOnce)
				writer.Write(TypeSerializer.ItemSeperator);
			else
				ranOnce = true;
		}

		public static void WriteString(TextWriter writer, string value)
		{
			writer.Write(value.ToCsvField());
		}

		public static void WriteDateTime(TextWriter writer, object oDateTime)
		{
			writer.Write(DateTimeSerializer.ToShortestXsdDateTimeString((DateTime)oDateTime));
		}

		public static void WriteNullableDateTime(TextWriter writer, object dateTime)
		{
			if (dateTime == null) return;
			writer.Write(DateTimeSerializer.ToShortestXsdDateTimeString((DateTime)dateTime));
		}

		public static void WriteGuid(TextWriter writer, object oValue)
		{
			writer.Write(((Guid)oValue).ToString("N"));
		}

		public static void WriteNullableGuid(TextWriter writer, object oValue)
		{
			if (oValue == null) return;
			writer.Write(((Guid)oValue).ToString("N"));
		}

		public static void WriteBytes(TextWriter writer, object oByteValue)
		{
			if (oByteValue == null) return;
			writer.Write(Convert.ToBase64String((byte[])oByteValue));
		}

		public static void WriteFloat(TextWriter writer, object floatValue)
		{
			if (floatValue == null) return;
			writer.Write(((float)floatValue).ToString(CultureInfo.InvariantCulture));
		}

		public static void WriteDouble(TextWriter writer, object doubleValue)
		{
			if (doubleValue == null) return;
			writer.Write(((double)doubleValue).ToString(CultureInfo.InvariantCulture));
		}

		public static void WriteDecimal(TextWriter writer, object decimalValue)
		{
			if (decimalValue == null) return;
			writer.Write(((decimal)decimalValue).ToString(CultureInfo.InvariantCulture));
		}
	}
}