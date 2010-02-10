using System;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text
{
	/// <summary>
	/// A fast, standards-based, serialization-issue free DateTime serailizer.
	/// </summary>
	public static class DateTimeExtensions
	{
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