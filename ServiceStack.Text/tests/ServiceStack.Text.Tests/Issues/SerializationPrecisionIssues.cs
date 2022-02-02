using System;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.Issues
{
    public class SerializationPrecisionIssues
    {
        public class TimeSpanWrapper
        {
            public TimeSpan TimeSpan { get; set; }
        }

        [Test]
        public void Can_convert_min_TimeSpan()
        {
            var dto = new TimeSpanWrapper {
                TimeSpan = TimeSpan.MinValue,
            };
            var json = JsonSerializer.SerializeToString(dto, typeof(TimeSpanWrapper));
            var fromJson = JsonSerializer.DeserializeFromString<TimeSpanWrapper>(json);
            Assert.That(fromJson.TimeSpan, Is.EqualTo(dto.TimeSpan));
        }

        [Test]
        public void Can_convert_max_TimeSpan()
        {
            var dto = new TimeSpanWrapper {
                TimeSpan = TimeSpan.MaxValue,
            };
            var json = JsonSerializer.SerializeToString(dto, typeof(TimeSpanWrapper));
            var fromJson = JsonSerializer.DeserializeFromString<TimeSpanWrapper>(json);
            Assert.That(fromJson.TimeSpan, Is.EqualTo(dto.TimeSpan));
        }

        class DateTimeOffsetWrapper
        {
            public DateTimeOffset DateTimeOffset { get; set; }
        }
 
        [Test]
        public void Can_convert_min_DateTimeOffset()
        {
            var dto = new DateTimeOffsetWrapper {
                DateTimeOffset = DateTimeOffset.MinValue,
            };
            var json = JsonSerializer.SerializeToString(dto, typeof(DateTimeOffsetWrapper));
            var fromJson = JsonSerializer.DeserializeFromString<DateTimeOffsetWrapper>(json);
            Assert.That(fromJson.DateTimeOffset, Is.EqualTo(dto.DateTimeOffset));
        }
 
        [Test]
        public void Can_convert_max_DateTimeOffset()
        {
            var dto = new DateTimeOffsetWrapper {
                DateTimeOffset = DateTimeOffset.MaxValue,
            };
            var json = JsonSerializer.SerializeToString(dto, typeof(DateTimeOffsetWrapper));
            var fromJson = JsonSerializer.DeserializeFromString<DateTimeOffsetWrapper>(json);
            Assert.That(fromJson.DateTimeOffset, Is.EqualTo(dto.DateTimeOffset));
        }
    }
}