﻿using System;
using System.Diagnostics;
using System.Threading;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class BuiltinRouteServices : Service {}

    public class BuiltinRouteTests
    {
        public class BuiltinPathAppHost : AppSelfHostBase
        {
            public BuiltinPathAppHost()
                : base(typeof(BuiltinPathAppHost).Name, typeof(BuiltinRouteServices).Assembly) {}

            public override void Configure(Container container)
            {
                PreRequestFilters.Add((req, res) =>
                {
                    req.UseBufferedStream = true;
                    res.UseBufferedStream = true;
                });
            }
        }

        readonly ServiceStackHost appHost;
        public BuiltinRouteTests()
        {
            appHost = new BuiltinPathAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Ignore("Debug Run")]
        [Test]
        public void RunFor10Mins()
        {
            Process.Start(Config.AbsoluteBaseUri);
            Thread.Sleep(TimeSpan.FromMinutes(10));
        }

        [Test]
        public void Can_download_metadata_page()
        {
            var contents = "{0}/metadata".Fmt(Config.AbsoluteBaseUri).GetStringFromUrl();
            Assert.That(contents, Is.StringContaining("The following operations are supported."));
        }

        [Test]
        public void Can_download_File_Template_OperationControl()
        {
            var contents = "{0}/json/metadata?op=Hello".Fmt(Config.AbsoluteBaseUri).GetStringFromUrl();
            Assert.That(contents, Is.StringContaining("/hello/{Name}"));
        }
    }
}