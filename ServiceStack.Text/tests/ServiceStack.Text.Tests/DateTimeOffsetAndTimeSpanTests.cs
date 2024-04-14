using System;
using NUnit.Framework;
#if NETFRAMEWORK
using ServiceStack.Serialization;
#endif

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class DateTimeOffsetAndTimeSpanTests : TestBase
    {
#if !IOS && NETFRAMEWORK
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            JsonDataContractSerializer.Instance.UseBcl = true;
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            JsonDataContractSerializer.Instance.UseBcl = false;            
        }
#endif

        [Test]
        public void Can_Serializable_DateTimeOffset_Field()
        {
            var model = new SampleModel { Id = 1, Date = new DateTimeOffset(2012, 6, 27, 11, 26, 04, 524, TimeSpan.FromHours(7)) };

            //Behaviour of .NET's BCL classes
            //JsonDataContractSerializer.Instance.SerializeToString(model).Print();
            //DataContractSerializer.Instance.Parse(model).Print();

            var json = JsonSerializer.SerializeToString(model);
            Assert.That(json, Does.Contain("\"TimeSpan\":\"PT0S\""));

            var fromJson = json.FromJson<SampleModel>();

            Assert.That(model.Date, Is.EqualTo(fromJson.Date));
            Assert.That(model.TimeSpan, Is.EqualTo(fromJson.TimeSpan));

            Serialize(fromJson);
        }

        [Test]
        public void Can_serialize_TimeSpan_field()
        {
            var fromDate = new DateTime(2069, 01, 02);
            var toDate = new DateTime(2079, 01, 02);
            var period = toDate - fromDate;

            var model = new SampleModel { Id = 1, TimeSpan = period };
            var json = JsonSerializer.SerializeToString(model);
            Assert.That(json, Does.Contain("\"TimeSpan\":\"P3652D\""));

            //Behaviour of .NET's BCL classes
            //JsonDataContractSerializer.Instance.SerializeToString(model).Print();
            //DataContractSerializer.Instance.Parse(model).Print();

            Serialize(model);
        }

        [Test]
        public void Can_serialize_TimeSpan_field_with_StandardTimeSpanFormat()
        {
            using (JsConfig.With(new Config { TimeSpanHandler = TimeSpanHandler.StandardFormat }))
            {
                var period = TimeSpan.FromSeconds(70);

                var model = new SampleModel { Id = 1, TimeSpan = period };
                var json = JsonSerializer.SerializeToString(model);
                Assert.That(json, Does.Contain("\"TimeSpan\":\"00:01:10\""));
            }
        }

        [Test]
        public void Can_serialize_NullableTimeSpan_field_with_StandardTimeSpanFormat()
        {
            using (JsConfig.With(new Config { TimeSpanHandler = TimeSpanHandler.StandardFormat }))
            {
                var period = TimeSpan.FromSeconds(70);

                var model = new NullableSampleModel { Id = 1, TimeSpan = period };
                var json = JsonSerializer.SerializeToString(model);
                Assert.That(json, Does.Contain("\"TimeSpan\":\"00:01:10\""));
            }
        }

        [Test]
        public void Can_serialize_NullTimeSpan_field_with_StandardTimeSpanFormat()
        {
            using (JsConfig.With(new Config { TimeSpanHandler = TimeSpanHandler.StandardFormat }))
            {
                var model = new NullableSampleModel { Id = 1 };
                var json = JsonSerializer.SerializeToString(model);
                Assert.That(json, Does.Not.Contain("\"TimeSpan\""));
            }
        }

        public class SampleModel
        {
            public int Id { get; set; }

            public DateTimeOffset Date { get; set; }
            public TimeSpan TimeSpan { get; set; }
        }

        public class NullableSampleModel
        {
            public int Id { get; set; }

            public DateTimeOffset Date { get; set; }
            public TimeSpan? TimeSpan { get; set; }
        }
    }
}
