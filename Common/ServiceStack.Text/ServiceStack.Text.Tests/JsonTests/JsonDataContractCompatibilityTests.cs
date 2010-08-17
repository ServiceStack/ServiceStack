using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Client;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests.JsonTests
{
	[TestFixture]
	public class JsonDataContractCompatibilityTests
	{

		[Test]
		public void Can_serialize_a_movie()
		{
			const string clientJson = "{\"Id\":\"tt0110912\",\"Title\":\"Pulp Fiction\",\"Rating\":\"8.9\",\"Director\":\"Quentin Tarantino\",\"ReleaseDate\":\"/Date(785635200000+0000)/\",\"TagLine\":\"Girls like me don't make invitations like this to just anyone!\",\"Genres\":[\"Crime\",\"Drama\",\"Thriller\"]}";
			var jsonModel = JsonSerializer.DeserializeFromString<Movie>(clientJson);
			var bclJsonModel = BclJsonDataContractDeserializer.Instance.Parse<Movie>(clientJson);

			var ssJson = JsonSerializer.SerializeToString(jsonModel);
			var wcfJson = BclJsonDataContractSerializer.Instance.Parse(jsonModel);

			Console.WriteLine("{0} == {1}", jsonModel.ReleaseDate, bclJsonModel.ReleaseDate);
			Console.WriteLine("CLIENT {0}\nSS {1}\nBCL {2}", clientJson, ssJson, wcfJson);

			Assert.That(jsonModel, Is.EqualTo(bclJsonModel));
		}

		[Test]
		public void Can_serialize_WcfJsonDate()
		{
			//1994/11/24
			var releaseDate = new DateTime(1994, 11, 24);
			var ssJson = JsonSerializer.SerializeToString(releaseDate);
			var bclJson = BclJsonDataContractSerializer.Instance.Parse(releaseDate);

			//Console.WriteLine("Ticks: {0}", releaseDate.Ticks);
			//Console.WriteLine("UnixEpoch: {0}", DateTimeExtensions.UnixEpoch);
			//Console.WriteLine("TicksPerMs: {0}", TimeSpan.TicksPerSecond / 1000);
			//Console.WriteLine("Ticks - UnixEpoch: {0}", releaseDate.Ticks - DateTimeExtensions.UnixEpoch);
			//Console.WriteLine("{0} == {1}", ssJson, bclJson);

			Assert.That(ssJson, Is.EqualTo(bclJson));
		}

		[Test]
		public void Can_deserialize_json_date()
		{
			var releaseDate = new DateTime(1994, 11, 24);
			var ssJson = JsonSerializer.SerializeToString(releaseDate);
			var fromJson = JsonSerializer.DeserializeFromString<DateTime>(ssJson);

			Assert.That(fromJson, Is.EqualTo(releaseDate));
		}

		[Test]
		public void Can_deserialize_empty_type()
		{
			var ssModel = JsonSerializer.DeserializeFromString<Movie>("{}");
			var ssDynamicModel = JsonSerializer.DeserializeFromString("{}", typeof(Movie));
			var bclModel = BclJsonDataContractDeserializer.Instance.Parse<Movie>("{}");

			var defaultModel = new Movie();
			Assert.That(ssModel, Is.EqualTo(defaultModel));
			Assert.That(ssModel, Is.EqualTo(ssDynamicModel));

			//It's equal except that the BCL resets Lists/Arrays to null which is stupid
			bclModel.Genres = new List<string>();
			Assert.That(ssModel, Is.EqualTo(bclModel));
		}

	}
}