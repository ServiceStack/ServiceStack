using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class CsvContentTypeFilterTests
    {
        const int HeaderRowCount = 1;
        private const string ListeningOn = "http://localhost:1182/";

        ExampleAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        [Explicit("Helps debugging when you need to find out WTF is going on")]
        public void Run_for_30secs()
        {
            Thread.Sleep(30000);
        }

        private static void FailOnAsyncError<T>(T response, Exception ex)
        {
            Assert.Fail(ex.Message);
        }

        [Test]
        public void Can_Serialize_Movies_Dto()
        {
            var csv = CsvSerializer.SerializeToString(ResetMoviesService.Top5Movies);
            var csvRows = csv.Split('\n').Where(x => !x.IsNullOrEmpty()).ToArray();
            Assert.That(csvRows.Length, Is.EqualTo(HeaderRowCount + ResetMoviesService.Top5Movies.Count));
        }

        [Test]
        public void Can_Serialize_MovieResponse_Dto()
        {
            var request = new MovieResponse { Movie = ResetMoviesService.Top5Movies[0] };
            var csv = CsvSerializer.SerializeToString(request);
            var csvRows = csv.Split('\n').Where(x => !x.IsNullOrEmpty()).ToArray();
            Assert.That(csvRows.Length, Is.EqualTo(HeaderRowCount + 1));
        }

        [Test]
        public void Can_Serialize_MoviesResponse_Dto()
        {
            var request = new MoviesResponse { Movies = ResetMoviesService.Top5Movies };
            var csv = CsvSerializer.SerializeToString(request);
            var csvRows = csv.Split('\n').Where(x => !x.IsNullOrEmpty()).ToArray();
            Assert.That(csvRows.Length, Is.EqualTo(HeaderRowCount + ResetMoviesService.Top5Movies.Count));
        }

        [Test]
        public async Task Can_download_movies_in_Csv()
        {
            var client = new CsvServiceClient(ListeningOn);

            var response = await client.GetAsync<MoviesResponse>(new Movies());

            Assert.That(response, Is.Not.Null, "No response received");
        }

        [Test]
        public void Can_download_CSV_movies_using_csv_reply_endpoint()
        {
            var req = (HttpWebRequest)WebRequest.Create(ListeningOn + "csv/reply/Movies");

            var res = req.GetResponse();
            Assert.That(res.ContentType, Is.EqualTo(MimeTypes.Csv));
            Console.WriteLine(res.Headers);
            Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Movies.csv"));

            var csvRows = res.ReadLines().ToList();

            const int headerRowCount = 1;
            Assert.That(csvRows, Has.Count.EqualTo(headerRowCount + ResetMoviesService.Top5Movies.Count));
            //Console.WriteLine(csvRows.Join("\n"));
        }

        [Test]
        public void Can_download_CSV_movies_using_csv_Accept_and_RestPath()
        {
            var req = (HttpWebRequest)WebRequest.Create(ListeningOn + "all-movies");
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
            var req = (HttpWebRequest)WebRequest.Create(ListeningOn + "csv/reply/Hello?Name=World!");

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
            var req = (HttpWebRequest)WebRequest.Create(ListeningOn + "hello/World!");
            req.Accept = MimeTypes.Csv;

            var res = req.GetResponse();
            Assert.That(res.ContentType, Is.EqualTo(MimeTypes.Csv));
            Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Hello.csv"));

            var csv = res.ReadToEnd();
            var lf = Environment.NewLine;
            Assert.That(csv, Is.EqualTo("Result{0}\"Hello, World!\"{0}".Fmt(lf)));

            Console.WriteLine(csv);
        }

        [Test]
        public void Can_download_CSV_movies_using_csv_reply_Path()
        {
            var req = (HttpWebRequest)WebRequest.Create(ListeningOn + "csv/reply/Movies");
            req.Accept = "application/xml";

            var res = req.GetResponse();
            Assert.That(res.ContentType, Is.EqualTo(MimeTypes.Csv));
            Assert.That(res.Headers[HttpHeaders.ContentDisposition], Is.EqualTo("attachment;filename=Movies.csv"));

            var csvRows = res.ReadLines().ToList();

            Assert.That(csvRows, Has.Count.EqualTo(HeaderRowCount + ResetMoviesService.Top5Movies.Count));
        }
    }
}
