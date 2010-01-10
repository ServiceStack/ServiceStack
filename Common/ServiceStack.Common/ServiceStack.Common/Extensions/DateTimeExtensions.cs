using System;

namespace ServiceStack.Common.Extensions
{
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
	}
}