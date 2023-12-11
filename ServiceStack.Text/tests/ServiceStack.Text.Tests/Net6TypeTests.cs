using System;
using NUnit.Framework;

#if NET6_0_OR_GREATER
namespace ServiceStack.Text.Tests;

public class DateOnlyDto
{
    public DateOnly Date { get; set; }

    protected bool Equals(DateOnlyDto other) => Date.Equals(other.Date);
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((DateOnlyDto)obj);
    }
    public override int GetHashCode() => Date.GetHashCode();
}

public class TimeOnlyDto
{
    public TimeOnly Time { get; set; }

    protected bool Equals(TimeOnlyDto other) => Time.Equals(other.Time);
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TimeOnlyDto)obj);
    }
    public override int GetHashCode() => Time.GetHashCode();
}

public class NullableDateOnlyDto
{
    public DateOnly? Date { get; set; }

    protected bool Equals(NullableDateOnlyDto other) => Date.Equals(other.Date);
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((NullableDateOnlyDto)obj);
    }
    public override int GetHashCode() => (Date ?? default).GetHashCode();
}

public class NullableTimeOnlyDto
{
    public TimeOnly? Time { get; set; }

    protected bool Equals(NullableTimeOnlyDto other) => Time.Equals(other.Time);
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((NullableTimeOnlyDto)obj);
    }
    public override int GetHashCode() => (Time ?? default).GetHashCode();
}

public class Net6TypeTests
{
    [Test]
    public void Can_json_serialize_DateOnly()
    {
        var date = new DateOnly(2001, 1, 13);
        var json = date.ToJson();
        Assert.That(json, Is.EqualTo("\"2001-01-13\""));

        var fromJson = json.FromJson<DateOnly>();
        Assert.That(fromJson, Is.EqualTo(date));
        
        var dto = new DateOnlyDto { Date = date };
        json = dto.ToJson();
        Assert.That(json, Is.EqualTo("{\"Date\":\"2001-01-13\"}"));
        var fromJsonDto = json.FromJson<DateOnlyDto>();
        Assert.That(fromJsonDto, Is.EqualTo(dto));

        var nullableDto = new NullableDateOnlyDto { Date = date };
        json = nullableDto.ToJson();
        Assert.That(json, Is.EqualTo("{\"Date\":\"2001-01-13\"}"));
        var fromJsonNullableDto = json.FromJson<NullableDateOnlyDto>();
        Assert.That(fromJsonNullableDto, Is.EqualTo(nullableDto));
    }

    [Test]
    public void Can_jsv_serialize_DateOnly()
    {
        var date = new DateOnly(2001, 1, 13);
        var json = date.ToJsv();
        Assert.That(json, Is.EqualTo("2001-01-13"));

        var fromJson = json.FromJsv<DateOnly>();
        Assert.That(fromJson, Is.EqualTo(date));
        
        var dto = new DateOnlyDto { Date = date };
        json = dto.ToJsv();
        Assert.That(json, Is.EqualTo("{Date:2001-01-13}"));
        var fromJsonDto = json.FromJsv<DateOnlyDto>();
        Assert.That(fromJsonDto, Is.EqualTo(dto));

        var nullableDto = new NullableDateOnlyDto { Date = date };
        json = nullableDto.ToJsv();
        Assert.That(json, Is.EqualTo("{Date:2001-01-13}"));
        var fromJsonNullableDto = json.FromJsv<NullableDateOnlyDto>();
        Assert.That(fromJsonNullableDto, Is.EqualTo(nullableDto));
    }

    [Test]
    public void Can_json_serialize_DateOnly_UnixTime()
    {
        using (JsConfig.With(new Config { DateHandler = DateHandler.UnixTime }))
        {
            var date = new DateOnly(2001, 1, 13);
            var json = date.ToJson();
            Assert.That(json, Is.EqualTo("979344000"));

            var fromJson = json.FromJson<DateOnly>();
            Assert.That(fromJson, Is.EqualTo(date));

            var dto = new DateOnlyDto { Date = date };
            json = dto.ToJson();
            Assert.That(json, Is.EqualTo("{\"Date\":979344000}"));

            var nullableDto = new NullableDateOnlyDto { Date = date };
            json = nullableDto.ToJson();
            Assert.That(json, Is.EqualTo("{\"Date\":979344000}"));
        }
    }

    [Test]
    public void Can_jsv_serialize_DateOnly_UnixTime()
    {
        using (JsConfig.With(new Config { DateHandler = DateHandler.UnixTime }))
        {
            var date = new DateOnly(2001, 1, 13);
            var json = date.ToJsv();
            Assert.That(json, Is.EqualTo("979344000"));

            var fromJson = json.FromJsv<DateOnly>();
            Assert.That(fromJson, Is.EqualTo(date));

            var dto = new DateOnlyDto { Date = date };
            json = dto.ToJsv();
            Assert.That(json, Is.EqualTo("{Date:979344000}"));

            var nullableDto = new NullableDateOnlyDto { Date = date };
            json = nullableDto.ToJsv();
            Assert.That(json, Is.EqualTo("{Date:979344000}"));
        }
    }
    
    [Test]
    public void Can_json_serialize_TimeOnly()
    {
        var time = new TimeOnly(13, 13, 13);
        var json = time.ToJson();
        Assert.That(json, Is.EqualTo("\"PT13H13M13S\""));

        var fromJson = json.FromJson<TimeOnly>();
        Assert.That(fromJson, Is.EqualTo(time));
        
        var dto = new TimeOnlyDto { Time = time };
        json = dto.ToJson();
        Assert.That(json, Is.EqualTo("{\"Time\":\"PT13H13M13S\"}"));
        var fromJsonDto = json.FromJson<TimeOnlyDto>();
        Assert.That(fromJsonDto, Is.EqualTo(dto));

        var nullableDto = new NullableTimeOnlyDto { Time = time };
        json = nullableDto.ToJson();
        Assert.That(json, Is.EqualTo("{\"Time\":\"PT13H13M13S\"}"));
        var fromJsonNullableDto = json.FromJson<NullableTimeOnlyDto>();
        Assert.That(fromJsonNullableDto, Is.EqualTo(nullableDto));
    }
    
    [Test]
    public void Can_jsv_serialize_TimeOnly()
    {
        var time = new TimeOnly(13, 13, 13);
        var json = time.ToJsv();
        Assert.That(json, Is.EqualTo("PT13H13M13S"));

        var fromJson = json.FromJsv<TimeOnly>();
        Assert.That(fromJson, Is.EqualTo(time));
        
        var dto = new TimeOnlyDto { Time = time };
        json = dto.ToJsv();
        Assert.That(json, Is.EqualTo("{Time:PT13H13M13S}"));
        var fromJsonDto = json.FromJsv<TimeOnlyDto>();
        Assert.That(fromJsonDto, Is.EqualTo(dto));

        var nullableDto = new NullableTimeOnlyDto { Time = time };
        json = nullableDto.ToJsv();
        Assert.That(json, Is.EqualTo("{Time:PT13H13M13S}"));
        var fromJsonNullableDto = json.FromJsv<NullableTimeOnlyDto>();
        Assert.That(fromJsonNullableDto, Is.EqualTo(nullableDto));
    }

}
#endif