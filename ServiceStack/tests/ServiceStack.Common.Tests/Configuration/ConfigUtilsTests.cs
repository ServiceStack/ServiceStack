﻿using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;

namespace ServiceStack.Common.Tests;

[TestFixture]
public class ConfigUtilsTests
{
    public class AppHostTest() : AppSelfHostBase("Test Config AppHost", typeof(AppHostTest).Assembly)
    {
        public override void Configure(Container container) {}
    }

    [Test]
    public void Can_parse_AppConfig_AppSettings_with_XmlReader()
    {
        using (new AppHostTest().Init())
        {
            var map = ConfigUtils.GetAppSettingsMap();
            Assert.That(map.Count, Is.EqualTo(11));
            Assert.That(map.Keys, Is.EquivalentTo(new[] {
                "servicestack:license",
                "EmptyKey",
                "RealKey",
                "ListKey",
                "IntKey",
                "BadIntegerKey",
                "DictionaryKey",
                "BadDictionaryKey",
                "ObjectNoLineFeed",
                "ObjectWithLineFeed",
                "Email",
            }));
        }
    }
}
