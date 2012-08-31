using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class CsvContentTypeFilterTests
	{
		const int HeaderRowCount = 1;
		private const string ServiceClientBaseUri = Config.ServiceStackBaseUri + "/";

		private static void FailOnAsyncError<T>(T response, Exception ex)
		{
			Assert.Fail(ex.Message);
		}

        [SetUp]
        public void SetUp()
        {
            // make sure that movies db is not modified
            RestsTestBase.GetWebResponse(HttpMethods.Post, ServiceClientBaseUri + "reset-movies", ContentType.Xml, 0);
        }

		[Test]
		[Ignore("Fails because CSV Deserializer is not implemented")]
		public void Can_download_movies_in_Csv()
		{
			var asyncClient = new AsyncServiceClient
			{
				ContentType = ContentType.Csv,
				StreamSerializer = (r, o, s) => CsvSerializer.SerializeToStream(o, s),
				StreamDeserializer = CsvSerializer.DeserializeFromStream,
			};

			MoviesResponse response = null;
			asyncClient.SendAsync<MoviesResponse>(HttpMethods.Get, ServiceClientBaseUri + "/movies", null,
												  r => response = r, FailOnAsyncError);

			Thread.Sleep(1000);

			Assert.That(response, Is.Not.Null, "No response received");
		}

		[Test]
		public void Can_download_CSV_movies_using_csv_syncreply_endpoint()
		{
			var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "csv/syncreply/Movies");

			var res = req.GetResponse();
			Assert.That(res.ContentType, Is.EqualTo(ContentType.Csv));
			Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Movies.csv"));

			var csvRows = new StreamReader(res.GetResponseStream()).ReadLines().ToList();

			const int headerRowCount = 1;
			Assert.That(csvRows, Has.Count.EqualTo(headerRowCount + ResetMoviesService.Top5Movies.Count));
			//Console.WriteLine(csvRows.Join("\n"));
		}

		[Test]
		public void Can_download_CSV_movies_using_csv_SyncReply_Path_and_alternate_XML_Accept_Header()
		{
			var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "csv/syncreply/Movies");
			req.Accept = "application/xml";

			var res = req.GetResponse();
			Assert.That(res.ContentType, Is.EqualTo(ContentType.Csv));
			Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Movies.csv"));

			var csvRows = new StreamReader(res.GetResponseStream()).ReadLines().ToList();

			Assert.That(csvRows, Has.Count.EqualTo(HeaderRowCount + ResetMoviesService.Top5Movies.Count));
			Console.WriteLine(csvRows.Join("\n"));
		}

		[Test]
		public void Can_download_CSV_movies_using_csv_Accept_and_RestPath()
		{
			var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "movies");
			req.Accept = ContentType.Csv;

			var res = req.GetResponse();
			Assert.That(res.ContentType, Is.EqualTo(ContentType.Csv));
			Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Movies.csv"));

			var csvRows = new StreamReader(res.GetResponseStream()).ReadLines().ToList();

			Assert.That(csvRows, Has.Count.EqualTo(HeaderRowCount + ResetMoviesService.Top5Movies.Count));
			//Console.WriteLine(csvRows.Join("\n"));
		}

		[Test]
		public void Can_download_CSV_Hello_using_csv_syncreply_endpoint()
		{
			var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "csv/syncreply/Hello?Name=World!");

			var res = req.GetResponse();
			Assert.That(res.ContentType, Is.EqualTo(ContentType.Csv));
			Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Hello.csv"));

			var csv = new StreamReader(res.GetResponseStream()).ReadToEnd();
			Assert.That(csv, Is.EqualTo("Result\r\n\"Hello, World!\"\r\n"));

			Console.WriteLine(csv);
		}

		[Test]
		public void Can_download_CSV_Hello_using_csv_Accept_and_RestPath()
		{
			var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "hello/World!");
			req.Accept = ContentType.Csv;

			var res = req.GetResponse();
			Assert.That(res.ContentType, Is.EqualTo(ContentType.Csv));
			Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Hello.csv"));

			var csv = new StreamReader(res.GetResponseStream()).ReadToEnd();
			Assert.That(csv, Is.EqualTo("Result\r\n\"Hello, World!\"\r\n"));

			Console.WriteLine(csv);
		}

	}
}