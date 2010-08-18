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
using System.Xml;
using ServiceStack.Text.Json;

namespace ServiceStack.Text.Common
{
	public static class DateTimeSerializer
	{
		public const string ShortDateTimeFormat = "yyyy-MM-dd";					//11
		public const string DefaultDateTimeFormat = "dd/MM/yyyy HH:mm:ss";		//20
		public const string DefaultDateTimeFormatWithFraction = "dd/MM/yyyy HH:mm:ss.fff";	//24
		public const string XsdDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";	//29
		public const string XsdDateTimeFormat3F = "yyyy-MM-ddTHH:mm:ss.fffZ";	//25
		public const string XsdDateTimeFormatSeconds = "yyyy-MM-ddTHH:mm:ssZ";	//21

		public const string EscapedWcfJsonPrefix = "\\/Date(";
		public const string WcfJsonPrefix = "/Date(";

		public static DateTime ParseShortestXsdDateTime(string dateTimeStr)
		{
			if (string.IsNullOrEmpty(dateTimeStr))
				return DateTime.MinValue;

			if (dateTimeStr.StartsWith(EscapedWcfJsonPrefix) || dateTimeStr.StartsWith(WcfJsonPrefix))
				return ParseWcfJsonDate(dateTimeStr);

			if (dateTimeStr.Length == DefaultDateTimeFormat.Length
				|| dateTimeStr.Length == DefaultDateTimeFormatWithFraction.Length)
				return DateTime.Parse(dateTimeStr, CultureInfo.InvariantCulture);

			if (dateTimeStr.Length == XsdDateTimeFormatSeconds.Length)
				return DateTime.ParseExact(dateTimeStr, XsdDateTimeFormatSeconds, null,
										   DateTimeStyles.AdjustToUniversal);

			if (dateTimeStr.Length >= XsdDateTimeFormat3F.Length
				&& dateTimeStr.Length <= XsdDateTimeFormat.Length)
				return XmlConvert.ToDateTime(dateTimeStr, XmlDateTimeSerializationMode.Local);

			return new DateTime(
				int.Parse(dateTimeStr.Substring(0, 4)),
				int.Parse(dateTimeStr.Substring(5, 2)),
				int.Parse(dateTimeStr.Substring(8, 2)),
				0, 0, 0,
				DateTimeKind.Local);
		}

		public static string ToDateTimeString(DateTime dateTime)
		{
			return dateTime.ToUniversalTime().ToString(XsdDateTimeFormat);
		}

		public static DateTime ParseDateTime(string dateTimeStr)
		{
			return DateTime.ParseExact(dateTimeStr, XsdDateTimeFormat, null);
		}

		public static string ToXsdDateTimeString(DateTime dateTime)
		{
			return XmlConvert.ToString(dateTime.ToUniversalTime(), XmlDateTimeSerializationMode.Utc);
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
				return dateTime.ToUniversalTime().ToString(XsdDateTimeFormatSeconds);

			return ToXsdDateTimeString(dateTime);
		}


		static readonly char[] TimeZoneChars = new[] { '+', '-' };
		/// <summary>
		/// WCF Json format: /Date(unixts+0000)/
		/// </summary>
		/// <param name="wcfJsonDate"></param>
		/// <returns></returns>
		public static DateTime ParseWcfJsonDate(string wcfJsonDate)
		{

			if (wcfJsonDate[0] == JsonUtils.EscapeChar)
			{
				wcfJsonDate = wcfJsonDate.Substring(1);
			}
			var timeZonePos = wcfJsonDate.IndexOfAny(TimeZoneChars, WcfJsonPrefix.Length + 1);
			if (timeZonePos != -1)
			{
				var timeZoneMultiplier = wcfJsonDate.IndexOf('-') == -1 ? 0 : -1;
				var unixTimeString = wcfJsonDate.Substring(WcfJsonPrefix.Length, timeZonePos - WcfJsonPrefix.Length);
				var timeZoneString = wcfJsonDate.Substring(timeZonePos + 1, wcfJsonDate.IndexOf(')') - 1 - timeZonePos);
				var unixTime = double.Parse(unixTimeString);
				var dateTime = unixTime.FromUnixTimeMs();

				const string defaultUtcTimeZone = "0000";
				if (timeZoneString == defaultUtcTimeZone)
				{
					return dateTime;
				}
				var timeZoneHours = int.Parse(timeZoneString.Substring(0, 2));
				var timeZoneMins = int.Parse(timeZoneString.Substring(2, 2));

				var timeZoneOffset = new TimeSpan(timeZoneHours * timeZoneMultiplier, timeZoneMins * timeZoneMultiplier, 0);
				return new DateTimeOffset(dateTime.Ticks, timeZoneOffset).DateTime;
			}
			else
			{
				var unixTimeString = wcfJsonDate.Substring(
					WcfJsonPrefix.Length, wcfJsonDate.IndexOf(')') - WcfJsonPrefix.Length);

				var unixTime = double.Parse(unixTimeString);
				return unixTime.FromUnixTimeMs();
			}
		}

		public static string ToWcfJsonDate(DateTime dateTime)
		{
			return EscapedWcfJsonPrefix + dateTime.ToUnixTimeMs() + "+0000)\\/";
		}
	}
}