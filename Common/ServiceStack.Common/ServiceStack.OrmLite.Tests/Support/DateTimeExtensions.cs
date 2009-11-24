using System;

namespace ServiceStack.OrmLite.Tests.Support
{
	public static class DateTimeExtensions
	{
		public static DateTime RoundToSecond(this DateTime dateTime)
		{
			return new DateTime(((dateTime.Ticks) / 10000000) * 10000000);
		}
	}
}