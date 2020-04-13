// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class VirtualPathTests
    {
        public static string ServiceStackBaseUri = Config.ServiceStackBaseUri;

        [Test]
        public void Can_download_static_file_at_root_directory()
        {
            var contents = "{0}/static-root.txt".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Is.EqualTo("static"));
        }

        [Test]
        public void Can_download_static_file_at_sub_directory()
        {
            var contents = "{0}/Content/static-sub.txt".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Is.EqualTo("static"));
        }

        [Test]
        public void Can_download_embedded_static_file_at_root_directory()
        {
            var contents = "{0}/static-root-embedded.txt".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Is.EqualTo("static"));
        }

        [Test]
        public void Can_download_embedded_static_file_at_sub_directory()
        {
            var contents = "{0}/Content/static-sub-embedded.txt".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Is.EqualTo("static"));
        }

        [Test]
        public void Can_download_default_static_file_at_sub_directory()
        {
            var contents = "{0}/Content".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Is.EqualTo("static"));
        }

        [Test]
        public void Can_download_default_document_at_root_directory()
        {
            var contents = "{0}/".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Is.Not.Null);
        }

        [Test]
        public void Can_download_ServiceStack_Template_IndexOperations()
        {
            var contents = "{0}/metadata".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Does.Contain("The following operations are supported."));
        }

        [Test]
        public void Can_download_File_Template_OperationControl()
        {
            var contents = "{0}/json/metadata?op=Hello".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Does.Contain("(File Resource)"));
        }

        [Test]
        public void Can_download_EmbeddedResource_Template_HtmlFormat()
        {
            var contents = "{0}/hello".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Does.Contain("(Embedded Resource)"));
        }

        [Test]
        public void Can_get_service_matching_api_prefix()
        {
            var contents = "{0}/gettestapi".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Does.Contain("GetTestapi"));
        }

        [Test]
        public void Can_get_swagger_urls()
        {
            var contents = "{0}/swagger-ui/".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Does.Contain(ServiceStackBaseUri));

            contents = "{0}/api/openapi".Fmt(ServiceStackBaseUri).GetJsonFromUrl();
            Assert.That(contents, Does.Contain("ServiceStack WebHost IntegrationTests"));
        }

        [Test]
        public void Can_call_template_Service()
        {
            var client = new JsonServiceClient(ServiceStackBaseUri);

            var postResponse = client.Post(new PostTemplateRequest { Template = "foo" });
            Assert.That(postResponse.PostResult, Is.EqualTo("foo"));

            var getResponse = client.Get(new GetTemplatesRequest { Name = "bar" });
            Assert.That(getResponse.GetResult, Is.EqualTo("bar"));

            var getSingleResponse = client.Get(new GetTemplateRequest { Name = "baz" });
            Assert.That(getSingleResponse.GetSingleResult, Is.EqualTo("baz"));
        }
    }
}