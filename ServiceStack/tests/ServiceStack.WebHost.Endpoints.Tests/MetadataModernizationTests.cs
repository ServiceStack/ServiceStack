using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Metadata;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class MetadataModernizationTests
{
    [Test]
    public void ServiceMetadata_Constructor_And_AfterInit_Handle_Null_RestPaths()
    {
        var metadata = new ServiceMetadata(null);
        Assert.DoesNotThrow(() => metadata.AfterInit());
        Assert.That(metadata.OperationsMap, Is.Not.Null);
    }

    [Test]
    public void ServiceMetadata_Add_NullGuards()
    {
        var metadata = new ServiceMetadata();
        Assert.Throws<ArgumentNullException>(() => metadata.Add(null, typeof(object), null));
        Assert.Throws<ArgumentNullException>(() => metadata.Add(typeof(object), null, null));
    }

    [Test]
    public void ServiceMetadata_GetOperationsByTags_NullGuards()
    {
        var metadata = new ServiceMetadata();
        var byTag = metadata.GetOperationsByTag(null);
        Assert.That(byTag, Is.Not.Null);
        Assert.That(byTag.Count, Is.EqualTo(0));

        var byTags = metadata.GetOperationsByTags(null);
        Assert.That(byTags, Is.Not.Null);
        Assert.That(byTags.Count, Is.EqualTo(0));
    }

    [Test]
    public void ServiceMetadata_GetOperationType_NullGuard()
    {
        var metadata = new ServiceMetadata();
        Assert.That(metadata.GetOperationType(null), Is.Null);
    }

    [Test]
    public void ServiceMetadata_CreateRequestFromUrl_NullGuard()
    {
        var metadata = new ServiceMetadata();
        Assert.Throws<ArgumentNullException>(() => metadata.CreateRequestFromUrl(null));
    }

    [Test]
    public void ServiceMetadata_IsAuthorized_NullGuards()
    {
        var metadata = new ServiceMetadata();
        Assert.That(metadata.IsAuthorized((Operation)null, (IRequest)null, (IAuthSession)null), Is.False);
        Assert.That(metadata.IsAuthorized((Operation)null, (AuthenticateResponse)null), Is.False);
    }

    [Test]
    public async Task ServiceMetadata_IsAuthorizedAsync_NullGuards()
    {
        var metadata = new ServiceMetadata();
        var result = await metadata.IsAuthorizedAsync(null, null, null);
        Assert.That(result, Is.False);
    }

    [Test]
    public void ServiceMetadata_IsVisible_And_CanAccess_NullGuards()
    {
        var metadata = new ServiceMetadata();
        Assert.That(metadata.IsVisible(null, (Operation)null), Is.False);
        Assert.That(metadata.IsVisible(null, (Type)null), Is.False);
        Assert.That(metadata.IsVisible(null, Format.Json, null), Is.False);
        Assert.That(metadata.CanAccess((IRequest)null, Format.Json, null), Is.False);
        Assert.That(metadata.CanAccess(Format.Json, null), Is.False);
    }

    public class DummyRequest { public int Id { get; set; } }
    public class DummyRequest2 { public int Id { get; set; } }
    public class DummyResponse { public string Result { get; set; } }
    public class DummyService : IService { }

    [Test]
    public void ServiceMetadata_GetDtoTypes_Filter_Does_Not_Corrupt_Global_Cache()
    {
        var metadata = new ServiceMetadata();
        metadata.Add(typeof(DummyService), typeof(DummyRequest), typeof(DummyResponse));
        metadata.Add(typeof(DummyService), typeof(DummyRequest2), null);

        // Filtered call only matching DummyRequest
        var filtered = metadata.GetDtoTypes(t => t == typeof(DummyRequest));
        Assert.That(filtered.Contains(typeof(DummyRequest)), Is.True);
        Assert.That(filtered.Contains(typeof(DummyRequest2)), Is.False);
        Assert.That(filtered.Contains(typeof(DummyResponse)), Is.False);

        // Subsequent call to GetAllDtos returns all DTOs, not contaminated by filtered cache
        var all = metadata.GetAllDtos();
        Assert.That(all.Contains(typeof(DummyRequest)), Is.True);
        Assert.That(all.Contains(typeof(DummyRequest2)), Is.True);
        Assert.That(all.Contains(typeof(DummyResponse)), Is.True);

        // Calling filtered again with a different filter still works properly
        var filteredAgain = metadata.GetDtoTypes(t => t == typeof(DummyRequest2));
        Assert.That(filteredAgain.Contains(typeof(DummyRequest2)), Is.True);
        Assert.That(filteredAgain.Contains(typeof(DummyRequest)), Is.False);
        Assert.That(filteredAgain.Contains(typeof(DummyResponse)), Is.False);
    }

    [Test]
    public void OperationDto_ToOperationDto_Safe_With_Sparse_Operation()
    {
        var op = new Operation
        {
            RequestType = typeof(DummyRequest),
            ServiceType = typeof(DummyService),
        };
        var dto = op.ToOperationDto();
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Name, Is.EqualTo(typeof(DummyRequest).GetOperationName()));
        Assert.That(dto.ServiceName, Is.EqualTo(typeof(DummyService).GetOperationName()));
    }

    [Test]
    public void MetadataPagesConfig_NullGuards()
    {
        var config = new MetadataPagesConfig(null, null, null, null);
        Assert.That(config.AvailableFormatConfigs, Is.Not.Null);
        Assert.That(config.AvailableFormatConfigs.Count, Is.EqualTo(0));
        Assert.That(config.GetMetadataConfig(null), Is.Null);
        Assert.That(config.GetMetadataConfig("unknown"), Is.Null);
        Assert.That(config.IsVisible(null, Format.Json, null), Is.False);
        Assert.That(config.CanAccess(null, Format.Json, null), Is.False);
        Assert.That(config.CanAccess(Format.Json, null), Is.False);
        Assert.That(config.AlwaysHideInMetadata(null), Is.False);
    }

    [Test]
    public void IndexOperationsControl_NullGuards()
    {
        var control = new IndexOperationsControl();
        Assert.That(control.RenderRow(null), Is.EqualTo(""));
        var urls = control.ToAbsoluteUrls(null);
        Assert.That(urls, Is.Not.Null);
        Assert.That(urls.Count, Is.EqualTo(0));
    }

    [Test]
    public void OperationControl_NullGuards()
    {
        var control = new OperationControl
        {
            OperationName = "TestOp",
            ContentType = MimeTypes.Json,
            Format = Format.Json,
        };
        Assert.That(control.RequestUri, Does.Contain("TestOp"));
        Assert.That(control.GetHttpRequestTemplate(), Does.Contain("POST"));
    }

    [Test]
    public void MetadataFeature_GetHandler_NullGuards()
    {
        var feature = new MetadataFeature();
        Assert.That(feature.GetHandler(null), Is.Null);

        var mockReq = new MockHttpRequest { PathInfo = "" };
        Assert.That(feature.GetHandler(mockReq), Is.Null);

        mockReq.PathInfo = "/";
        Assert.That(feature.GetHandler(mockReq), Is.Null);

        mockReq.PathInfo = "/nonexistent";
        Assert.That(feature.GetHandler(mockReq), Is.Null);
    }

    [Test]
    public void MetadataFeature_Link_Extension_Methods_NullGuards()
    {
        MetadataFeature feature = null;
        Assert.DoesNotThrow(() => feature.AddPluginLink("href", "title"));
        Assert.DoesNotThrow(() => feature.RemovePluginLink("href"));
        Assert.DoesNotThrow(() => feature.AddDebugLink("href", "title"));
        Assert.DoesNotThrow(() => feature.RemoveDebugLink("href"));

        var actualFeature = new MetadataFeature();
        Assert.DoesNotThrow(() => actualFeature.RemovePluginLink(null));
        Assert.DoesNotThrow(() => actualFeature.RemoveDebugLink(null));
    }

    private class TestMetadataHandler : BaseMetadataHandler
    {
        public override Format Format => Format.Json;
        protected override string CreateMessage(Type dtoType) => dtoType.Name;
        public bool TestAssertAccess(IRequest req, IResponse res, string op) => AssertAccess(req, res, op);
    }

    [Test]
    public void BaseMetadataHandler_AssertAccess_Without_AppHost_Returns_False()
    {
        var handler = new TestMetadataHandler();
        Assert.That(handler.TestAssertAccess(new MockHttpRequest(), new MockHttpResponse(), "TestOp"), Is.False);
    }
}
