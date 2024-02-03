#if NET8_0_OR_GREATER

using System;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using ServiceStack.Extensions.Tests.Types;
using ServiceStack.Text;

namespace ServiceStack.Extensions.Tests;

public class SystemJsonTests
{
    JsonSerializerOptions Options = TextConfig.CreateSystemJsonOptions();
        
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

    [Test]
    public void SystemJson_Does_serialize_enums()
    {
        void assertEquals(HelloWithEnum a, HelloWithEnum b)
        {
            Assert.That(a.EnumProp, Is.EqualTo(b.EnumProp));
            Assert.That(a.EnumTypeFlags, Is.EqualTo(b.EnumTypeFlags));
            Assert.That(a.EnumWithValues, Is.EqualTo(b.EnumWithValues));
            Assert.That(a.NullableEnumProp, Is.EqualTo(b.NullableEnumProp));
            Assert.That(a.EnumFlags, Is.EqualTo(b.EnumFlags));
            Assert.That(a.EnumAsInt, Is.EqualTo(b.EnumAsInt));
            Assert.That(a.EnumStyle, Is.EqualTo(b.EnumStyle));
            Assert.That(a.EnumStyleMembers, Is.EqualTo(b.EnumStyleMembers));
        }
        
        var dto = new HelloWithEnum
        {
            EnumProp = EnumType.Value2,
            EnumTypeFlags = EnumTypeFlags.Value2 | EnumTypeFlags.Value3,
            EnumWithValues = EnumWithValues.Value2,
            NullableEnumProp = null,
            EnumFlags = EnumFlags.Value2,
            EnumAsInt = EnumAsInt.Value2,
            EnumStyle = EnumStyle.camelUPPER,
            EnumStyleMembers = EnumStyleMembers.Upper,
        };

        var json = System.Text.Json.JsonSerializer.Serialize(dto, Options);
        json.Print();
        
        var fromJson = System.Text.Json.JsonSerializer.Deserialize<HelloWithEnum>(json, Options);
        assertEquals(fromJson, dto);

        fromJson = ServiceStack.Text.JsonSerializer.DeserializeFromString<HelloWithEnum>(json);
        assertEquals(fromJson, dto);
        
        json = ServiceStack.Text.JsonSerializer.SerializeToString(fromJson);
        fromJson = System.Text.Json.JsonSerializer.Deserialize<HelloWithEnum>(json, Options);
        assertEquals(fromJson, dto);
    }

    [Test]
    public void SystemJson_Does_serialize_enum_lists()
    {
        void assertEquals(HelloWithEnumList a, HelloWithEnumList b)
        {
            Assert.That(a.EnumProp, Is.EquivalentTo(b.EnumProp));
            Assert.That(a.EnumWithValues, Is.EquivalentTo(b.EnumWithValues));
            Assert.That(a.NullableEnumProp, Is.Null);
            Assert.That(a.EnumFlags, Is.EquivalentTo(b.EnumFlags));
            Assert.That(a.EnumStyle, Is.EquivalentTo(b.EnumStyle));
        }
        
        var dto = new HelloWithEnumList
        {
            EnumProp = [EnumType.Value2],
            EnumWithValues = [EnumWithValues.Value2],
            NullableEnumProp = null,
            EnumFlags = [EnumFlags.Value2],
            EnumStyle = [EnumStyle.camelUPPER],
        };

        var json = System.Text.Json.JsonSerializer.Serialize(dto, Options);

        var fromJson = System.Text.Json.JsonSerializer.Deserialize<HelloWithEnumList>(json, Options);
        assertEquals(fromJson, dto);
        
        fromJson = ServiceStack.Text.JsonSerializer.DeserializeFromString<HelloWithEnumList>(json);
        assertEquals(fromJson, dto);
        
        json = ServiceStack.Text.JsonSerializer.SerializeToString(fromJson);
        fromJson = System.Text.Json.JsonSerializer.Deserialize<HelloWithEnumList>(json, Options);
        assertEquals(fromJson, dto);
    }

    [Test]
    public void SystemJson_Does_serialize_enum_dictionaries()
    {
        void assertEquals(HelloWithEnumMap a, HelloWithEnumMap b)
        {
            Assert.That(a.EnumProp, Is.EquivalentTo(b.EnumProp));
            Assert.That(a.EnumWithValues, Is.EquivalentTo(b.EnumWithValues));
            Assert.That(a.NullableEnumProp, Is.Null);
            Assert.That(a.EnumFlags, Is.EquivalentTo(b.EnumFlags));
            Assert.That(a.EnumStyle, Is.EquivalentTo(b.EnumStyle));
        }
        
        var dto = new HelloWithEnumMap
        {
            EnumProp = new() { [EnumType.Value2] = EnumType.Value2 },
            EnumWithValues = new() { [EnumWithValues.Value1] = EnumWithValues.Value2 },
            NullableEnumProp = null,
            EnumFlags = new() { [EnumFlags.Value2] = EnumFlags.Value2 },
            EnumStyle = new() { [EnumStyle.camelUPPER] = EnumStyle.camelUPPER },
        };

        var json = System.Text.Json.JsonSerializer.Serialize(dto, Options);
        
        var fromJson = System.Text.Json.JsonSerializer.Deserialize<HelloWithEnumMap>(json, Options);
        assertEquals(fromJson, dto);
        
        fromJson = ServiceStack.Text.JsonSerializer.DeserializeFromString<HelloWithEnumMap>(json);
        assertEquals(fromJson, dto);
        
        json = ServiceStack.Text.JsonSerializer.SerializeToString(fromJson);
        fromJson = System.Text.Json.JsonSerializer.Deserialize<HelloWithEnumMap>(json, Options);
        assertEquals(fromJson, dto);
    }
    
    void AssertEquals(Rockstar a, Rockstar b)
    {
        Assert.That(b.Id, Is.EqualTo(a.Id));
        Assert.That(b.FirstName, Is.EqualTo(a.FirstName));
        Assert.That(b.LastName, Is.EqualTo(a.LastName));
        Assert.That(b.Age, Is.EqualTo(a.Age));
        Assert.That(b.DateOfBirth, Is.EqualTo(a.DateOfBirth));
        Assert.That(b.DateDied, Is.EqualTo(a.DateDied));
        Assert.That(b.LivingStatus, Is.EqualTo(a.LivingStatus));
    }

    [Test]
    public void SystemJson_Does_serialize_Rockstars()
    {
        var dto = AutoQueryAppHost.SeedRockstars;
        var json = System.Text.Json.JsonSerializer.Serialize(dto, Options);
        // json.Print();
        var fromJson = System.Text.Json.JsonSerializer.Deserialize<Rockstar[]>(json, Options);
        
        foreach (var rockstar in dto)
        {
            var fromJsonRockstar = fromJson.First(x => x.Id == rockstar.Id);
            AssertEquals(fromJsonRockstar, rockstar);
        }
    }

    [Test]
    public void SystemJson_Does_serialize_QueryResponseRockstars()
    {
        var dto = new QueryResponse<Rockstar>
        {
            Total = AutoQueryAppHost.SeedRockstars.Length,
            Results = AutoQueryAppHost.SeedRockstars.ToList()
        };
        var json = System.Text.Json.JsonSerializer.Serialize(dto, Options);
        // json.Print();
        var fromJson = System.Text.Json.JsonSerializer.Deserialize<QueryResponse<Rockstar>>(json, Options);
        
        foreach (var rockstar in dto.Results)
        {
            var fromJsonRockstar = fromJson.Results.First(x => x.Id == rockstar.Id);
            AssertEquals(fromJsonRockstar, rockstar);
        }
    }
    
}

#endif
