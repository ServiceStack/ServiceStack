#if NET8_0_OR_GREATER
#nullable enable

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Common.Tests;

public class ApiTests
{
    class TestUpload : IReturn<TestUploadResponse>
    {
        
    }
    class TestUploadResponse {}
    class BareTestUpload {}
    
    // Compilation-only test
    public async Task Can_call_different_JsonApiClient_Apis_without_ambiguity_Async()
    {
        UploadFile[] files = [
            new("a.png", new MemoryStream()),
            new("b.png", new MemoryStream()),
        ];
        
        var client = new JsonApiClient("https://example.org");
        TestUploadResponse? r = null;
        r = await client.PostFilesWithRequestAsync(new TestUpload(), files);
        r = await client.PostFilesWithRequestAsync<TestUploadResponse>(new BareTestUpload(), files);
        r = await client.PostFilesWithRequestAsync("/with/url", new TestUpload(), files);
        r = await client.PostFilesWithRequestAsync<TestUploadResponse>("/with/url", new BareTestUpload(), files);

        var lFiles = files.ToList();
        r = await client.PostFilesWithRequestAsync(new TestUpload(), lFiles.ToArray());
        r = await client.PostFilesWithRequestAsync<TestUploadResponse>(new BareTestUpload(), lFiles);
        r = await client.PostFilesWithRequestAsync("/with/url", new TestUpload(), lFiles.ToArray());
        r = await client.PostFilesWithRequestAsync<TestUploadResponse>("/with/url", new BareTestUpload(), lFiles);
    }
    
    // Compilation-only test
    public void Can_call_different_JsonApiClient_Apis_without_ambiguity_Sync()
    {
        UploadFile[] files = [
            new("a.png", new MemoryStream()),
            new("b.png", new MemoryStream()),
        ];
        
        var client = new JsonApiClient("https://example.org");
        TestUploadResponse? r = null;
        r = client.PostFilesWithRequest(new TestUpload(), files);
        r = client.PostFilesWithRequest<TestUploadResponse>(new BareTestUpload(), files);
        r = client.PostFilesWithRequest("/with/url", new TestUpload(), files);
        r = client.PostFilesWithRequest<TestUploadResponse>("/with/url", new BareTestUpload(), files);

        var lFiles = files.ToList();
        r = client.PostFilesWithRequest(new TestUpload(), lFiles.ToArray());
        r = client.PostFilesWithRequest<TestUploadResponse>(new BareTestUpload(), lFiles);
        r = client.PostFilesWithRequest("/with/url", new TestUpload(), lFiles.ToArray());
        r = client.PostFilesWithRequest<TestUploadResponse>("/with/url", new BareTestUpload(), lFiles);
    }
    
}
#endif
