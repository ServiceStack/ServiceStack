using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Tests;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text.Tests
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
			const string shortDateTimeString = "1979-05-09T00:00:01Z";

			var longDateTime = new DateTime(1979, 5, 9, 0, 0, 0, 1);
			const string longDateTimeString = "1979-05-09T00:00:00.001Z";


			Assert.That(shortDateString, Is.EqualTo(DateTimeSerializer.ToShortestXsdDateTimeString(shortDate)));
			Assert.That(shortDateTimeString, Is.EqualTo(DateTimeSerializer.ToShortestXsdDateTimeString(shortDateTime)));
			Assert.That(longDateTimeString, Is.EqualTo(DateTimeSerializer.ToShortestXsdDateTimeString(longDateTime)));
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

		private void AssertDateIsEqual(DateTime dateTime)
		{
			var shortDateStr = dateTime.ToString(DateTimeSerializer.ShortDateTimeFormat);
			var shortDateTimeStr = dateTime.ToString(DateTimeSerializer.XsdDateTimeFormatSeconds);
			var longDateTimeStr = dateTime.ToString(DateTimeSerializer.XsdDateTimeFormat);
			var shortestDateStr = DateTimeSerializer.ToShortestXsdDateTimeString(dateTime);

			Log("{0} | {1} | {2}  [{3}]",
				shortDateStr, shortDateTimeStr, longDateTimeStr, shortestDateStr);

			var shortDate = DateTimeSerializer.ParseShortestXsdDateTime(shortDateStr);
			var shortDateTime = DateTimeSerializer.ParseShortestXsdDateTime(shortDateTimeStr);
			var longDateTime = DateTimeSerializer.ParseShortestXsdDateTime(longDateTimeStr);

			Assert.That(shortDate, Is.EqualTo(dateTime.Date));
			Assert.That(shortDateTime, Is.EqualTo(new DateTime(
				shortDateTime.Year, shortDateTime.Month, shortDateTime.Day,
				shortDateTime.Hour, shortDateTime.Minute, shortDateTime.Second,
				shortDateTime.Millisecond)));
			Assert.That(longDateTime, Is.EqualTo(dateTime));

			var toDateTime = DateTimeSerializer.ParseShortestXsdDateTime(shortestDateStr);
			Assert.That(toDateTime, Is.EqualTo(dateTime));
		}
	}
}