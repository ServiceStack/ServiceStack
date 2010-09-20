using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class DynamicObjectTests
		: TestBase
	{
		public class UrlStatus
		{
			public int Status { get; set; }
			public string Url { get; set; }
		}

		[Test]
		public void Dictionary_Object_UrlStatus()
		{
			var urlStatus = new UrlStatus
        	{
        		Status = 301,
        		Url = "http://www.ehow.com/how_5615409_create-pdfs-using-bean.html",
        	};

			var map = new Dictionary<string, object>
          	{
          		{"Status","OK"},
          		{"Url","http://www.ehow.com/m/how_5615409_create-pdfs-using-bean.html"},
          		{"Parent Url","http://www.ehow.com/mobilearticle35.xml"},
          		{"Redirect Chai", urlStatus},
          	};

			var json = JsonSerializer.SerializeToString(map);
			var fromJson = JsonSerializer.DeserializeFromString<Dictionary<string, object>>(json);

			Assert.That(fromJson["Status"], Is.EqualTo(map["Status"]));
			Assert.That(fromJson["Url"], Is.EqualTo(map["Url"]));
			Assert.That(fromJson["Parent Url"], Is.EqualTo(map["Parent Url"]));

			var actualStatus = JsonSerializer.DeserializeFromString<UrlStatus>((string)fromJson["Redirect Chai"]);
			Assert.That(actualStatus.Status, Is.EqualTo(urlStatus.Status));
			Assert.That(actualStatus.Url, Is.EqualTo(urlStatus.Url));

			Console.WriteLine("JSON: " + json);
		}

	}
}