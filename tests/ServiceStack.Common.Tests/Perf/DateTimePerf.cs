using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using NUnit.Framework;
using ServiceStack.Text.Common;

namespace ServiceStack.Common.Tests.Perf
{
    [TestFixture]
    public class DateTimePerf
        : PerfTestBase
    {
        public DateTimePerf()
        {
            this.MultipleIterations = new List<int> { 10000 };
        }


        [Test]
        public void PrintFormats()
        {
            var now = DateTime.Now;
            var nowWithoutTime = new DateTime(now.Date.Ticks);

            Log(now.ToString("d"));
            Log(now.ToString("t"));
            Log(now.ToString("D"));
            Log(now.ToString("T"));
            Log(now.ToString());
            Log(DateTimeSerializer.ToDateTimeString(now));
            Log(DateTimeSerializer.ToShortestXsdDateTimeString(now));

            Log("\n");
            Log(nowWithoutTime.ToString("d"));
            Log(nowWithoutTime.ToString("t"));
            Log(nowWithoutTime.ToString("D"));
            Log(nowWithoutTime.ToString("T"));
            Log(nowWithoutTime.ToString());
            Log(DateTimeSerializer.ToDateTimeString(nowWithoutTime));
            Log(DateTimeSerializer.ToShortestXsdDateTimeString(nowWithoutTime));
        }

        [Test]
        public void Compare_DateTime_Serializtion()
        {
            var now = DateTime.Now;
            var nowWithoutTime = new DateTime(now.Date.Ticks);

            CompareMultipleRuns(
                "now.ToString()", () => now.ToString(),
                "XmlConvert.ToString(now, XmlDateTimeSerializationMode.Utc)", () => XmlConvert.ToString(now, XmlDateTimeSerializationMode.Utc)
                );

            CompareMultipleRuns(
                "now.ToString()", () => now.ToString(),
                "XmlConvert.ToString(now, DateTimeFormat)", () => XmlConvert.ToString(now, DateTimeSerializer.XsdDateTimeFormat)
                );

            CompareMultipleRuns(
                "ToDateTimeString(now)", () => DateTimeSerializer.ToDateTimeString(now),
                "ToDateOrDateTimeString(now)", () => DateTimeSerializer.ToShortestXsdDateTimeString(now)
                );

            CompareMultipleRuns(
                "ToDateTimeString(nowWithoutTime)", () => DateTimeSerializer.ToDateTimeString(nowWithoutTime),
                "ToDateOrDateTimeString(nowWithoutTime)", () => DateTimeSerializer.ToShortestXsdDateTimeString(nowWithoutTime)
                );
        }

        [Test]
        public void Compare_DateTime_DeSerializtion()
        {
            var nowStr = DateTime.Now.ToString();
            var nowXmlStr = XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc);
            var nowXmlExact = XmlConvert.ToString(DateTime.UtcNow, DateTimeSerializer.XsdDateTimeFormat);

            CompareMultipleRuns(
                "DateTime.Parse(now.ToString())", () => DateTime.Parse(nowStr),
                "XmlConvert.ToString", () => XmlConvert.ToDateTime(nowXmlStr, XmlDateTimeSerializationMode.Utc)
                );
#if !NETCORE
            CompareMultipleRuns(
                "DateTime.ParseExact(utcNow, DateTimeFormat, null)", () => DateTime.ParseExact(nowXmlExact, DateTimeSerializer.XsdDateTimeFormat, null),
                "XmlConvert.ToString", () => XmlConvert.ToDateTime(nowXmlExact, DateTimeSerializer.XsdDateTimeFormat)
                );
#endif
        }

        [Test]
        public void Compare_Serialization()
        {
            var now = DateTime.Now;
            CompareMultipleRuns(
                "FromDateTimeString(ToDateTimeString(now))", () => DateTimeSerializer.ParseDateTime(DateTimeSerializer.ToDateTimeString(now)),
                "FromDateOrDateTimeString(ToDateOrDateTimeString(now))", () => DateTimeSerializer.ParseShortestXsdDateTime(DateTimeSerializer.ToShortestXsdDateTimeString(now))
            );

            Assert.That(now, Is.EqualTo(DateTimeSerializer.ParseDateTime(DateTimeSerializer.ToDateTimeString(now))));
            Assert.That(now, Is.EqualTo(DateTimeSerializer.ParseShortestXsdDateTime(DateTimeSerializer.ToShortestXsdDateTimeString(now))));

            var nowWithoutTime = new DateTime(now.Date.Ticks);
            CompareMultipleRuns(
                "FromDateTimeString(ToDateTimeString(nowWithoutTime))", () => DateTimeSerializer.ParseDateTime(DateTimeSerializer.ToDateTimeString(nowWithoutTime)),
                "FromDateOrDateTimeString(ToDateOrDateTimeString(nowWithoutTime))", () => DateTimeSerializer.ParseShortestXsdDateTime(DateTimeSerializer.ToShortestXsdDateTimeString(nowWithoutTime))
            );

            Assert.That(nowWithoutTime, Is.EqualTo(DateTimeSerializer.ParseDateTime(DateTimeSerializer.ToDateTimeString(nowWithoutTime))));
            Assert.That(nowWithoutTime, Is.EqualTo(DateTimeSerializer.ParseShortestXsdDateTime(DateTimeSerializer.ToShortestXsdDateTimeString(nowWithoutTime))));

            Log("OK");
        }

        [Test]
        public void Compare_Parsing()
        {
            const string dateTimeStr = "2010-11-22T11:11:11.001Z";

            CompareMultipleRuns(
                "XmlConvert.ToDateTime()", () => XmlConvert.ToDateTime(dateTimeStr, XmlDateTimeSerializationMode.Utc),
                "DateTime.ParseExact()", () => DateTime.ParseExact(dateTimeStr,
                    DateTimeSerializer.XsdDateTimeFormat3F, null,
                    DateTimeStyles.AdjustToUniversal)
            );
        }

    }


}