using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class AnonymousTypes
		: TestBase
	{
		[Test]
		public void Can_serialize_anonymous_types()
		{
			Serialize(new { Id = 1, Name = "Name", IntList = new[] { 1, 2, 3 } }, includeXml: false); // xmlserializer cannot serialize anonymous types.
		}

		[Test]
		public void Can_serialize_anonymous_type_and_read_as_string_Dictionary()
		{
			var json = JsonSerializer.SerializeToString(
				new { Id = 1, Name = "Name", IntList = new[] { 1, 2, 3 } });

			Console.WriteLine("JSON: " + json);

			var map = JsonSerializer.DeserializeFromString<Dictionary<string, string>>(json);

			Console.WriteLine("MAP: " + map.Dump());
		}

		[Test]
		public void Can_reset_JsConfig_after_serialization()
		{
		    var t = new { Name="123"};

	            var json = ServiceStack.Text.JsonSerializer.SerializeToString(t);
        	    JsConfig.Reset();
		}

        public class TestObj
        {
            public string Title1 { get; set; }
            public object Title2 { get; set; }
        }

        [Test]
        public void Escapes_string_in_object_correctly()
        {
            const string expectedValue = @"a\nb";
            string json = string.Format(@"{{""Title1"":""{0}"",""Title2"":""{0}""}}", expectedValue);

            var value = JsonSerializer.DeserializeFromString<TestObj>(json);

            value.PrintDump();

            Assert.That(value.Title1, Is.EqualTo(value.Title2.ToString()));
        }
	}

}