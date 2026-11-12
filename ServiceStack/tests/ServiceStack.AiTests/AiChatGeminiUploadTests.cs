#nullable enable
using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public class AiChatGeminiUploadTests
{
    sealed class Factory(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new AiChatMigrationHttpHandler(send));
    }
    static HttpResponseMessage Response(string json = "{}", HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json) };

    [Test]
    public async Task Polling_retries_unavailable_response_without_starting_another_upload()
    {
        var polls = 0;
        var client = new GeminiClient(new Factory((request, _) => {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/v1beta/operations/accepted"));
            return Task.FromResult(++polls == 1
                ? Response("""{"error":{"message":"Temporarily unavailable"}}""", HttpStatusCode.ServiceUnavailable)
                : Response("""{"done":true,"response":{"documentName":"documents/ready"}}"""));
        }), "fixture") { PollInterval = TimeSpan.FromMilliseconds(1), OperationTimeout = TimeSpan.FromSeconds(5) };
        var result = await client.WaitForOperationAsync(new JsonObject { ["name"] = "operations/accepted" });
        Assert.That(result.GetBool("done"), Is.True);
        Assert.That(polls, Is.EqualTo(2));
    }

    [Test]
    public void Polling_deadline_reports_indexing_operation_and_preserves_caller_cancellation()
    {
        var client = new GeminiClient(new Factory((_, _) => Task.FromResult(Response())), "fixture") {
            PollInterval = TimeSpan.FromSeconds(1), OperationTimeout = TimeSpan.FromMilliseconds(20),
        };
        var error = Assert.ThrowsAsync<TimeoutException>(() => client.WaitForOperationAsync(new JsonObject { ["name"] = "operations/slow" }));
        Assert.That(error!.Message, Does.Contain("operations/slow").And.Contain("file was transferred"));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.ThrowsAsync<TaskCanceledException>(() => client.WaitForOperationAsync(new JsonObject { ["name"] = "operations/slow" }, cancelled.Token));
    }

    [Test]
    public void Polling_deadline_includes_last_transient_failure()
    {
        var client = new GeminiClient(new Factory((_, _) => Task.FromResult(
            Response("""{"error":{"message":"Provider overloaded"}}""", HttpStatusCode.ServiceUnavailable))), "fixture") {
            PollInterval = TimeSpan.FromMilliseconds(1), OperationTimeout = TimeSpan.FromMilliseconds(100),
        };
        var error = Assert.ThrowsAsync<TimeoutException>(() => client.WaitForOperationAsync(new JsonObject { ["name"] = "operations/slow" }));
        Assert.That(error!.Message, Does.Contain("Provider overloaded"));
    }

    [Test]
    public void Polling_permanent_error_is_returned_immediately()
    {
        var polls = 0;
        var client = new GeminiClient(new Factory((_, _) => { polls++; return Task.FromResult(
            Response("""{"error":{"message":"Permission denied"}}""", HttpStatusCode.Forbidden)); }), "fixture") {
            PollInterval = TimeSpan.FromMilliseconds(1),
        };
        var error = Assert.ThrowsAsync<GeminiApiException>(() => client.WaitForOperationAsync(new JsonObject { ["name"] = "operations/denied" }));
        Assert.That(error!.StatusCode, Is.EqualTo(403));
        Assert.That(polls, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Retry_resumes_saved_operation_but_changed_metadata_starts_new_upload(bool changeMetadata)
    {
        using var host = new AiChatMigrationTestHost();
        host.Feature.Services = new ServiceCollection().BuildServiceProvider();
        host.Feature.Log = NullLogger.Instance;
        var db = new GeminiDb(host.Db);
        db.InitSchema();
        var store = db.InsertFilestore(new ChatFilestore { User = "alice", Name = "fileSearchStores/docs" });
        var context = new ExtensionContext(host.Feature, "gemini");
        var path = context.GetCachePath("aa/document.md");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "test markdown");
        var doc = new ChatDocument { User = "alice", FilestoreId = store, DisplayName = "document.md", Hash = "hash", Url = "/~cache/aa/document.md", MimeType = "text/markdown" };
        doc.Id = db.InsertDocument(doc);
        var uploads = 0;
        var complete = false;
        var terminal = false;
        var factory = new Factory(async (request, token) => {
            var requestPath = request.RequestUri!.AbsolutePath;
            if (requestPath.Contains(":uploadToFileSearchStore")) {
                uploads++;
                var response = Response();
                response.Headers.Add("x-goog-upload-url", "https://fixture.example/finalize");
                return response;
            }
            if (requestPath == "/finalize") {
                Assert.That(request.Content!.Headers.ContentType!.MediaType, Is.EqualTo("text/markdown"));
                Assert.That(await request.Content.ReadAsStringAsync(token), Is.EqualTo("test markdown"));
                return Response("""{"name":"operations/accepted"}""");
            }
            if (requestPath == "/v1beta/operations/accepted") {
                if (terminal) return Response("""{"done":true,"error":{"code":3,"message":"Cannot index document"}}""");
                return complete ? Response("""{"done":true,"response":{"documentName":"documents/ready"}}""")
                    : Response("""{"name":"operations/accepted"}""");
            }
            if (requestPath == "/v1beta/documents/ready")
                return Response("""{"name":"documents/ready","displayName":"document.md","mimeType":"text/markdown","state":"STATE_ACTIVE"}""");
            return Response();
        });
        var client = new GeminiClient(factory, "fixture") { PollInterval = TimeSpan.FromMilliseconds(5), OperationTimeout = TimeSpan.FromMilliseconds(50) };
        async Task Drain() {
            var worker = new GeminiUploadWorker(context, db, client, new GeminiStores(db, client, NullLogger.Instance));
            try {
                worker.Start();
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                while (worker.Running) await Task.Delay(5, deadline.Token);
            } finally { await worker.StopAsync(); }
        }
        await Drain();
        var saved = db.GetDocument(doc.Id, "alice")!;
        Assert.That(saved.UploadOperationName, Is.EqualTo("operations/accepted"));
        Assert.That(saved.Error, Does.Contain("operations/accepted"));
        Assert.That(saved.UploadedAt, Is.Null);
        Assert.That(uploads, Is.EqualTo(1));
        if (changeMetadata) { saved.Category = "changed"; db.UpdateDocument(saved); }
        db.ResetDocumentUpload(doc.Id);
        complete = true;
        // A new worker instance simulates retry after a host restart.
        await Drain();
        saved = db.GetDocument(doc.Id, "alice")!;
        Assert.That(uploads, Is.EqualTo(changeMetadata ? 2 : 1));
        Assert.That(saved.UploadedAt, Is.Not.Null);
        Assert.That(saved.UploadOperationName, Is.Null);
        Assert.That(saved.Error, Is.Null);
        // A terminal operation error must clear the receipt so explicit retry can upload again.
        saved.Name = null; saved.UploadedAt = null; db.UpdateDocument(saved);
        terminal = true;
        await Drain();
        saved = db.GetDocument(doc.Id, "alice")!;
        Assert.That(saved.UploadOperationName, Is.Null);
        Assert.That(saved.Error, Does.Contain("code 3").And.Contain("Cannot index document"));
    }
}
