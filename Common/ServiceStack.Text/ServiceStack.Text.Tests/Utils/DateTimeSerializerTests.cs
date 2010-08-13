using System;
using NUnit.Framework;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Tests.Utils
{
	[TestFixture]
	public class DateTimeSerializerTests
		: TestBase
	{
		public void PrintFormats(DateTime dateTime)
		{
			Log("dateTime.ToShortDateString(): " + dateTime.ToShortDateString());
			Log("dateTime.ToShortTimeString(): " + dateTime.ToShortTimeString());
			Log("dateTime.ToLongTimeString(): " + dateTime.ToLongTimeString());
			Log("dateTime.ToShortTimeString(): " + dateTime.ToShortTimeString());
			Log("dateTime.ToString(): " + dateTime.ToString());
			Log("DateTimeSerializer.ToShortestXsdDateTimeString(dateTime): " + DateTimeSerializer.ToShortestXsdDateTimeString(dateTime));
			Log("DateTimeSerializer.ToDateTimeString(dateTime): " + DateTimeSerializer.ToDateTimeString(dateTime));
			Log("DateTimeSerializer.ToXsdDateTimeString(dateTime): " + DateTimeSerializer.ToXsdDateTimeString(dateTime));
			Log("\n");
		}

		[Test]
		public void PrintDate()
		{
			PrintFormats(DateTime.Now);
			PrintFormats(DateTime.UtcNow);
			PrintFormats(new DateTime(1979, 5, 9));
			PrintFormats(new DateTime(1979, 5, 9, 0, 0, 1));
			PrintFormats(new DateTime(1979, 5, 9, 0, 0, 0, 1));
			PrintFormats(new DateTime(2010, 10, 20, 10, 10, 10, 1));
			PrintFormats(new DateTime(2010, 11, 22, 11, 11, 11, 1));
		}

		[Test]
		public void ToShortestXsdDateTimeString_works()
		{
			var shortDate = new DateTime(1979, 5, 9);
			const string shortDateString = "1979-05-09";

			var shortDateTime = new DateTime(1979, 5, 9, 0, 0, 1);
			var shortDateTimeString = shortDateTime.Equals(shortDateTime.ToUniversalTime())
              	? "1979-05-09T00:00:01Z"
              	: "1979-05-08T23:00:01Z";

			var longDateTime = new DateTime(1979, 5, 9, 0, 0, 0, 1);
			var longDateTimeString = longDateTime.Equals(longDateTime.ToUniversalTime())
         		? "1979-05-09T00:00:00.001Z"
         		: "1979-05-08T23:00:00.001Z";

			Assert.That(shortDateString, Is.EqualTo(DateTimeSerializer.ToShortestXsdDateTimeString(shortDate)));
			Assert.That(shortDateTimeString, Is.EqualTo(DateTimeSerializer.ToShortestXsdDateTimeString(shortDateTime)));
			Assert.That(longDateTimeString, Is.EqualTo(DateTimeSerializer.ToShortestXsdDateTimeString(longDateTime)));
		}

		[Test][Ignore]
		public void Utc_Local_Equals()
		{
			var now = DateTime.Now;
			var utcNow = now.ToUniversalTime();

			Assert.That(now.Ticks, Is.EqualTo(utcNow.Ticks), "Ticks are different");
			Assert.That(now, Is.EqualTo(utcNow), "DateTimes are different");
		}

		[Test]
		public void ParseShortestXsdDateTime_works()
		{
			AssertDateIsEqual(DateTime.Now);
			AssertDateIsEqual(DateTime.UtcNow);
			AssertDateIsEqual(new DateTime(1979, 5, 9));
			AssertDateIsEqual(new DateTime(1979, 5, 9, 0, 0, 1));
			AssertDateIsEqual(new DateTime(1979, 5, 9, 0, 0, 0, 1));
			AssertDateIsEqual(new DateTime(2010, 10, 20, 10, 10, 10, 1));
			AssertDateIsEqual(new DateTime(2010, 11, 22, 11, 11, 11, 1));
		}

		[Test]
		public void TestSqlServerDateTime()
		{
			var result = TypeSerializer.DeserializeFromString<DateTime>("2010-06-01 21:52:59.280");
			Assert.That(result, Is.Not.Null);
		}

		private void AssertDateIsEqual(DateTime dateTime)
		{
			//Don't test short dates without time to UTC as you lose precision
			var shortDateStr = dateTime.ToString(DateTimeSerializer.ShortDateTimeFormat);
			var shortDateTimeStr = dateTime.ToUniversalTime().ToString(DateTimeSerializer.XsdDateTimeFormatSeconds);
			var longDateTimeStr = dateTime.ToUniversalTime().ToString(DateTimeSerializer.XsdDateTimeFormat);
			var shortestDateStr = DateTimeSerializer.ToShortestXsdDateTimeString(dateTime);

			Log("{0} | {1} | {2}  [{3}]",
			    shortDateStr, shortDateTimeStr, longDateTimeStr, shortestDateStr);

			var shortDate = DateTimeSerializer.ParseShortestXsdDateTime(shortDateStr);
			var shortDateTime = DateTimeSerializer.ParseShortestXsdDateTime(shortDateTimeStr);
			var longDateTime = DateTimeSerializer.ParseShortestXsdDateTime(longDateTimeStr);

			Assert.That(shortDate, Is.EqualTo(dateTime.Date));

			var shortDateTimeUtc = shortDateTime.ToUniversalTime();
			Assert.That(shortDateTimeUtc, Is.EqualTo(
				new DateTime(
					shortDateTimeUtc.Year, shortDateTimeUtc.Month, shortDateTimeUtc.Day,
					shortDateTimeUtc.Hour, shortDateTimeUtc.Minute, shortDateTimeUtc.Second,
					shortDateTimeUtc.Millisecond, DateTimeKind.Utc)));
			
			Assert.That(longDateTime.ToUniversalTime(), Is.EqualTo(dateTime.ToUniversalTime()));

			var toDateTime = DateTimeSerializer.ParseShortestXsdDateTime(shortestDateStr);
			AssertDatesAreEqual(toDateTime, dateTime);

			var unixTime = dateTime.ToUnixTime();
			var fromUnixTime = DateTimeExtensions.FromUnixTime(unixTime);
			AssertDatesAreEqual(fromUnixTime, dateTime);
		}

		private void AssertDatesAreEqual(DateTime toDateTime, DateTime dateTime)
		{
			try
			{
				Assert.That(toDateTime.ToUniversalTime(), Is.EqualTo(dateTime.ToUniversalTime()));
			}
			catch (Exception ex)
			{
				Log("Trouble with DateTime precisions, trying Assert again with rounding to seconds", ex);
				Assert.That(toDateTime.ToUniversalTime().RoundToSecond(), Is.EqualTo(dateTime.ToUniversalTime().RoundToSecond()));
			}
		}
	}
}