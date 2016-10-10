//TODO: fix test failures when running on Linux build agent
#if NETCORE

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using RestFiles.ServiceModel;
using ServiceStack;

/* For syntax highlighting and better readability of this file, view it on GitHub:
 * https://github.com/ServiceStack/ServiceStack.Examples/blob/master/src/RestFiles/RestFiles.Tests/AsyncRestClientTests.cs
 */

namespace RestFiles.Tests
{
    /// <summary>
    /// These test show how you can call ServiceStack REST web services asynchronously using an IRestClientAsync.
    /// 
    /// Async service calls are a great for GUI apps as they can be called without blocking the UI thread.
    /// They are also great for performance as no time is spent on blocking IO calls.
    /// </summary>
    [TestFixture]
    public class AsyncRestClientTests
    {
        public const string WebServiceHostUrl = "http://localhost:8080/";
        private const string ReadmeFileContents = "THIS IS A README FILE";
        private const string ReplacedFileContents = "THIS README FILE HAS BEEN REPLACED";
        private const string TestUploadFileContents = "THIS FILE IS USED FOR UPLOADING IN TESTS";
        public string FilesRootDir;

        TestAppHost appHost;

        [OneTimeSetUp]
        public void TextFixtureSetUp()
        {
            appHost = new TestAppHost();
            appHost.Init();
            appHost.Start(TestAppHost.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown() => appHost.Dispose();

        [SetUp]
        public void OnBeforeEachTest()
        {
            //Setup the files directory with some test files and folders
            FilesRootDir = appHost.MapProjectPath("~/App_Data/files/");
            if (Directory.Exists(FilesRootDir))
            {
                Directory.Delete(FilesRootDir, true);
            }
            Directory.CreateDirectory(FilesRootDir + "SubFolder");
            Directory.CreateDirectory(FilesRootDir + "SubFolder2");
            File.WriteAllText(Path.Combine(FilesRootDir, "README.txt"), ReadmeFileContents);
            File.WriteAllText(Path.Combine(FilesRootDir, "TESTUPLOAD.txt"), TestUploadFileContents);
        }

        public IRestClientAsync CreateAsyncRestClient()
        {
            return new JsonServiceClient(WebServiceHostUrl);  //Best choice for Ajax web apps, faster than XML
            //return new XmlServiceClient(WebServiceHostUrl); //Ubiquitous structured data format best for supporting non .NET clients
            //return new JsvServiceClient(WebServiceHostUrl); //Fastest, most compact and resilient format great for .NET to .NET client / server
        }

        private static void FailOnAsyncError<T>(T response, Exception ex)
        {
            Assert.Fail(ex.Message);
        }

        [Test]
        public async Task Can_GetAsync_to_retrieve_existing_file()
        {
            var restClient = CreateAsyncRestClient();

            var response = await restClient.GetAsync<FilesResponse>("files/README.txt");

            Assert.That(response.File.Contents, Is.EqualTo("THIS IS A README FILE"));
        }

        [Test]
        public async Task Can_GetAsync_to_retrieve_existing_folder_listing()
        {
            var restClient = CreateAsyncRestClient();

            var response = await restClient.GetAsync<FilesResponse>("files/");

            Assert.That(response.Directory.Folders.Count, Is.EqualTo(2));
            Assert.That(response.Directory.Files.Count, Is.EqualTo(2));
        }

        [Test]
        public void Can_WebRequest_POST_upload_file_to_save_new_file_and_create_new_Directory()
        {
            var webRequest = WebRequest.Create(WebServiceHostUrl + "files/UploadedFiles/");

            var fileToUpload = new FileInfo(FilesRootDir + "TESTUPLOAD.txt");
            using (var stream = fileToUpload.OpenRead())
            {
                webRequest.UploadFile(stream, fileToUpload.Name);
                var webRes = PclExport.Instance.GetResponse(webRequest);
            }

            Assert.That(Directory.Exists(FilesRootDir + "UploadedFiles"));
            Assert.That(File.ReadAllText(FilesRootDir + "UploadedFiles/TESTUPLOAD.txt"),
                        Is.EqualTo(TestUploadFileContents));
        }

        [Test]
        public void Can_RestClient_POST_upload_file_to_save_new_file_and_create_new_Directory()
        {
            var restClient = (IRestClient)CreateAsyncRestClient();

            var fileToUpload = new FileInfo(FilesRootDir + "TESTUPLOAD.txt");
            restClient.PostFile<FilesResponse>("files/UploadedFiles/",
                fileToUpload, MimeTypes.GetMimeType(fileToUpload.Name));

            Assert.That(Directory.Exists(FilesRootDir + "UploadedFiles"));
            Assert.That(File.ReadAllText(FilesRootDir + "UploadedFiles/TESTUPLOAD.txt"),
                        Is.EqualTo(TestUploadFileContents));
        }

        [Test]
        public async Task Can_PutAsync_to_replace_text_content_of_an_existing_file()
        {
            var restClient = CreateAsyncRestClient();

            var response = await restClient.PutAsync<FilesResponse>("files/README.txt",
                new Files { TextContents = ReplacedFileContents });

            Assert.That(File.ReadAllText(FilesRootDir + "README.txt"),
                        Is.EqualTo(ReplacedFileContents));
        }

        [Test]
        public async Task Can_DeleteAsync_to_replace_text_content_of_an_existing_file()
        {
            var restClient = CreateAsyncRestClient();

            var response = await restClient.DeleteAsync<FilesResponse>("files/README.txt");

            Assert.That(!File.Exists(FilesRootDir + "README.txt"));
        }


        /* 
         * Error Handling Tests
         */
        [Test]
        public async Task GET_a_file_that_doesnt_exist_throws_a_404_FileNotFoundException()
        {
            var restClient = CreateAsyncRestClient();

            try
            {
                await restClient.GetAsync<FilesResponse>("files/UnknownFolder");
            }
            catch (WebServiceException webEx)
            {
                var response = (FilesResponse)webEx.ResponseDto;
                Assert.That(webEx.StatusCode, Is.EqualTo(404));
                Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo(typeof(FileNotFoundException).Name));
                Assert.That(response.ResponseStatus.Message, Is.EqualTo("Could not find: UnknownFolder"));
            }
        }

        [Test]
        public async Task POST_to_an_existing_file_throws_a_500_NotSupportedException()
        {
            var restClient = (IRestClient)CreateAsyncRestClient();

            var fileToUpload = new FileInfo(FilesRootDir + "TESTUPLOAD.txt");

            try
            {
                var response = restClient.PostFile<FilesResponse>("files/README.txt",
                    fileToUpload, MimeTypes.GetMimeType(fileToUpload.Name));

                Assert.Fail("Should fail with NotSupportedException");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(405));
                var response = (FilesResponse)webEx.ResponseDto;
                Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo(typeof(NotSupportedException).Name));
                Assert.That(response.ResponseStatus.Message,
                    Is.EqualTo("POST only supports uploading new files. Use PUT to replace contents of an existing file"));
            }
        }

        [Test]
        public async Task PUT_to_replace_a_non_existing_file_throws_404()
        {
            var restClient = CreateAsyncRestClient();

            try
            {
                await restClient.PutAsync<FilesResponse>("files/non-existing-file.txt",
                    new Files { TextContents = ReplacedFileContents });
            }
            catch (WebServiceException webEx)
            {
                var response = (FilesResponse)webEx.ResponseDto;
                Assert.That(webEx.StatusCode, Is.EqualTo(404));
                Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo(typeof(FileNotFoundException).Name));
                Assert.That(response.ResponseStatus.Message, Is.EqualTo("Could not find: non-existing-file.txt"));
            }
        }

        [Test]
        public async Task DELETE_a_non_existing_file_throws_404()
        {
            var restClient = CreateAsyncRestClient();

            try
            {
                await restClient.DeleteAsync<FilesResponse>("files/non-existing-file.txt");
            }
            catch (WebServiceException webEx)
            {
                var response = (FilesResponse)webEx.ResponseDto;

                Assert.That(webEx.StatusCode, Is.EqualTo(404));
                Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo(typeof(FileNotFoundException).Name));
                Assert.That(response.ResponseStatus.Message, Is.EqualTo("Could not find: non-existing-file.txt"));
            }
        }

    }
}

#endif
