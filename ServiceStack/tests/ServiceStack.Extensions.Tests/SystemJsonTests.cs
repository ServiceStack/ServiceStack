#if NET8_0_OR_GREATER

using System;
using System.Text.Json;
using NUnit.Framework;
using ServiceStack.Extensions.Tests.Types;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests;

public class SystemJsonTests
{
    JsonSerializerOptions Options = ServiceStackServicesOptions.DefaultSystemJsonOptions();
        
    public SystemJsonTests()
    {
        Options.WriteIndented = true;

        JsConfig.Init(new() {
            SystemJsonCompatible = true,
            TextCase = TextCase.CamelCase,
        });
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => JsConfig.Reset();

    [Test]
    public void SystemJson_Does_serialize_AllTypes()
    {
        Tracer.Instance = new Tracer.ConsoleTracer();
        
        var dto = AllTypes.Create(2);
        // "ServiceStack.Text".Print();
        // dto.ToJson().Print();
        //
        // "\nSystem.Text".Print();
        var json = System.Text.Json.JsonSerializer.Serialize(dto, Options);
        json.Print();
        
        var fromJson = System.Text.Json.JsonSerializer.Deserialize<AllTypes>(json, Options);
        fromJson.AssertEquals(dto);

        fromJson = ServiceStack.Text.JsonSerializer.DeserializeFromString<AllTypes>(json);
        fromJson.AssertEquals(dto);
        
        json = ServiceStack.Text.JsonSerializer.SerializeToString(fromJson);
        fromJson = System.Text.Json.JsonSerializer.Deserialize<AllTypes>(json, Options);
        fromJson.AssertEquals(dto);
    }

    void AssertEquals(QueryBase a, QueryBase b)
    {
        Assert.That(b.Skip, Is.EqualTo(a.Skip));
        Assert.That(b.Take, Is.EqualTo(a.Take));
        Assert.That(b.OrderBy, Is.EqualTo(a.OrderBy));
        Assert.That(b.OrderByDesc, Is.EqualTo(a.OrderByDesc));
        Assert.That(b.Fields, Is.EqualTo(a.Fields));

        Assert.That(a.QueryParams, Is.Null); // [IgnoreDataMember] doesn't serialize
        
        if (b.Meta == null)
            Assert.That(a.Meta, Is.Null);
        else
            Assert.That(a.Meta, Is.EquivalentTo(b.Meta));
    }

    [Test]
    public void SystemJson_Does_serialize_QueryBookings()
    {
        void assertEquals(QueryBookings a, QueryBookings b)
        {
            if (b.Ids == null)
                Assert.That(a.Ids, Is.Null);
            else
                Assert.That(a.Ids, Is.EquivalentTo(b.Ids));
            AssertEquals(a, b);
        }
        
        var dto = new QueryBookings
        {
            Ids = [1, 2, 3],
            Fields = string.Join(",", [nameof(Booking.Id), nameof(Booking.Cost)]),
            Include = "total",
            Skip = 1,
            Take = 10,
            OrderBy = nameof(Booking.Id),
            QueryParams = new(){ [nameof(Booking.Id)] = "A" }, 
            Meta = new(){ [nameof(Booking.Cost)] = "B" },
        };
        
        var json = System.Text.Json.JsonSerializer.Serialize(dto, Options);
        // json.Print();
        
        var fromJson = System.Text.Json.JsonSerializer.Deserialize<QueryBookings>(json, Options);
        assertEquals(fromJson, dto);

        fromJson = ServiceStack.Text.JsonSerializer.DeserializeFromString<QueryBookings>(json);
        assertEquals(fromJson, dto);
        
        json = ServiceStack.Text.JsonSerializer.SerializeToString(fromJson);
        fromJson = System.Text.Json.JsonSerializer.Deserialize<QueryBookings>(json, Options);
        assertEquals(fromJson, dto);
    }

    [Test]
    public void SystemJson_Does_serialize_CreateBooking()
    {
        void assertEquals(CreateBooking a, CreateBooking b)
        {
            Assert.That(a.RoomType, Is.EqualTo(b.RoomType));
            Assert.That(a.RoomNumber, Is.EqualTo(b.RoomNumber));
            Assert.That(a.BookingStartDate, Is.EqualTo(b.BookingStartDate));
            Assert.That(a.RoomType, Is.EqualTo(b.RoomType));
            Assert.That(a.RoomType, Is.EqualTo(b.RoomType));
        }
        
        var dto = new CreateBooking
        {
            RoomType = RoomType.Suite,
            RoomNumber = 200,
            BookingStartDate = new(2024, 01, 01),
            Cost = 300,
            Notes = nameof(Booking.Notes),
            BookingEndDate = new(2024, 01, 01, 1, 1, 1, DateTimeKind.Utc),
        };
        
        var json = System.Text.Json.JsonSerializer.Serialize(dto, Options);
        json.Print();
        
        var fromJson = System.Text.Json.JsonSerializer.Deserialize<CreateBooking>(json, Options);
        assertEquals(fromJson, dto);

        fromJson = ServiceStack.Text.JsonSerializer.DeserializeFromString<CreateBooking>(json);
        assertEquals(fromJson, dto);
        
        json = ServiceStack.Text.JsonSerializer.SerializeToString(fromJson);
        fromJson = System.Text.Json.JsonSerializer.Deserialize<CreateBooking>(json, Options);
        assertEquals(fromJson, dto);
    }
}

#endif
