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
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text
{
	/// <summary>
	/// A fast, standards-based, serialization-issue free DateTime serailizer.
	/// </summary>
	public static class DateTimeExtensions
	{
		public const long UnixEpoch = 621355968000000000L;
		private static readonly DateTime UnixEpochDateTime = new DateTime(UnixEpoch);

		public static long ToUnixTime(this DateTime dateTime)
		{
			var epoch = (dateTime.ToUniversalTime().Ticks - UnixEpoch) / TimeSpan.TicksPerSecond;
			return epoch;
		}

		public static DateTime FromUnixTime(this double unixTime)
		{
			return UnixEpochDateTime + TimeSpan.FromSeconds(unixTime);
		}

		public static DateTime RoundToSecond(this DateTime dateTime)
		{
			return new DateTime(((dateTime.Ticks) / 10000000) * 10000000);
		}

		public static string ToShortestXsdDateTimeString(this DateTime dateTime)
		{
			return DateTimeSerializer.ToShortestXsdDateTimeString(dateTime);
		}

		public static DateTime FromShortestXsdDateTimeString(this string xsdDateTime)
		{
			return DateTimeSerializer.ParseShortestXsdDateTime(xsdDateTime);
		}
	}
}