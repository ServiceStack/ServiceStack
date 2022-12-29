using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class CustomRawSerializerTests
    {
        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            JsConfig.Reset();
        }

        public class RealType
        {
            public string Name { get; set; }
            public byte[] Data { get; set; }
        }

        [Test]
        public void Can_Serialize_TypeProperties_WithCustomFunction()
        {
            var test = new RealType { Name = "Test", Data = new byte[] { 1, 2, 3, 4, 5 } };

            JsConfig<byte[]>.RawSerializeFn = c =>
            {
                var temp = new int[c.Length];
                Array.Copy(c, temp, c.Length);
                return JsonSerializer.SerializeToString(temp);
            };
            var json = JsonSerializer.SerializeToString(test);

            Assert.That(json, Is.EquivalentTo("{\"Name\":\"Test\",\"Data\":[1,2,3,4,5]}"));

            JsConfig<byte[]>.RawSerializeFn = null;
            JsConfig.Reset();
        }

        [Test]
        public void Can_Serialize_bytes_as_Hex()
        {
            JsConfig<byte[]>.SerializeFn = BitConverter.ToString;
            JsConfig<byte[]>.DeSerializeFn = hex =>
            {
                hex = hex.Replace("-", "");
                return Enumerable.Range(0, hex.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                    .ToArray();
            };

            var dto = new RealType
            {
                Name = "Red",
                Data = new byte[] { 255, 0, 0 }
            };

            var json = dto.ToJson();
            Assert.That(json, Does.Contain("FF-00-00"));

            var fromJson = json.FromJson<RealType>();

            Assert.That(fromJson.Data, Is.EquivalentTo(dto.Data));

            JsConfig<byte[]>.SerializeFn = null;
            JsConfig<byte[]>.DeSerializeFn = null;
            JsConfig.Reset();

            json = dto.ToJson();
            json.Print();
            fromJson = json.FromJson<RealType>();
            Assert.That(fromJson.Data, Is.EquivalentTo(dto.Data));
        }

        [Test]
        public void Can_Serialize_AnonymousTypeProperties_WithCustomFunction()
        {
            var test = new { Name = "Test", Data = new byte[] { 1, 2, 3, 4, 5 } };

            // Act: now we set a custom function for byte[]
            JsConfig<byte[]>.RawSerializeFn = c =>
            {
                var temp = new int[c.Length];
                Array.Copy(c, temp, c.Length);
                return JsonSerializer.SerializeToString(temp);
            };
            var json = JsonSerializer.SerializeToString(test);

            // Assert:
            Assert.That(json, Is.EquivalentTo("{\"Name\":\"Test\",\"Data\":[1,2,3,4,5]}"));

            JsConfig<byte[]>.RawSerializeFn = null;
            JsConfig.Reset();
        }

        [Test]
        public void Reset_ShouldClear_JsConfigT_CachedFunctions()
        {
            var test = new { Name = "Test", Data = new byte[] { 1, 2, 3, 4, 5 } };
            JsConfig<byte[]>.RawSerializeFn = c =>
            {
                var temp = new int[c.Length];
                Array.Copy(c, temp, c.Length);
                return JsonSerializer.SerializeToString(temp);
            };
            var json = JsonSerializer.SerializeToString(test);

            Assert.That(json, Is.EquivalentTo("{\"Name\":\"Test\",\"Data\":[1,2,3,4,5]}"));

            JsConfig<byte[]>.RawSerializeFn = null;
            JsConfig.Reset();
            json = JsonSerializer.SerializeToString(test);

            Assert.That(json, Is.EquivalentTo("{\"Name\":\"Test\",\"Data\":\"AQIDBAU=\"}"));
        }

        [Test]
        public void Can_override_Guid_serialization_format()
        {
            var guid = new Guid("ADFA988B-01F6-490D-B65B-63750F869496");

            Assert.That(guid.ToJson().Trim('"'), Is.EqualTo("adfa988b01f6490db65b63750f869496"));
            Assert.That(guid.ToJsv(), Is.EqualTo("adfa988b01f6490db65b63750f869496"));

            JsConfig<Guid>.RawSerializeFn = x => x.ToString();

            Assert.That(guid.ToJson().Trim('"'), Is.EqualTo("adfa988b-01f6-490d-b65b-63750f869496"));
            Assert.That(guid.ToJsv(), Is.EqualTo("adfa988b-01f6-490d-b65b-63750f869496"));
        }

        public class Parent
        {
            public ICar Car { get; set; }
        }

        public interface ICar
        {
            string CarType { get; }
        }

        public class LuxaryCar : ICar
        {
            public string Sunroof { get; set; }

            public string CarType { get { return "Luxary"; } }
        }

        public class CheapCar : ICar
        {
            public bool HasCupHolder { get; set; }

            public string CarType { get { return "Cheap"; } }
        }

        [Test]
        public void Does_call_RawSerializeFn_for_toplevel_types()
        {
            JsConfig<ICar>.RawSerializeFn = SerializeCar;

            var luxaryParent = new Parent() { Car = new LuxaryCar() { Sunroof = "Big" } };
            var cheapParent = new Parent() { Car = new CheapCar() { HasCupHolder = true } };

            // Works when ICar is a child
            var luxaryParentJson = luxaryParent.ToJson();
            var cheapParentJson = cheapParent.ToJson();

            Assert.That(luxaryParentJson, Does.Not.Contain("__type"));
            Assert.That(cheapParentJson, Does.Not.Contain("__type"));

            ICar luxary = new LuxaryCar() { Sunroof = "Big" };
            ICar cheap = new CheapCar() { HasCupHolder = true };

            // ToJson() loses runtime cast of interface type, to keep it we need to specify it on call-site
            var luxaryJson = JsonSerializer.SerializeToString(luxary, typeof(ICar));
            var cheapJson = JsonSerializer.SerializeToString(cheap, typeof(ICar));

            Assert.That(luxaryJson, Does.Not.Contain("__type"));
            Assert.That(cheapJson, Does.Not.Contain("__type"));

            JsConfig.Reset();
        }

        private static string SerializeCar(ICar car)
        {
            var jsonObject = JsonObject.Parse(car.ToJson());

            if (jsonObject.ContainsKey("__type"))
                jsonObject.Remove("__type");

            return jsonObject.ToJson();
        }

        [Test]
        public void Does_call_RawSerializeFn_for_toplevel_concrete_type()
        {
            JsConfig<LuxaryCar>.RawSerializeFn = c => "{\"foo\":1}";

            ICar luxary = new LuxaryCar { Sunroof = "Big" };

            var luxaryJson = luxary.ToJson();

            Assert.That(luxaryJson, Does.Contain("foo"));

            JsConfig.Reset();
        }

        [Test]
        public void Can_call_different_nested_types_custom_serializers()
        {
            JsConfig<InnerType>.SerializeFn = o => InnerType.Serialize(o);
            JsConfig<InnerType>.DeSerializeFn = str => InnerType.Deserialize(str);
            JsConfig<OuterType>.RawSerializeFn = d => JsonSerializer.SerializeToString(d.P1);
            JsConfig<OuterType>.RawDeserializeFn = str =>
            {
                var d = str.FromJson<InnerType>();
                return new OuterType
                {
                    P1 = d
                };
            };

            var t = new InnerType { A = "Hello", B = "World" };

            var data = new OuterType { P1 = t };

            var json = data.ToJson();
            json.Print();

            Assert.That(json, Is.EqualTo(@"""Hello-World"""));

            var outer = json.FromJson<OuterType>();
            Assert.That(outer.P1.A, Is.EqualTo("Hello"));
            Assert.That(outer.P1.B, Is.EqualTo("World"));
        }

        public class Response
        {
            public DateTime DateTime { get; set; }
        }

        [Test]
        public void Can_serialize_custom_DateTime()
        {
            JsConfig<DateTime>.SerializeFn = time =>
            {
                var result = time;
                if (time.Kind == DateTimeKind.Unspecified)
                {
                    result = DateTime.SpecifyKind(result, DateTimeKind.Local);
                }
                return result.ToString(CultureInfo.InvariantCulture);
            };

            var dto = new Response { DateTime = new DateTime(2001, 1, 1, 1, 1, 1) };

            var csv = dto.ToCsv();
            Assert.That(csv, Is.EqualTo("DateTime\r\n01/01/2001 01:01:01\r\n"));

            var json = dto.ToJson();
            Assert.That(json, Is.EqualTo("{\"DateTime\":\"01/01/2001 01:01:01\"}"));

            JsConfig<DateTime>.SerializeFn = null;
            JsConfig.Reset();
        }

        [Test]
        public void Can_serialize_custom_DateTime2()
        {
            JsConfig<DateTime>.SerializeFn = time =>
            {
                var x = new DateTime(time.Ticks, DateTimeKind.Unspecified).ToString("o");
                return x;
            };

            JsConfig<DateTime>.DeSerializeFn = time =>
            {
                var x = DateTime.ParseExact(time, "o", null);
                return x;
            };

            var dateTime = new DateTime(2015, 08, 12, 12, 12, 12, DateTimeKind.Unspecified);

            var json = dateTime.ToJson();
            Assert.That(json, Is.EqualTo("\"2015-08-12T12:12:12.0000000\""));

            var fromJson = json.FromJson<DateTime>();
            Assert.That(fromJson, Is.EqualTo(dateTime));

            var dto = new Response
            {
                DateTime = dateTime,
            };

            json = dto.ToJson();
            Assert.That(json, Is.EqualTo("{\"DateTime\":\"2015-08-12T12:12:12.0000000\"}"));
            Assert.That(json.FromJson<Response>().DateTime, Is.EqualTo(dateTime));

            JsConfig<DateTime>.SerializeFn = null;
            JsConfig<DateTime>.DeSerializeFn = null;
            JsConfig.Reset();
        }
    }

    public class OuterType
    {
        public InnerType P1 { get; set; }
    }

    public class InnerType
    {
        public string A { get; set; }

        public string B { get; set; }

        public static string Serialize(InnerType o)
        {
            return o.A + "-" + o.B;
        }

        public static InnerType Deserialize(string s)
        {
            var p = s.Split('-');
            return new InnerType
            {
                A = p[0],
                B = p[1]
            };
        }
    }

}