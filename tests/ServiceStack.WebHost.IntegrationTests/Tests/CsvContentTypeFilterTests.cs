using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;
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
            RestsTestBase.GetWebResponse(HttpMethods.Post, ServiceClientBaseUri + "reset-movies", MimeTypes.Xml, 0);
        }

        [Test]
        public async Task Can_download_movies_in_Csv()
        {
            var client = new CsvServiceClient(ServiceClientBaseUri);

            var response = await client.GetAsync<MoviesResponse>(new Movies());

            Assert.That(response, Is.Not.Null, "No response received");
        }

        [Test]
        public void Can_download_CSV_movies_using_csv_reply_endpoint()
        {
            var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "csv/reply/Movies");

            var res = req.GetResponse();
            Assert.That(res.ContentType, Is.EqualTo(MimeTypes.Csv));
            Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Movies.csv"));

            var csvRows = res.ReadLines().ToList();

            const int headerRowCount = 1;
            Assert.That(csvRows, Has.Count.EqualTo(headerRowCount + ResetMoviesService.Top5Movies.Count));
            //Console.WriteLine(csvRows.Join("\n"));
        }

        [Test]
        public void Can_download_CSV_movies_using_csv_reply_Path_and_alternate_XML_Accept_Header()
        {
            var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "csv/reply/Movies");
            req.Accept = "application/xml";

            var res = req.GetResponse();
            Assert.That(res.ContentType, Is.EqualTo(MimeTypes.Csv));
            Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Movies.csv"));

            var csvRows = res.ReadLines().ToList();

            Assert.That(csvRows, Has.Count.EqualTo(HeaderRowCount + ResetMoviesService.Top5Movies.Count));
            Console.WriteLine(csvRows.Join("\n"));
        }

        [Test]
        public void Can_download_CSV_movies_using_csv_Accept_and_RestPath()
        {
            var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "movies");
            req.Accept = MimeTypes.Csv;

            var res = req.GetResponse();
            Assert.That(res.ContentType, Is.EqualTo(MimeTypes.Csv));
            Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Movies.csv"));

            var csvRows = res.ReadLines().ToList();

            Assert.That(csvRows, Has.Count.EqualTo(HeaderRowCount + ResetMoviesService.Top5Movies.Count));
            //Console.WriteLine(csvRows.Join("\n"));
        }

        [Test]
        public void Can_download_CSV_Hello_using_csv_reply_endpoint()
        {
            var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "csv/reply/Hello?Name=World!");

            var res = req.GetResponse();
            Assert.That(res.ContentType, Is.EqualTo(MimeTypes.Csv));
            Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Hello.csv"));

            var csv = res.ReadToEnd();
            var lf = Environment.NewLine;
            Assert.That(csv, Is.EqualTo("Result{0}\"Hello, World!\"{0}".Fmt(lf)));

            Console.WriteLine(csv);
        }

        [Test]
        public void Can_download_CSV_Hello_using_csv_Accept_and_RestPath()
        {
            var req = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "hello/World!");
            req.Accept = MimeTypes.Csv;

            var res = req.GetResponse();
            Assert.That(res.ContentType, Is.EqualTo(MimeTypes.Csv));
            Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Hello.csv"));

            var csv = res.ReadToEnd();
            var lf = Environment.NewLine;
            Assert.That(csv, Is.EqualTo("Result{0}\"Hello, World!\"{0}".Fmt(lf)));

            Console.WriteLine(csv);
        }

    }
}
