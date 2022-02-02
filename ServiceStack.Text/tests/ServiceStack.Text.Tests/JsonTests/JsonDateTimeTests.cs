using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Text.Common;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text.Tests.JsonTests
{
    public class JsonDateTimeTests
    {
        private string _localTimezoneOffset;

        [SetUp]
        public void SetUp()
        {
            JsConfig.Reset();
            _localTimezoneOffset = TimeZoneInfo.Local.BaseUtcOffset.Hours.ToString("00") + TimeZoneInfo.Local.BaseUtcOffset.Minutes.ToString("00");
        }

        #region TimestampOffset Tests
        [Test]
        public void When_using_TimestampOffset_and_serializing_as_Utc_It_should_deserialize_as_Utc()
        {
            JsConfig.DateHandler = DateHandler.TimestampOffset;
            var initialDate = new DateTime(2012, 7, 25, 16, 17, 00, DateTimeKind.Utc);
            var json = JsonSerializer.SerializeToString(initialDate); //"2012-07-25T16:17:00.0000000Z"

            var deserializedDate = JsonSerializer.DeserializeFromString<DateTime>(json);

            Assert.AreEqual(DateTimeKind.Utc, deserializedDate.Kind);
            Assert.AreEqual(initialDate, deserializedDate);
        }

        [Test]
        public void Can_serialize_json_date_timestampOffset_utc()
        {
            JsConfig.DateHandler = DateHandler.TimestampOffset;

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Utc);
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            Assert.That(ssJson, Is.EqualTo(@"""\/Date(785635200000)\/"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_timestampOffset_local()
        {
            JsConfig.DateHandler = DateHandler.TimestampOffset;

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Local);
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            var offsetSpan = TimeZoneInfo.Local.GetUtcOffset(dateTime);
            var ticks = 785635200000 - offsetSpan.TotalMilliseconds;
            var offset = offsetSpan.ToTimeOffsetString();

            Assert.That(ssJson, Is.EqualTo(@"""\/Date(" + ticks + offset + @")\/"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_timestampOffset_unspecified()
        {
            JsConfig.DateHandler = DateHandler.TimestampOffset;

            // Unspecified time emits '-0000' offset and treated as local time when parsed

            var dateTime1 = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Unspecified);
            var ssJson1 = JsonSerializer.SerializeToString(dateTime1);

            var offsetSpan = TimeZoneInfo.Local.GetUtcOffset(dateTime1);
            var ticks = 785635200000 - offsetSpan.TotalMilliseconds;

            Assert.That(ssJson1, Is.EqualTo(@"""\/Date(" + ticks + @"-0000)\/"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_timestampOffset_withoutOffset_asUtc()
        {
            JsConfig.DateHandler = DateHandler.TimestampOffset;

            const string json = @"""\/Date(785635200000)\/""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(json);

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Utc);
            Assert.That(fromJson, Is.EqualTo(dateTime));
            Assert.That(fromJson.Kind, Is.EqualTo(dateTime.Kind));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_timestampOffset_withOffset_as_Local()
        {
            JsConfig.DateHandler = DateHandler.TimestampOffset;

            const string json = @"""\/Date(785660400000-0700)\/""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(json);

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Local);
            Assert.That(fromJson, Is.EqualTo(dateTime));
            Assert.That(fromJson.Kind, Is.EqualTo(dateTime.Kind));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_timestampOffset_withZeroOffset_as_Local()
        {
            JsConfig.DateHandler = DateHandler.TimestampOffset;

            const string json = @"""\/Date(785635200000+0000)\/""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(json);

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Local);
            Assert.That(fromJson, Is.EqualTo(dateTime));
            Assert.That(fromJson.Kind, Is.EqualTo(dateTime.Kind));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_timestampOffset_unspecified_assume_utc()
        {
            JsConfig.DateHandler = DateHandler.TimestampOffset;
            JsConfig.AssumeUtc = true;

            var dateTime = DateTime.Parse("2013-06-14 19:43:37.663");
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            Assert.That(ssJson, Is.EqualTo(@"""\/Date(1371239017663)\/"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_timestampOffset_unspecified_appendUtcOffset()
        {
            JsConfig.DateHandler = DateHandler.TimestampOffset;
            JsConfig.AppendUtcOffset = true;

            var dateTime = DateTime.SpecifyKind(DateTime.Parse("2013-06-14 19:43:37.663"), DateTimeKind.Utc);
            var ssJson = JsonSerializer.SerializeToString(dateTime);
            Assert.That(ssJson, Is.EqualTo(@"""\/Date(1371239017663+0000)\/"""));

            JsConfig.Reset();
        }

        [Test]
        public void ParseShortestXsdDateTime_TwoDateTimesWithDifferentPrecision_ReturnsSameParsedDateTime()
        {
            JsConfig.AssumeUtc = true;
            JsConfig.AlwaysUseUtc = true;

            const string noDecimalPoint = "1979-05-09T00:00:01Z";
            const string twoDecimalPoints = "1979-05-09T00:00:01.00Z";

            var dateTimeWithNoDecimalPoint = DateTimeSerializer.ParseShortestXsdDateTime(noDecimalPoint);
            var dateTimeWithTwoDecimalPoints = DateTimeSerializer.ParseShortestXsdDateTime(twoDecimalPoints);

            Assert.That(dateTimeWithNoDecimalPoint, Is.EqualTo(dateTimeWithTwoDecimalPoints));

            JsConfig.Reset();
        }

        #endregion

        #region TimeSpan Tests
        [Test]
        public void JsonSerializerReturnsTimeSpanAsString()
        {
            Assert.That(JsonSerializer.SerializeToString(new TimeSpan()), Is.EqualTo("\"PT0S\""));
            Assert.That(JsonSerializer.SerializeToString(new TimeSpan(1)), Is.EqualTo("\"PT0.0000001S\""));
        }

        [Test]
        public void JsonDeserializerReturnsTimeSpanFromString()
        {
            Assert.That(JsonSerializer.DeserializeFromString<TimeSpan>("\"PT0S\""), Is.EqualTo(TimeSpan.Zero));
            Assert.That(JsonSerializer.DeserializeFromString<TimeSpan>("\"PT0.0000001S\""), Is.EqualTo(new TimeSpan(1)));
        }
        #endregion

        #region DCJS Compatibility Tests
        [Test]
        public void Can_serialize_json_date_dcjsCompatible_utc()
        {
            JsConfig.DateHandler = DateHandler.DCJSCompatible;

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Utc);
            var ssJson = JsonSerializer.SerializeToString(dateTime);
            var bclJson = @"""\/Date(785635200000)\/"""; //BclJsonDataContractSerializer.Instance.Parse(dateTime);

            Assert.That(ssJson, Is.EqualTo(bclJson));
            JsConfig.Reset();
        }

#if !__MonoCS__
        [Test]
        public void Can_serialize_json_date_dcjsCompatible_local()
        {
            JsConfig.DateHandler = DateHandler.DCJSCompatible;

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Local);
            var ssJson = JsonSerializer.SerializeToString(dateTime);
            var bclJson = BclJsonDataContractSerializer.Instance.Parse(dateTime);

            Assert.That(ssJson, Is.EqualTo(bclJson));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_dcjsCompatible_unspecified()
        {
            JsConfig.DateHandler = DateHandler.DCJSCompatible;

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Unspecified);
            var ssJson = JsonSerializer.SerializeToString(dateTime);
            var bclJson = BclJsonDataContractSerializer.Instance.Parse(dateTime);

            Assert.That(ssJson, Is.EqualTo(bclJson));
            JsConfig.Reset();
        }
#endif

#if !IOS
        [Test]
        public void Can_deserialize_json_date_dcjsCompatible_utc()
        {
            JsConfig.DateHandler = DateHandler.DCJSCompatible;

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Utc);
            var ssJson = JsonSerializer.SerializeToString(dateTime);
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(ssJson);
            var fromBclJson = BclJsonDataContractDeserializer.Instance.Parse<DateTime>(ssJson);

            Assert.That(fromJson, Is.EqualTo(fromBclJson));
            Assert.That(fromJson.Kind, Is.EqualTo(DateTimeKind.Utc)); // fromBclJson.Kind
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_dcjsCompatible_local()
        {
            JsConfig.DateHandler = DateHandler.DCJSCompatible;

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Local);
            var ssJson = JsonSerializer.SerializeToString(dateTime);
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(ssJson);
            var fromBclJson = BclJsonDataContractDeserializer.Instance.Parse<DateTime>(ssJson);

            Assert.That(fromJson, Is.EqualTo(fromBclJson));
            Assert.That(fromJson.Kind, Is.EqualTo(DateTimeKind.Local)); // fromBclJson.Kind
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_dcjsCompatible_unspecified()
        {
            JsConfig.DateHandler = DateHandler.DCJSCompatible;

            var dateTime = new DateTime(1994, 11, 24, 0, 0, 0, DateTimeKind.Unspecified);
            var ssJson = JsonSerializer.SerializeToString(dateTime);
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(ssJson);
            var fromBclJson = BclJsonDataContractDeserializer.Instance.Parse<DateTime>(ssJson);

            Assert.That(fromJson, Is.EqualTo(fromBclJson));
            Assert.That(fromJson.Kind, Is.EqualTo(DateTimeKind.Local)); // fromBclJson.Kind
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_dcjsCompatible_unspecified_assume_utc()
        {
            JsConfig.DateHandler = DateHandler.DCJSCompatible;
            JsConfig.AssumeUtc = true;

            var dateTime = DateTime.Parse("2013-06-14 19:43:37.663");
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            Assert.That(ssJson, Is.EqualTo(@"""\/Date(1371239017663)\/"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_dcjsCompatible_unspecified_appendUtcOffset()
        {
            JsConfig.DateHandler = DateHandler.DCJSCompatible;
            JsConfig.AppendUtcOffset = true;

            var dateTime = DateTime.SpecifyKind(DateTime.Parse("2013-06-14 19:43:37.663"), DateTimeKind.Utc);
            var ssJson = JsonSerializer.SerializeToString(dateTime);
            Assert.That(ssJson, Is.EqualTo(@"""\/Date(1371239017663)\/"""));

            JsConfig.Reset();
        }
#endif
        #endregion

        #region ISO-8601 Tests
        [Test]
        public void When_using_ISO8601_and_serializing_as_Utc_It_should_deserialize_as_Utc()
        {
            JsConfig.AlwaysUseUtc = true;
            JsConfig.DateHandler = DateHandler.ISO8601;
            var initialDate = new DateTime(2012, 7, 25, 16, 17, 00, DateTimeKind.Utc);
            var json = JsonSerializer.SerializeToString(initialDate); //"2012-07-25T16:17:00.0000000Z"

            var deserializedDate = JsonSerializer.DeserializeFromString<DateTime>(json);

            Assert.AreEqual(DateTimeKind.Utc, deserializedDate.Kind);
            Assert.AreEqual(initialDate, deserializedDate);
        }

        [Test]
        public void ISO8601_assumeUtc_serialize_datetime_is_the_same()
        {
            JsConfig.AssumeUtc = true;
            JsConfig.DateHandler = DateHandler.ISO8601;
            var initialDate = new DateTime(2012, 7, 25, 16, 17, 00, DateTimeKind.Unspecified);
            var writers = new
            {
                jsv = new System.IO.StringWriter(new System.Text.StringBuilder()),
                json = new System.IO.StringWriter(new System.Text.StringBuilder())
            };
            new JsvTypeSerializer().WriteDateTime(writers.jsv, initialDate);
            new Json.JsonTypeSerializer().WriteDateTime(writers.json, initialDate);
            var results = new
            {
                jsv = DateTime.SpecifyKind(DateTime.Parse(writers.jsv.ToString()), DateTimeKind.Utc),
                json = DateTime.SpecifyKind(DateTime.Parse(writers.json.ToString().Replace("\"", "")), DateTimeKind.Utc)
            };
            Assert.AreEqual(results.jsv, results.json);
        }

        [Test]
        public void Can_serialize_json_date_iso8601_utc()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            var dateTime = new DateTime(1994, 11, 24, 12, 34, 56, DateTimeKind.Utc);
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            Assert.That(ssJson, Is.EqualTo(@"""1994-11-24T12:34:56.0000000Z"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_iso8601_local()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            var dateTime = new DateTime(1994, 11, 24, 12, 34, 56, DateTimeKind.Local);
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            var offsetSpan = TimeZoneInfo.Local.GetUtcOffset(dateTime);
            var offset = offsetSpan.ToTimeOffsetString(":");

            Assert.That(ssJson, Is.EqualTo(@"""1994-11-24T12:34:56.0000000" + offset + @""""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_iso8601_unspecified()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            var dateTime = new DateTime(1994, 11, 24, 12, 34, 56, DateTimeKind.Unspecified);
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            Assert.That(ssJson, Is.EqualTo(@"""1994-11-24T12:34:56.0000000"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_iso8601_withZOffset_asUtc_alwaysUseUtc_true()
        {
            JsConfig.AlwaysUseUtc = true;
            JsConfig.DateHandler = DateHandler.ISO8601;

            const string json = @"""1994-11-24T12:34:56Z""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(json);

            var dateTime = new DateTime(1994, 11, 24, 12, 34, 56, DateTimeKind.Utc);
            Assert.That(fromJson, Is.EqualTo(dateTime));
            Assert.That(fromJson.Kind, Is.EqualTo(dateTime.Kind));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_iso8601_withZOffset_asUtc_skipDateTimeConversion_true()
        {
            JsConfig.SkipDateTimeConversion = true;
            JsConfig.DateHandler = DateHandler.ISO8601;

            const string json = "\"1994-11-24T12:34:56Z\"";
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(json);

            var dateTime = new DateTime(1994, 11, 24, 12, 34, 56, DateTimeKind.Utc);
            Assert.That(fromJson, Is.EqualTo(dateTime));
            Assert.That(fromJson.Kind, Is.EqualTo(dateTime.Kind));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_iso8601_with_skipDateTimeConversion_true()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.SkipDateTimeConversion = true;
            string serilizedResult;
            Utils.DateTimeISO8601Tests.TestObject deserilizedResult;

            var testObject = new Utils.DateTimeISO8601Tests.TestObject
            {
                Date = new DateTime(2013, 1, 1, 0, 0, 1, DateTimeKind.Utc)
            };
            serilizedResult = JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject);
            deserilizedResult = JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(serilizedResult);
            Assert.AreEqual(deserilizedResult.Date, testObject.Date);
            Assert.AreEqual(DateTimeKind.Utc, deserilizedResult.Date.Kind);

            using (JsConfig.With(new Config { SkipDateTimeConversion = false }))
            {
                Assert.AreEqual(DateTimeKind.Local, JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject)).Date.Kind);
            }

            testObject = new Utils.DateTimeISO8601Tests.TestObject
            {
                Date = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(2)
            };
            serilizedResult = JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject);
            deserilizedResult = JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(serilizedResult);
            Assert.AreEqual(deserilizedResult.Date, testObject.Date);
            Assert.AreEqual(DateTimeKind.Utc, deserilizedResult.Date.Kind);

            using (JsConfig.With(new Config { SkipDateTimeConversion = false }))
            {
                Assert.AreEqual(DateTimeKind.Local, JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject)).Date.Kind);
            }
            using (JsConfig.With(new Config { AlwaysUseUtc = true, SkipDateTimeConversion = false }))
            {
                Assert.AreEqual(DateTimeKind.Utc, JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject)).Date.Kind);
            }

            //make sure it still keeps local local
            testObject = new Utils.DateTimeISO8601Tests.TestObject
            {
                Date = new DateTime(2013, 1, 2, 0, 2, 0, DateTimeKind.Local).AddMilliseconds(2)
            };
            serilizedResult = JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject);
            deserilizedResult = JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(serilizedResult);
            Assert.AreEqual(deserilizedResult.Date, testObject.Date);
            Assert.AreEqual(DateTimeKind.Local, deserilizedResult.Date.Kind);

            using (JsConfig.With(new Config { AlwaysUseUtc = true }))
            {
                Assert.AreEqual(DateTimeKind.Utc, JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject)).Date.Kind);
            }
            using (JsConfig.With(new Config { AlwaysUseUtc = true, SkipDateTimeConversion = false }))
            {
                Assert.AreEqual(DateTimeKind.Utc, JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject)).Date.Kind);
            }


            testObject = new Utils.DateTimeISO8601Tests.TestObject
            {
                Date = new DateTime(2013, 1, 2, 0, 2, 0, DateTimeKind.Unspecified).AddMilliseconds(2)
            };
            serilizedResult = JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject);
            deserilizedResult = JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(serilizedResult);
            Assert.AreEqual(deserilizedResult.Date, testObject.Date);
            Assert.AreEqual(DateTimeKind.Unspecified, deserilizedResult.Date.Kind);

            using (JsConfig.With(new Config { AlwaysUseUtc = true }))
            {
                Assert.AreEqual(DateTimeKind.Unspecified, JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject)).Date.Kind);
            }
            using (JsConfig.With(new Config { AlwaysUseUtc = true, SkipDateTimeConversion = false }))
            {
                Assert.AreEqual(DateTimeKind.Utc, JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject)).Date.Kind);
            }

            using (JsConfig.With(new Config { SkipDateTimeConversion = false }))
            {
                serilizedResult = JsonSerializer.SerializeToString<Utils.DateTimeISO8601Tests.TestObject>(testObject);
                deserilizedResult = JsonSerializer.DeserializeFromString<Utils.DateTimeISO8601Tests.TestObject>(serilizedResult);
                Assert.AreEqual(DateTimeKind.Local, deserilizedResult.Date.Kind);
            }
            JsConfig.Reset();
        }



        [Test]
        public void Can_deserialize_json_date_iso8601_withoutOffset_as_Unspecified()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            const string json = @"""1994-11-24T12:34:56""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(json);

            var dateTime = new DateTime(1994, 11, 24, 12, 34, 56, DateTimeKind.Unspecified);
            Assert.That(fromJson, Is.EqualTo(dateTime));
            Assert.That(fromJson.Kind, Is.EqualTo(dateTime.Kind));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_iso8601_withOffset_asLocal()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            var dateTime = new DateTime(1994, 11, 24, 12, 34, 56, DateTimeKind.Local);
            var offset = TimeZoneInfo.Local.GetUtcOffset(dateTime).ToTimeOffsetString(":");

            var json = @"""1994-11-24T12:34:56" + offset + @"""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(json);


            Assert.That(fromJson, Is.EqualTo(dateTime));
            Assert.That(fromJson.Kind, Is.EqualTo(dateTime.Kind));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_iso8601_unspecified_assume_utc()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.AssumeUtc = true;

            var dateTime = DateTime.Parse("2013-06-14 19:43:37.663");
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            Assert.That(ssJson, Is.EqualTo(@"""2013-06-14T19:43:37.6630000Z"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_iso8601_unspecified_appendUtcOffset()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.AppendUtcOffset = true;

            var dateTime = DateTime.SpecifyKind(DateTime.Parse("2013-06-14 19:43:37.663"), DateTimeKind.Utc);
            var ssJson = JsonSerializer.SerializeToString(dateTime);
            Assert.That(ssJson, Is.EqualTo(@"""2013-06-14T19:43:37.6630000Z"""));

            JsConfig.Reset();
        }

        #endregion

        #region ISO-8601 TimeStampOffset Tests
        [Test]
        public void Can_serialize_json_datetimeoffset_iso8601_utc()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            var dateTimeOffset = new DateTimeOffset(1994, 11, 24, 12, 34, 56, TimeSpan.Zero);
            var ssJson = JsonSerializer.SerializeToString(dateTimeOffset);

            Assert.That(ssJson, Is.EqualTo(@"""1994-11-24T12:34:56.0000000+00:00"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_datetimeoffset_iso8601_specified()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            var dateTimeOffset = new DateTimeOffset(1994, 11, 24, 12, 34, 56, TimeSpan.FromHours(-7));
            var ssJson = JsonSerializer.SerializeToString(dateTimeOffset);

            Assert.That(ssJson, Is.EqualTo(@"""1994-11-24T12:34:56.0000000-07:00"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_datetimeoffset_iso8601_withZOffset_asUtc()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            const string json = @"""1994-11-24T12:34:56Z""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTimeOffset>(json);

            var dateTimeOffset = new DateTimeOffset(1994, 11, 24, 12, 34, 56, TimeSpan.Zero);
            Assert.That(fromJson, Is.EqualTo(dateTimeOffset));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_datetimeoffset_iso8601_withoutOffset_asUtc()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            const string json = @"""1994-11-24T12:34:56""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTimeOffset>(json);

            var dateTimeOffset = new DateTimeOffset(1994, 11, 24, 12, 34, 56, TimeSpan.Zero);
            Assert.That(fromJson, Is.EqualTo(dateTimeOffset));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_datetimeoffset_iso8601_withOffset_asSpecified()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;

            var dateTimeOffset = new DateTimeOffset(1994, 11, 24, 12, 34, 56, TimeSpan.FromHours(-7));

            const string json = @"""1994-11-24T12:34:56-07:00""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTimeOffset>(json);

            Assert.That(fromJson, Is.EqualTo(dateTimeOffset));
            JsConfig.Reset();
        }
        #endregion

        #region RFC1123 Tests
        [Test]
        public void When_using_rfc1123_and_serializing_as_Utc_It_should_deserialize_as_Utc()
        {
            JsConfig.AlwaysUseUtc = true;
            JsConfig.DateHandler = DateHandler.RFC1123;
            var initialDate = new DateTime(2012, 7, 25, 16, 17, 00, DateTimeKind.Utc);
            var json = JsonSerializer.SerializeToString(initialDate);

            var deserializedDate = JsonSerializer.DeserializeFromString<DateTime>(json);

            Assert.AreEqual(DateTimeKind.Utc, deserializedDate.Kind);
            Assert.AreEqual(initialDate, deserializedDate);
        }

        [Test]
        public void Can_serialize_json_date_rfc1123_utc()
        {
            JsConfig.DateHandler = DateHandler.RFC1123;

            var dateTime = new DateTime(1994, 11, 24, 12, 34, 56, DateTimeKind.Utc);
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            Assert.That(ssJson, Is.EqualTo(@"""Thu, 24 Nov 1994 12:34:56 GMT"""));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_rfc1123_local()
        {
            JsConfig.DateHandler = DateHandler.RFC1123;

            var dateTime = new DateTime(1994, 11, 24, 12, 34, 56, DateTimeKind.Local);
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            var dateTimeUtc = dateTime.ToUniversalTime();
            var ssJsonUtc = JsonSerializer.SerializeToString(dateTimeUtc);

            Assert.That(ssJson, Is.EqualTo(ssJsonUtc)); //Convert to UTC on wire
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_json_date_rfc1123_unspecified()
        {
            JsConfig.DateHandler = DateHandler.RFC1123;

            var dateTime = new DateTime(1994, 11, 24, 12, 34, 56, DateTimeKind.Unspecified);
            var ssJson = JsonSerializer.SerializeToString(dateTime);

            var dateTimeUtc = dateTime.ToUniversalTime();
            var ssJsonUtc = JsonSerializer.SerializeToString(dateTimeUtc);

            Assert.That(ssJson, Is.EqualTo(ssJsonUtc)); //Convert to UTC on wire
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_rfc1123_local()
        {
            JsConfig.DateHandler = DateHandler.RFC1123;

            const string json = @"""Tue, 12 Nov 2013 14:32:07 GMT""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(json);

            var dateTime = new DateTime(2013, 11, 12, 14, 32, 07, DateTimeKind.Utc).ToLocalTime();
            Assert.That(fromJson, Is.EqualTo(dateTime));
            Assert.That(fromJson.Kind, Is.EqualTo(dateTime.Kind));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_json_date_rfc1123_always_utc()
        {
            JsConfig.AlwaysUseUtc = true;
            JsConfig.DateHandler = DateHandler.RFC1123;

            const string json = @"""Tue, 12 Nov 2013 14:32:07 GMT""";
            var fromJson = JsonSerializer.DeserializeFromString<DateTime>(json);

            var dateTime = new DateTime(2013, 11, 12, 14, 32, 07, DateTimeKind.Utc);
            Assert.That(fromJson, Is.EqualTo(dateTime));
            Assert.That(fromJson.Kind, Is.EqualTo(dateTime.Kind));
            JsConfig.Reset();
        }

        #endregion

        #region InteropTests

        [Test]
        public void Can_serialize_TimestampOffset_deserialize_ISO8601()
        {
            var dateTimeOffset = new DateTimeOffset(1997, 11, 24, 12, 34, 56, TimeSpan.FromHours(-10));

            JsConfig.DateHandler = DateHandler.TimestampOffset;
            var json = ServiceStack.Text.Common.DateTimeSerializer.ToWcfJsonDateTimeOffset(dateTimeOffset);

            JsConfig.DateHandler = DateHandler.ISO8601;
            var fromJson = ServiceStack.Text.Common.DateTimeSerializer.ParseDateTimeOffset(json);

            Assert.That(fromJson, Is.EqualTo(dateTimeOffset));
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_ISO8601_deserialize_DCJSCompatible()
        {
            var dateTimeOffset = new DateTimeOffset(1994, 11, 24, 12, 34, 56, TimeSpan.FromHours(-10));

            JsConfig.DateHandler = DateHandler.ISO8601;
            var json = ServiceStack.Text.Common.DateTimeSerializer.ToWcfJsonDateTimeOffset(dateTimeOffset);

            JsConfig.DateHandler = DateHandler.DCJSCompatible;
            var fromJson = ServiceStack.Text.Common.DateTimeSerializer.ParseDateTimeOffset(json);

            // NOTE: DJCS goes to local, so botches offset
            Assert.That(fromJson, Is.EqualTo(dateTimeOffset));
            JsConfig.Reset();
        }

        [Test]
        public void Can_deserialize_null()
        {
            const string json = (string)null;
            var expected = default(DateTimeOffset);
            var fromJson = ServiceStack.Text.Common.DateTimeSerializer.ParseDateTimeOffset(json);
            Assert.That(fromJson, Is.EqualTo(expected));
        }

        #endregion

        public void Test1()
        {
            var tz = TimeZoneInfo.GetSystemTimeZones().ToList().First(t => t.Id == "Afghanistan Standard Time");

            JsConfig.AlwaysUseUtc = true;
            var date = TimeZoneInfo.ConvertTime(new DateTime(2013, 3, 17, 0, 0, 0, DateTimeKind.Utc), tz);
            date.PrintDump();
            date.ToJson().Print();
        }

        [Test]
        public void ToUnixTimeTests()
        {
            var dates = new[]
                {
                    DateTime.Now,
                    DateTime.UtcNow,
                    new DateTime(1979, 5, 9),
                    new DateTime(1972, 3, 24, 0, 0, 0, DateTimeKind.Local),
                    new DateTime(1972, 4, 24),
                    new DateTime(1979, 5, 9, 0, 0, 1),
                    new DateTime(1979, 5, 9, 0, 0, 0, 1),
                    new DateTime(2010, 10, 20, 10, 10, 10, 1),
                    new DateTime(2010, 11, 22, 11, 11, 11, 1),
                    new DateTime(1970, 1, 1, 1, 1, 1, DateTimeKind.Unspecified),
                    new DateTime(1991, 1, 1, 1, 1, 1, DateTimeKind.Unspecified),
                    new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Unspecified),
                    new DateTime(622119282055250000)
                }.ToList();

            dates.ForEach(x => "{0} == {1} :: {2}".Print(x.ToUnixTimeMs(), x.ToUnixTimeMsAlt(), x.ToUnixTimeMs() == x.ToUnixTimeMsAlt()));
            Assert.That(dates.All(x => x.ToUnixTimeMs() == x.ToUnixTimeMsAlt()));
        }

        [Test]
        public void Does_deserialize_UTC_dates_as_UTC()
        {
            JsConfig.AlwaysUseUtc = true;
            var initialDate = new DateTime(2012, 7, 25, 16, 17, 00, DateTimeKind.Utc);

            var json = JsonSerializer.SerializeToString(initialDate); //"2012-07-25T16:17:00.0000000Z"
            var deserializedDate = JsonSerializer.DeserializeFromString<DateTime>(json);

            Assert.AreEqual(DateTimeKind.Utc, deserializedDate.Kind);
            Assert.AreEqual(initialDate, deserializedDate);
        }

        [Test]
        public void Does_deserialize_Local_dates_as_UTC()
        {
            JsConfig.AlwaysUseUtc = true;
            var initialDate = new DateTime(2012, 7, 25, 16, 17, 00, DateTimeKind.Local);

            var json = JsonSerializer.SerializeToString(initialDate); //"2012-07-25T16:17:00.0000000Z"
            var deserializedDate = JsonSerializer.DeserializeFromString<DateTime>(json);

            var expected = initialDate.ToUniversalTime(); //Convert to UTC

            Assert.AreEqual(DateTimeKind.Utc, deserializedDate.Kind);
            Assert.AreEqual(expected, deserializedDate);
        }

        [Test]
        public void Does_serialize_Dates_without_time_as_Local()
        {
            var date = "2000-01-01".FromJson<DateTime>();
            Assert.That(date.Kind, Is.EqualTo(DateTimeKind.Local));
            Assert.That(date.ToString("yyyy-MM-dd"), Is.EqualTo("2000-01-01"));
        }

        [Test]
        public void Does_serialize_Dates_without_time_as_UTC_when_UseUtc()
        {
            JsConfig.AlwaysUseUtc = true;
            var date = "2000-01-01".FromJson<DateTime>();
            Assert.That(date.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(date.ToString("yyyy-MM-dd"), Is.EqualTo("2000-01-01"));
        }

        [Test]
        public void Does_serialize_Dates_without_time_as_UTC_when_AssumeUtc()
        {
            JsConfig.AssumeUtc = true;
            var date = "2000-01-01".FromJson<DateTime>();
            Assert.That(date.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(date.ToString("yyyy-MM-dd"), Is.EqualTo("2000-01-01"));
        }

        [Test]
        public void Does_parse_unspecified_date_with_7sec_fraction_as_UTC()
        {
            JsConfig.AssumeUtc = true;
            JsConfig.AlwaysUseUtc = true;

            // var dateStr = "2014-08-27T14:30:23.123"; // Parsed OK
            var dateStr = "2014-08-27T14:30:23.1230000";
            var dateTime = dateStr.FromJson<DateTime>();

            Assert.That(dateTime.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(dateTime.Hour, Is.EqualTo(14));
            Assert.That(dateTime.Minute, Is.EqualTo(30));
            Assert.That(dateTime.Second, Is.EqualTo(23));
            Assert.That(dateTime.Millisecond, Is.EqualTo(123));

            JsConfig.Reset();
        }
    }
}
