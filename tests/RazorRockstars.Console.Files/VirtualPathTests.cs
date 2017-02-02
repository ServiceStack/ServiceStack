// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace RazorRockstars.Console.Files
{
    [TestFixture]
    public class VirtualPathTests
    {
        public static string ServiceStackBaseUri = RazorRockstars_FilesTests.Host;
        public static string ListeningOn = RazorRockstars_FilesTests.ListeningOn;

        private AppHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }
        

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
            var contents = "{0}/Content/".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Is.EqualTo("static"));
        }

        [Test]
        public void Can_download_default_document_at_root_directory()
        {
            var contents = "{0}/".Fmt(ServiceStackBaseUri).GetStringFromUrl();
            Assert.That(contents, Is.Not.Null);
        }
    }
}