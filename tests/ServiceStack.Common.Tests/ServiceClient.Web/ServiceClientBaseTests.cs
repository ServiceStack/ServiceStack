﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.Common.Tests.ServiceClient.Web
{
    [TestFixture]
    public class ServiceClientBaseTests
    {
        [Test]
        public void SetBaseUri_FormatLoaded_LoadedFormatUsedInSyncAndAsyncUri()
        {
            var serviceClientBaseTester = new ServiceClientBaseTester();
            String baseUri = "BaseURI";

            serviceClientBaseTester.SetBaseUri(baseUri);

            String expectedBaseUri = baseUri;
            String expectedSyncReplyBaseUri = baseUri + "/" + serviceClientBaseTester.Format + "/syncreply/";
            String expectedAsyncOneWayBaseUri = baseUri + "/" + serviceClientBaseTester.Format + "/asynconeway/";
            Assert.That(serviceClientBaseTester.BaseUri, Is.EqualTo(expectedBaseUri));
            Assert.That(serviceClientBaseTester.SyncReplyBaseUri, Is.EqualTo(expectedSyncReplyBaseUri));
            Assert.That(serviceClientBaseTester.AsyncOneWayBaseUri, Is.EqualTo(expectedAsyncOneWayBaseUri));
        }
    }
}
