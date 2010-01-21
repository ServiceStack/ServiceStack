using System;
using System.Globalization;
using System.Xml;

namespace ServiceStack.Common.Text
{
	public static class DateTimeSerializer
	{
		public const string ShortDateTimeFormat = "yyyy-MM-dd";
		public const string XsdDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
		public const string XsdDateTimeFormat3F = "yyyy-MM-ddTHH:mm:ss.fffZ";
		public const string XsdDateTimeFormatSeconds = "yyyy-MM-ddTHH:mm:ssZ";

		public static string ToDateTimeString(DateTime dateTime)
		{
			return dateTime.ToString(XsdDateTimeFormat);
		}

		public static DateTime ParseDateTime(string dateTimeStr)
		{
			return DateTime.ParseExact(dateTimeStr, XsdDateTimeFormat, null);
		}

		public static string ToXsdDateTimeString(DateTime dateTime)
		{
			return XmlConvert.ToString(dateTime, XmlDateTimeSerializationMode.Utc);
		}

		public static DateTime ParseXsdDateTime(string dateTimeStr)
		{
			return XmlConvert.ToDateTime(dateTimeStr, XmlDateTimeSerializationMode.Utc);
		}

		public static string ToShortestXsdDateTimeString(DateTime dateTime)
		{
			var timeOfDay = dateTime.TimeOfDay;
			
			if (timeOfDay.Ticks == 0)
				return dateTime.ToString(ShortDateTimeFormat);

			if (timeOfDay.Milliseconds == 0)
				return dateTime.ToString(XsdDateTimeFormatSeconds);

			return ToXsdDateTimeString(dateTime);
		}

		public static DateTime ParseShortestXsdDateTime(string dateTimeStr)
		{
			if (string.IsNullOrEmpty(dateTimeStr)) 
				return DateTime.MinValue;

			if (dateTimeStr.Length == XsdDateTimeFormat.Length
				|| dateTimeStr.Length == XsdDateTimeFormat3F.Length)
				return XmlConvert.ToDateTime(dateTimeStr, XmlDateTimeSerializationMode.Utc);

			if (dateTimeStr.Length == XsdDateTimeFormatSeconds.Length)
				return DateTime.ParseExact(dateTimeStr, XsdDateTimeFormatSeconds, null,
					DateTimeStyles.AdjustToUniversal);

			return new DateTime(
				int.Parse(dateTimeStr.Substring(0, 4)),
				int.Parse(dateTimeStr.Substring(5, 2)),
				int.Parse(dateTimeStr.Substring(8, 2)),
				0, 0, 0,
				DateTimeKind.Utc);
		}
		
	}
}